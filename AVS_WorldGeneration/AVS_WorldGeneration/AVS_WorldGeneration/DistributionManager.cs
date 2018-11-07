using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AVS_WorldGeneration
{
    class DistributionManager
    {
        delegate void VoronoiProgress(double bValue);
        delegate void Logging(string sLogMessage, LogLevel eLogLevel);

        public List<Helper.Node> acAvailableNodes;

        #region Own Stats
        
        private int m_nPhysicalProcessors = 0;
        private int m_nCores = 0;
        private int m_nLogicalProcessors = 0;
        private int m_nThreadsInUse = 1;

        private List<uint> m_acCurrentCPUFrequency;
        private List<uint> m_acMaxCPUFrequency;

        #endregion

        #region Generation Variables

        public List<List<BenTools.Mathematics.Vector>> acVectors
        {
            get { return m_acVectors; }
        }

        private List<List<BenTools.Mathematics.Vector>> m_acVectors = new List<List<BenTools.Mathematics.Vector>>();
        private Random m_kRnd = new Random();
        private int m_nSeed = 0;

        private double m_dMinimum;
        private double m_dMaximum;

        #endregion

        #region Threading Variables

        private double m_dVoronoiCount_Threaded = 0.0;
        private int m_nVoronoiFinished_Threaded = 0;

        private Object m_cObjThreadLocking = new Object();
        private Object m_cObjProgressbarLocking = new Object();

        #endregion

        #region Broadcast Socket

        private Socket m_cBroadcastSocket;
        private EndPoint m_cBroadcastServerEndpoint;
        private EndPoint m_cBroadcastClientEndpoint;
        private byte[] m_abBroadcastReceiveBuffer;
        private int m_nBroadcastReceiveBufferLength;
        private SocketAsyncEventArgs m_cBroadcastArgs;
        private bool m_bBroadcastClose;

        #endregion

        public DistributionManager(double dMinimum, double dMaximum)
        {
            m_dMinimum = dMinimum;
            m_dMaximum = dMaximum;

            acAvailableNodes = new List<Helper.Node>();
            
            m_nSeed = m_kRnd.Next();
            m_kRnd = new Random(m_nSeed);
            (Application.Current.MainWindow as MainWindow).tbxSeed.Text = m_nSeed.ToString();

            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
            {
                m_nPhysicalProcessors = int.Parse(item["NumberOfProcessors"].ToString());
                m_nLogicalProcessors = int.Parse(item["NumberOfLogicalProcessors"].ToString());
            }
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                m_nCores += int.Parse(item["NumberOfCores"].ToString());
            }

            m_acCurrentCPUFrequency = new List<uint>();
            m_acMaxCPUFrequency = new List<uint>();

            for (int i = 0; i < m_nPhysicalProcessors; i++)
            {
                string sCPU = "Win32_Processor.DeviceID='CPU" + i.ToString() + "'";
                using (ManagementObject item = new ManagementObject(sCPU))
                {
                    m_acCurrentCPUFrequency.Add((uint)(item["CurrentClockSpeed"]));
                    m_acMaxCPUFrequency.Add((uint)(item["MaxClockSpeed"]));
                }
            }

            (Application.Current.MainWindow as MainWindow).tllbSystemInfo.Content = "Number of physical processors:\t" + m_nPhysicalProcessors.ToString() + "\nNumber of cores:\t\t\t" + m_nCores.ToString() + "\nNumber of logical processors:\t" + m_nLogicalProcessors.ToString() + "\nNumber of threads in use:\t\t" + m_nThreadsInUse.ToString();
        }

        public void SearchNodes()
        {
            try
            {
                m_nBroadcastReceiveBufferLength = 1024;
                m_abBroadcastReceiveBuffer = new byte[m_nBroadcastReceiveBufferLength];
                m_cBroadcastServerEndpoint = new IPEndPoint(IPHelper.GetBroadcastAddress(IPHelper.GetLocalIPAddress(), IPHelper.GetLocalSubmask()), 7345);
                m_cBroadcastClientEndpoint = new IPEndPoint(IPHelper.GetLocalIPAddress(), 55261);
                m_bBroadcastClose = false;

                m_cBroadcastSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                m_cBroadcastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                m_cBroadcastSocket.Bind(m_cBroadcastClientEndpoint);

                byte[] abBroadcastContent = new byte[m_nBroadcastReceiveBufferLength];
                abBroadcastContent[0] = Helper.SocketCommunicationProtocol.SEARCH_FOR_NODES;

                m_cBroadcastArgs = new SocketAsyncEventArgs();
                m_cBroadcastArgs.RemoteEndPoint = m_cBroadcastClientEndpoint;
                m_cBroadcastArgs.SetBuffer(m_abBroadcastReceiveBuffer, 0, m_nBroadcastReceiveBufferLength);

                m_cBroadcastArgs.Completed += BroadcastCompleted;

                m_cBroadcastSocket.ReceiveMessageFromAsync(m_cBroadcastArgs);
                m_cBroadcastSocket.SendTo(abBroadcastContent, m_cBroadcastServerEndpoint);

                BroadcastClose();
                Debug.WriteLine("Sended broadcast");
            }
            catch(Exception cException)
            {
                Debug.WriteLine("Couldn't create socket: " + cException.Message);
            }
        }

        public void DistributeWork(int nSeed, double dVoronoiCount)
        {
            m_nSeed = nSeed;
            m_kRnd = new Random(m_nSeed);



            if (acAvailableNodes.Count <= 0)
            {
                // LOCAL DISTRIBUTED
                (Application.Current.MainWindow as MainWindow).AddLog("Start process: Generate World", LogLevel.INFO);
                (Application.Current.MainWindow as MainWindow).AddLog("Info: Seed: " + m_nSeed.ToString(), LogLevel.INFO);
                (Application.Current.MainWindow as MainWindow).AddLog("Info: Loop count: " + dVoronoiCount.ToString(), LogLevel.INFO);

                (Application.Current.MainWindow as MainWindow).AddLog("Info: Physical processors: " + m_nPhysicalProcessors.ToString(), LogLevel.INFO);
                (Application.Current.MainWindow as MainWindow).AddLog("Info: Cores: " + m_nCores.ToString(), LogLevel.INFO);
                (Application.Current.MainWindow as MainWindow).AddLog("Info: Logical processors: " + m_nLogicalProcessors.ToString(), LogLevel.INFO);
                (Application.Current.MainWindow as MainWindow).AddLog("Info: Threads in use: " + m_nThreadsInUse.ToString(), LogLevel.INFO);

                for (int nCurrent = 0; nCurrent < m_acCurrentCPUFrequency.Count; nCurrent++)
                {
                    (Application.Current.MainWindow as MainWindow).AddLog("Info: CPU" + nCurrent + " current frequency: " + m_acCurrentCPUFrequency[nCurrent].ToString(), LogLevel.INFO);
                    (Application.Current.MainWindow as MainWindow).AddLog("Info: CPU" + nCurrent + " maximal frequency: " + m_acMaxCPUFrequency[nCurrent].ToString(), LogLevel.INFO);
                }

                double dOneStep = 100.0 / (dVoronoiCount + (dVoronoiCount * 0.33f));
                m_acVectors = new List<List<BenTools.Mathematics.Vector>>();

                (Application.Current.MainWindow as MainWindow).AddLog("Start process: Randomizing Vectors", LogLevel.INFO);

                m_dVoronoiCount_Threaded = dVoronoiCount / m_nThreadsInUse;
                double dExtra = dVoronoiCount % m_nThreadsInUse;

                for (int i = 0; i < m_nThreadsInUse; i++)
                {
                    (Application.Current.MainWindow as MainWindow).AddLog("Start process: Thread " + (i + 1).ToString() + "/" + m_nThreadsInUse.ToString() + " Generation", LogLevel.INFO);
                    BackgroundWorker cWorker = new BackgroundWorker
                    {
                        WorkerReportsProgress = false,
                        WorkerSupportsCancellation = false
                    };
                    cWorker.DoWork += GenerateVoronoiVectors;
                    cWorker.RunWorkerCompleted += GenerateVoronoiVectorsFinished;

                    cWorker.RunWorkerAsync(dVoronoiCount / m_nThreadsInUse + dExtra);
                    dExtra = 0.0;
                }
            }
            else
            {
                // NETWORK DISTRIBUTED
            }
        }

        private async void BroadcastClose()
        {
            for (int nProgress = 0; nProgress < 1000; nProgress++)
            {
                await Task.Delay(10);
                (Application.Current.MainWindow as MainWindow).pbSearchForNodes.Value += 0.1f;
            }
            m_bBroadcastClose = true;
            (Application.Current.MainWindow as MainWindow).UpdateNodeList();
        }

        private void BroadcastCompleted(object cSender, SocketAsyncEventArgs cArgs)
        {
            Debug.WriteLine("Called Broadcast Completed");
            if (m_bBroadcastClose)
            {
                Debug.WriteLine("Close Broadcast Socket");
                m_cBroadcastSocket.Close();
                m_bBroadcastClose = false;
                return;
            }

            m_cBroadcastArgs = new SocketAsyncEventArgs();
            m_cBroadcastArgs.RemoteEndPoint = m_cBroadcastClientEndpoint;
            m_cBroadcastArgs.SetBuffer(m_abBroadcastReceiveBuffer, 0, m_nBroadcastReceiveBufferLength);
            m_cBroadcastArgs.Completed += BroadcastCompleted;
            m_cBroadcastSocket.ReceiveMessageFromAsync(m_cBroadcastArgs);

            byte bHeaderProtocol = cArgs.Buffer[0];
            Debug.WriteLine("Broadcast : " + bHeaderProtocol);
            if (bHeaderProtocol == Helper.SocketCommunicationProtocol.READY_FOR_WORK)
            {
                Debug.WriteLine("Try adding Node...");
                Helper.NodeInfos nodeInfos = new Helper.NodeInfos();
                Int32 nlen = BitConverter.ToInt32(cArgs.Buffer, 1);
                IntPtr i = Marshal.AllocHGlobal(nlen);
                Marshal.Copy(cArgs.Buffer, 5, i, nlen);
                nodeInfos = (Helper.NodeInfos)Marshal.PtrToStructure(i, nodeInfos.GetType());
                Marshal.FreeHGlobal(i);

                acAvailableNodes.Add(new Helper.Node((((IPEndPoint)cArgs.RemoteEndPoint).Address.ToString() + ":" + ((IPEndPoint)cArgs.RemoteEndPoint).Port.ToString()), nodeInfos));

                Debug.WriteLine("Added Node!");
            }
        }

        public void UpdateOwnThreads(int nThreadCount)
        {
            m_nThreadsInUse = nThreadCount;
            (Application.Current.MainWindow as MainWindow).tllbSystemInfo.Content = "Number of physical processors:\t" + m_nPhysicalProcessors.ToString() + "\nNumber of cores:\t\t\t" + m_nCores.ToString() + "\nNumber of logical processors:\t" + m_nLogicalProcessors.ToString() + "\nNumber of threads in use:\t\t" + m_nThreadsInUse.ToString();
        }
        
        private void GenerateVoronoiVectors(object sender, DoWorkEventArgs e)
        {
            double dOneStep = 100.0 / m_nThreadsInUse / (double)e.Argument;
            List<BenTools.Mathematics.Vector> acTempList = new List<BenTools.Mathematics.Vector>();

            for (int i = 0; i < (double)e.Argument; i++)
            {
                double[] adVector = new double[] { m_kRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum, m_kRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum };
                acTempList.Add(new BenTools.Mathematics.Vector(adVector));
                Application.Current.Dispatcher.Invoke(new Action(() => (Application.Current.MainWindow as MainWindow).LogVoronoiProgress(dOneStep)));
            }

            lock (m_cObjThreadLocking)
            {
                m_acVectors.Add(acTempList);
            }
        }

        private void GenerateVoronoiVectorsFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            m_nVoronoiFinished_Threaded++;
            if (e.Cancelled)
            {
                (Application.Current.MainWindow as MainWindow).AddLog("End process: Thread " + m_nVoronoiFinished_Threaded.ToString() + "/" + m_nThreadsInUse.ToString() + " Generation", LogLevel.WARN);
            }
            else if (e.Error != null)
            {
                (Application.Current.MainWindow as MainWindow).AddLog("End process: Thread " + m_nVoronoiFinished_Threaded.ToString() + "/" + m_nThreadsInUse.ToString() + " Generation\nERROR: " + e.Error.Message, LogLevel.ERROR);
            }
            else
            {
                (Application.Current.MainWindow as MainWindow).AddLog("End process: Thread " + m_nVoronoiFinished_Threaded.ToString() + "/" + m_nThreadsInUse.ToString() + " Generation", LogLevel.INFO);
            }
            if(m_nVoronoiFinished_Threaded >= m_nThreadsInUse)
            {
                (Application.Current.MainWindow as MainWindow).btnDrawVoronoi.IsEnabled = true;
                Application.Current.Dispatcher.Invoke(new Action(() => (Application.Current.MainWindow as MainWindow).GenerateVoronoiEnd()));
            }
        }

    }
}
