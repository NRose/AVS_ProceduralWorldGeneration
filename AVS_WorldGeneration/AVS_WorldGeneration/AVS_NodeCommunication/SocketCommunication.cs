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

    public class SocketCommunicationListener
    {
        private Socket socket;
        private byte[] receiveBuffer;
        private int port;
        private int receiveBufferLength;
        private EndPoint serverEndpoint;
        
        private EndPoint m_cRemoteEndpoint;

        private ServiceHost m_cServiceHost;

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
                else if (receiveBuffer[0] == SocketCommunicationProtocol.START_WCF_SERVICE)
                {
                    StartWcfService();
                }
                else if (receiveBuffer[0] == SocketCommunicationProtocol.GENERATE_VORONOI)
                {
                    // toDo Generate Voronoi
                    SendAnswer(SocketCommunicationProtocol.SEND_VECTORS_BACK, ((IPEndPoint)e.RemoteEndPoint).Address,  ((IPEndPoint)e.RemoteEndPoint).Port);
                }
            }

            SocketAsyncEventArgs cArgs = new SocketAsyncEventArgs();
            cArgs.SetBuffer(receiveBuffer, 0, receiveBufferLength);
            cArgs.SocketFlags = new SocketFlags();
            cArgs.RemoteEndPoint = m_cRemoteEndpoint;
            cArgs.Completed += CArgs_Completed;

            socket.ReceiveMessageFromAsync(cArgs);
        }

        public void StartWcfService()
        {
            if(m_cServiceHost == null)
            {
                m_cEvent.WriteEntry("Start WCF Service");
                m_cServiceHost = new ServiceHost(typeof(VoronoiGenerationService));
                m_cEvent.WriteEntry("Created ServiceHost for WCF Service");
                m_cServiceHost.Open();
                m_cEvent.WriteEntry("Opened WCF Service");
            }
        }

        private void SendAnswer(byte protocol, IPAddress ipAddress, int port)
        {
            var destinationendpoint = new IPEndPoint(ipAddress, port);

            if (protocol == SocketCommunicationProtocol.READY_FOR_WORK)
            {
                
                NodeInfos node = ReadNodeInfos();

                byte[] nodeByte = StructureToByteArray(node, SocketCommunicationProtocol.READY_FOR_WORK);

                socket.SendTo(nodeByte, destinationendpoint);
                //Console.WriteLine("Answer: READY_FOR_WORK + NodeInfos");
            }else if (protocol == SocketCommunicationProtocol.SEND_VECTORS_BACK)
            {
                // Generate Voronois
                String test = "hallooo";
                byte[] nodeByte = StructureToByteArray(test, SocketCommunicationProtocol.SEND_VECTORS_BACK);
                socket.SendTo(nodeByte, destinationendpoint);
            }

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

    }

    public struct NodeInfos
    {
        public byte bCores { get; set; }
        public byte bProcessorsPhysical { get; set; }
        public byte bProcessorsLogical { get; set; }
    }
}
