using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceModel;
using AVS_WorldGeneration.WcfCommunication;
using System.Runtime.InteropServices;
using System.Management;
using System.ServiceProcess;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace AVS_NodeCommunication
{
    public static class SocketCommunicationProtocol
    {
        public static byte SEARCH_FOR_NODES = 127;
        public static byte READY_FOR_WORK = 93;
        public static byte START_WCF_SERVICE = 101;
        public static byte GENERATE_VORONOI = 156;
        public static byte SEND_VECTORS_BACK = 189;
    }
    
    public struct NodeCalculationData
    {
        public double dMinimum;
        public double dMaximum;
        public byte bThreads;
        public int nCount;
        public int nSeed;
        public int nRandomStart;
        public Guid cID;
    }

    public struct ThreadObject
    {
        public int nCount;
        public int nRandomStart;
    }

    public class SocketCommunicationListener
    {
        private Socket socket;
        private byte[] receiveBuffer;
        private int port;
        private int receiveBufferLength;
        private EndPoint serverEndpoint;

        private EndPoint m_cRemoteEndpoint;
        private IPEndPoint m_cRemoteEndpointForAnswer;

        private ServiceHost m_cServiceHost;

        #region Voronoi Generation

        private Guid m_cID;

        private double m_dMinimum;
        private double m_dMaximum;

        private int m_nSeed;

        private byte m_bThreads;
        private Object m_cObjThreadLocking = new Object();
        private Object m_cObjThreadLockingAnswer = new Object();
        private byte m_bThreadsFinished;

        private List<List<double[]>> m_acVectors = new List<List<double[]>>();

        #endregion

        public SocketCommunicationListener(int port, int receiveBufferLength)
        {
            this.port = port;
            this.receiveBufferLength = receiveBufferLength;
            receiveBuffer = new Byte[receiveBufferLength];

            serverEndpoint = new IPEndPoint(IPAddress.Any, port);

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            socket.Bind(serverEndpoint);
            
            m_cRemoteEndpoint = new IPEndPoint(((IPEndPoint)serverEndpoint).Address, ((IPEndPoint)serverEndpoint).Port);
        }

        private EventLog m_cEvent;

        public void Listen(EventLog cEvent)
        {
            m_cEvent = cEvent;
            cEvent.WriteEntry("Defined");
            //socket.ReceiveTimeout = 500;

            SocketAsyncEventArgs cArgs = new SocketAsyncEventArgs();
            cArgs.SetBuffer(receiveBuffer, 0, receiveBufferLength);
            cArgs.SocketFlags = new SocketFlags();
            cArgs.RemoteEndPoint = m_cRemoteEndpoint;
            cArgs.Completed += CArgs_Completed;
            
            socket.ReceiveMessageFromAsync(cArgs);
            
            cEvent.WriteEntry("Defined End");
        }

        private void CArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (receiveBuffer.Length > 0)
            {
                if (receiveBuffer[0] == SocketCommunicationProtocol.SEARCH_FOR_NODES)
                {
                    SendAnswer(SocketCommunicationProtocol.READY_FOR_WORK, ((IPEndPoint)e.RemoteEndPoint).Address, ((IPEndPoint)e.RemoteEndPoint).Port);
                }
                else if (receiveBuffer[0] == SocketCommunicationProtocol.GENERATE_VORONOI)
                {
                    m_cEvent.WriteEntry("Start generating Voronoi via Sockets");

                    byte[] abData = new byte[receiveBuffer[1]];
                    Buffer.BlockCopy(receiveBuffer, 2, abData, 0, Convert.ToInt32(receiveBuffer[1]));

                    NodeCalculationData cData = GetNodeCalculationData(abData);

                    m_cEvent.WriteEntry("Calculation Stats:\nGeneration minimum: " + cData.dMinimum.ToString() + "\nGeneration maximum: " + cData.dMaximum.ToString() + "\nThreads to use: " + cData.bThreads.ToString() + "\nAmount of data to calculate: " + cData.nCount.ToString());

                    m_dMinimum = cData.dMinimum;
                    m_dMaximum = cData.dMaximum;
                    m_bThreads = cData.bThreads;
                    m_cID = cData.cID;
                    m_bThreadsFinished = 0;
                    m_nSeed = cData.nSeed;
                    int nCountPerThread = cData.nCount / cData.bThreads;
                    int nExtra = cData.nCount % cData.bThreads;
                    int nStartPosition = 0;

                    m_cRemoteEndpointForAnswer = (IPEndPoint)e.RemoteEndPoint;

                    m_cEvent.WriteEntry("Calculation Settings:\nAmount per Thread: " + nCountPerThread.ToString() + "\nExtra Amount for first thread: " + nExtra.ToString());

                    for(byte i = 0; i < cData.bThreads; i++)
                    {
                        BackgroundWorker cWorker = new BackgroundWorker
                        {
                            WorkerReportsProgress = false,
                            WorkerSupportsCancellation = false
                        };

                        cWorker.DoWork += GenerateVoronoiVectors;
                        cWorker.RunWorkerCompleted += GenerateVoronoiVectorsFinished;

                        ThreadObject cThreadArgs = new ThreadObject();
                        cThreadArgs.nCount = nCountPerThread + nExtra;
                        cThreadArgs.nRandomStart = nStartPosition;

                        cWorker.RunWorkerAsync(cThreadArgs);
                        nExtra = 0;
                        nStartPosition += cThreadArgs.nCount;

                        m_cEvent.WriteEntry("Started Thread " + (i + 1).ToString() + "/" + cData.bThreads.ToString());
                    }
                }
            }

            SocketAsyncEventArgs cArgs = new SocketAsyncEventArgs();
            cArgs.SetBuffer(receiveBuffer, 0, receiveBufferLength);
            cArgs.SocketFlags = new SocketFlags();
            cArgs.RemoteEndPoint = m_cRemoteEndpoint;
            cArgs.Completed += CArgs_Completed;

            socket.ReceiveMessageFromAsync(cArgs);
        }
        
        private void SendAnswer(byte protocol, IPAddress ipAddress, int port)
        {
            m_cEvent.WriteEntry("Start sending answer...");
            var destinationendpoint = new IPEndPoint(ipAddress, port);

            if (protocol == SocketCommunicationProtocol.READY_FOR_WORK)
            {
                m_cEvent.WriteEntry("Packing and parsing node info...");
                NodeInfos node = ReadNodeInfos();
                byte[] nodeByte = StructureToByteArray(node, SocketCommunicationProtocol.READY_FOR_WORK);
                m_cEvent.WriteEntry("Sending node info...");
                socket.SendTo(nodeByte, destinationendpoint);
            }
            else if (protocol == SocketCommunicationProtocol.SEND_VECTORS_BACK)
            {
                byte[] abResultData = GetResultArray(m_acVectors);
                byte[] abResultArray = new byte[abResultData.Length + 1];
                abResultArray[0] = SocketCommunicationProtocol.SEND_VECTORS_BACK;
                Buffer.BlockCopy(abResultData, 0, abResultArray, 1, abResultData.Length);

                socket.SendTo(abResultArray, destinationendpoint);
                m_cEvent.WriteEntry("Sending Result Byte Array back to Application!\nArray Length: " + (abResultData.Length + 1).ToString());
            }
            m_cEvent.WriteEntry("END sending answer!");
        }

        private NodeInfos ReadNodeInfos()
        {
            NodeInfos node = new NodeInfos();

            foreach (var item in new ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
            {
                node.bProcessorsPhysical = byte.Parse(item["NumberOfProcessors"].ToString());
                node.bProcessorsLogical = byte.Parse(item["NumberOfLogicalProcessors"].ToString());
            }
            foreach (var item in new ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                node.bCores = byte.Parse(item["NumberOfCores"].ToString());
            }

            return node;
        }
        
        private byte[] StructureToByteArray(object obj, byte protocol)
        {
            Int32 nlen = Marshal.SizeOf(obj);
            byte[] arr = new byte[nlen + 5];
            arr[0] = protocol;
            byte[] abLen = BitConverter.GetBytes(nlen);
            abLen.CopyTo(arr, 1);


            IntPtr ptr = Marshal.AllocHGlobal(nlen);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 5, nlen);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        private NodeCalculationData GetNodeCalculationData(byte[] cArray)
        {
            NodeCalculationData cData = new NodeCalculationData();

            int nSize = Marshal.SizeOf(cData);
            IntPtr cPointer = Marshal.AllocHGlobal(nSize);

            Marshal.Copy(cArray, 0, cPointer, nSize);

            cData = (NodeCalculationData)Marshal.PtrToStructure(cPointer, cData.GetType());
            Marshal.FreeHGlobal(cPointer);

            return cData;
        }

        private byte[] GetResultArray(List<List<double[]>> acResults)
        {
            try
            {
                NodeResult cResult = new NodeResult();
                cResult.cID = m_cID;
                cResult.acVectors = acResults;

                BinaryFormatter cFormatter = new BinaryFormatter();
                MemoryStream cStream = new MemoryStream();
                cFormatter.Serialize(cStream, cResult);

                return cStream.ToArray();
            }
            catch(Exception cEx)
            {
                m_cEvent.WriteEntry("ERROR: " + cEx.Message);
            }
            return null;
        }
        
        private void GenerateVoronoiVectors(object sender, DoWorkEventArgs e)
        {
            ThreadObject cThreadArgs = (ThreadObject)(e.Argument);
            Random cRnd = new Random(m_nSeed);
            
            for (int i = 0; i < cThreadArgs.nRandomStart; i++)
            {
                double dTemp = cRnd.NextDouble();
            }
            
            double dOneStep = 100.0 / m_bThreads / (double)cThreadArgs.nCount;
            List<double[]> acTempList = new List<double[]>();
            
            for (int i = 0; i < cThreadArgs.nCount; i++)
            {
                double[] adVector = new double[] { cRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum, cRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum };
                acTempList.Add(adVector);
            }
            
            lock (m_cObjThreadLocking)
            {
                m_acVectors.Add(acTempList);
            }
        }

        private void GenerateVoronoiVectorsFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            lock (m_cObjThreadLockingAnswer)
            {
                m_bThreadsFinished++;
                if (e.Cancelled)
                {
                    m_cEvent.WriteEntry("Thread " + m_bThreadsFinished.ToString() + "/" + m_bThreads.ToString() + " CANCELLED ");
                }
                else if (e.Error != null)
                {
                    m_cEvent.WriteEntry("Thread " + m_bThreadsFinished.ToString() + "/" + m_bThreads.ToString() + " ERROR: " + e.Error.Message);
                }
                else
                {
                    m_cEvent.WriteEntry("Thread " + m_bThreadsFinished.ToString() + "/" + m_bThreads.ToString() + " SUCCESSFULLY FINISHED!");
                }
                if (m_bThreadsFinished == m_bThreads)
                {
                    m_cEvent.WriteEntry("All Threads ended!");
                    SendAnswer(SocketCommunicationProtocol.SEND_VECTORS_BACK, m_cRemoteEndpointForAnswer.Address, m_cRemoteEndpointForAnswer.Port);
                }
            }
        }
    }

    public struct NodeInfos
    {
        public byte bCores { get; set; }
        public byte bProcessorsPhysical { get; set; }
        public byte bProcessorsLogical { get; set; }
    }

    [Serializable]
    public struct NodeResult
    {
        public Guid cID;
        public List<List<double[]>> acVectors;
    }
}
