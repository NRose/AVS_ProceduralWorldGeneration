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
        private int m_nSeed = 0;

        private Dictionary<Guid, List<List<double[]>>> m_dicResults = new Dictionary<Guid, List<List<double[]>>>();
        private List<Guid> m_acIndexes = new List<Guid>();

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

        #region Communication Socket

        private List<Socket> m_acCommunicationSockets;
        private EndPoint m_cCommunicationApplicationEndpoint;
        private List<EndPoint> m_acCommunicationServiceEndPoints;
        private List<byte[]> m_aabCommunicationReceiveBuffers;
        private int m_nCommunicationReceiveBufferLength;
        private List<SocketAsyncEventArgs> m_acCommunicationArgs;
        private List<bool> m_abCommunicationClose;

        #endregion

        public DistributionManager(double dMinimum, double dMaximum)
        {
            m_dMinimum = dMinimum;
            m_dMaximum = dMaximum;

            acAvailableNodes = new List<Helper.Node>();
            
            m_nSeed = new Random().Next();
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

        public void InitializeCommunication()
        {
            m_nCommunicationReceiveBufferLength = 1024;
            m_cCommunicationApplicationEndpoint = new IPEndPoint(IPHelper.GetLocalIPAddress(), 55261);

            m_acCommunicationSockets = new List<Socket>();
            m_acCommunicationServiceEndPoints = new List<EndPoint>();
            m_aabCommunicationReceiveBuffers = new List<byte[]>();
            m_acCommunicationArgs = new List<SocketAsyncEventArgs>();
            m_abCommunicationClose = new List<bool>();
        }

        public void SendCalculationDataToNode(IPAddress cIPAddress, Helper.NodeCalculationData cData)
        {
            m_cBroadcastSocket.Close();
            try
            {
                Helper.NodeResult cResult = new Helper.NodeResult();
                cResult.cID = Guid.NewGuid();
                cResult.acVectors = new List<List<double[]>>();

                int nLoopsPerThread = cData.nCount / cData.bThreads;
                int nExtraLoops = cData.nCount % cData.bThreads;

                for(int nThreadCount = 0; nThreadCount < cData.bThreads; nThreadCount++)
                {
                    cResult.acVectors.Add(new List<double[]>());
                    int nLoops = nLoopsPerThread + nExtraLoops;
                    for (int nLoopCount = 0; nLoopCount < nLoops; nLoopCount++)
                    {
                        cResult.acVectors[nThreadCount].Add(new double[2]);
                    }
                    nExtraLoops = 0;
                }

                int nCommunicationReceiveBufferLength = Helper.GetBytes(cResult).Length;
                Application.Current.Dispatcher.Invoke(new Action(() => (Application.Current.MainWindow as MainWindow).AddLog("Estimated receive buffer length: " + (nCommunicationReceiveBufferLength + 1).ToString(), LogLevel.INFO)));

                m_aabCommunicationReceiveBuffers.Add(new byte[nCommunicationReceiveBufferLength + 1]);
                m_acCommunicationServiceEndPoints.Add(new IPEndPoint(cIPAddress, 7345));
                m_abCommunicationClose.Add(false);

                m_acCommunicationSockets.Add(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp));
                m_acCommunicationSockets.Last().SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                m_acCommunicationSockets.Last().Bind(m_cCommunicationApplicationEndpoint);

                byte[] abCommunicationContent = new byte[m_nCommunicationReceiveBufferLength];
                abCommunicationContent[0] = Helper.SocketCommunicationProtocol.GENERATE_VORONOI;
                byte[] abData = Helper.GetBytes(cData);
                if(abData.Length > 255)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() => (Application.Current.MainWindow as MainWindow).AddLog("Byte Array Length is over 255!", LogLevel.WARN)));
                }
                abCommunicationContent[1] = Convert.ToByte(abData.Length);
                System.Buffer.BlockCopy(abData, 0, abCommunicationContent, 2, abData.Length);

                m_acCommunicationArgs.Add(new SocketAsyncEventArgs());
                m_acCommunicationArgs.Last().RemoteEndPoint = m_cCommunicationApplicationEndpoint;
                m_acCommunicationArgs.Last().SetBuffer(m_aabCommunicationReceiveBuffers.Last(), 0, nCommunicationReceiveBufferLength + 1);

                m_acCommunicationArgs.Last().Completed += ResultCompleted;

                m_acCommunicationSockets.Last().ReceiveMessageFromAsync(m_acCommunicationArgs.Last());
                m_acCommunicationSockets.Last().SendTo(abCommunicationContent, m_acCommunicationServiceEndPoints.Last());

                Application.Current.Dispatcher.Invoke(new Action(() => (Application.Current.MainWindow as MainWindow).AddLog("Start process: Service " + m_acCommunicationSockets.Count.ToString(), LogLevel.INFO)));

                Debug.WriteLine("Sended data to " + cIPAddress.ToString());
            }
            catch (Exception cException)
            {
                Debug.WriteLine("Couldn't create communication socket: " + cException.Message);
            }
        }

        private int m_nResultsReceived = 0;

        private void ResultCompleted(object cSender, SocketAsyncEventArgs cArgs)
        {
            byte bHeaderProtocol = cArgs.Buffer[0];

            if (bHeaderProtocol == Helper.SocketCommunicationProtocol.SEND_VECTORS_BACK)
            {
                Application.Current.Dispatcher.Invoke(new Action(() => (Application.Current.MainWindow as MainWindow).AddLog("Received results from Service " + (m_nResultsReceived + 1).ToString(), LogLevel.INFO)));

                byte[] acResults = new byte[cArgs.Buffer.Length - 1];
                Buffer.BlockCopy(cArgs.Buffer, 1, acResults, 0, cArgs.Buffer.Length - 1);

                Helper.NodeResult cResult = Helper.GetNodeResult(acResults);

                m_dicResults[cResult.cID] = cResult.acVectors;
                m_nResultsReceived++;
                
                if (m_nResultsReceived >= acAvailableNodes.FindAll(o => o.bInUse).Count)
                {
                    m_acVectors = new List<List<BenTools.Mathematics.Vector>>();
                    for(int i = 0; i < m_acIndexes.Count; i++)
                    {
                        foreach(List<double[]> cListPair in m_dicResults[m_acIndexes[i]])
                        {
                            List<BenTools.Mathematics.Vector> acVectors = new List<BenTools.Mathematics.Vector>();
                            foreach (double[] acData in cListPair)
                            {
                                acVectors.Add(new BenTools.Mathematics.Vector(acData));
                            }
                            m_acVectors.Add(acVectors);
                        }
                    }

                    Application.Current.Dispatcher.Invoke(new Action(() => (Application.Current.MainWindow as MainWindow).AddLog("Received all results from all Services!", LogLevel.INFO)));
                    Application.Current.Dispatcher.Invoke(new Action(() => (Application.Current.MainWindow as MainWindow).GenerateVoronoiEnd()));
                }
            }
        }

        public void DistributeWork(int nSeed, double dVoronoiCount)
        {
            m_nSeed = nSeed;
            
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

                    Helper.ThreadOpts cOpts = new Helper.ThreadOpts();
                    cOpts.dLoops = dVoronoiCount / m_nThreadsInUse + dExtra;
                    cOpts.nStartNumber = i;

                    cWorker.DoWork += GenerateVoronoiVectors;
                    cWorker.RunWorkerCompleted += GenerateVoronoiVectorsFinished;

                    cWorker.RunWorkerAsync(cOpts);
                    dExtra = 0.0;
                }
            }
            else
            {
                // NETWORK DISTRIBUTED
                InitializeCommunication();

                List<Helper.Node> acDistributors = acAvailableNodes.FindAll(o => o.bInUse);

                int nCountPerDistributor = Convert.ToInt32(dVoronoiCount / acDistributors.Count);
                int nExtra = Convert.ToInt32(dVoronoiCount % acDistributors.Count);
                int nRandomStart = 0;

                for(int i = 0; i < acDistributors.Count; i++)
                {
                    Helper.NodeCalculationData cData = new Helper.NodeCalculationData();
                    cData.dMinimum = m_dMinimum;
                    cData.dMaximum = m_dMaximum;
                    cData.bThreads = acDistributors[i].bThreads;
                    cData.nCount = nCountPerDistributor + nExtra;
                    cData.nSeed = m_nSeed;
                    cData.nRandomStart = nRandomStart;
                    cData.cID = Guid.NewGuid();
                    nExtra = 0;
                    nRandomStart += cData.nCount;

                    m_dicResults.Add(cData.cID, null);
                    m_acIndexes.Add(cData.cID);

                    SendCalculationDataToNode(acDistributors[i].cIPAddress, cData);
                }
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

                acAvailableNodes.Add(new Helper.Node((((IPEndPoint)cArgs.RemoteEndPoint).Address.ToString() + ":" + ((IPEndPoint)cArgs.RemoteEndPoint).Port.ToString()), ((IPEndPoint)cArgs.RemoteEndPoint).Address, nodeInfos));

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
            Helper.ThreadOpts cOpts = (Helper.ThreadOpts)e.Argument;

            Random cRnd = new Random(m_nSeed + cOpts.nStartNumber);

            double dOneStep = 100.0 / m_nThreadsInUse / cOpts.dLoops;
            List<BenTools.Mathematics.Vector> acTempList = new List<BenTools.Mathematics.Vector>();

            for (int i = 0; i < cOpts.dLoops; i++)
            {
                double[] adVector = new double[] { cRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum, cRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum };
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
                Application.Current.Dispatcher.Invoke(new Action(() => (Application.Current.MainWindow as MainWindow).GenerateVoronoiEnd()));
            }
        }

    }
}
