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

namespace TestClientServer
{
    public static class SocketCommunicationProtocol
    {
        public static byte SEARCH_FOR_NODES = 127;
        public static byte READY_FOR_WORK = 93;
        public static byte START_WCF_SERVICE = 101;
    }
    public class SocketCommunicationListener
    {
        private Socket socket;
        private byte[] receiveBuffer;
        private int port;
        private int receiveBufferLength;
        private EndPoint serverEndpoint;

        public SocketCommunicationListener(int port, int receiveBufferLength)
        {
            this.port = port;
            this.receiveBufferLength = receiveBufferLength;
            receiveBuffer = new Byte[receiveBufferLength];

            serverEndpoint = new IPEndPoint(IPAddress.Any, port);

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            socket.Bind(serverEndpoint);
        }

        public void Listen()
        {
            Console.WriteLine("Start Server!");
            SocketFlags socketFlags = new SocketFlags();
            IPPacketInformation ipPacketInformation;
            EndPoint clientEndpoint = new IPEndPoint(((IPEndPoint)serverEndpoint).Address, ((IPEndPoint)serverEndpoint).Port);
            Console.WriteLine("Waiting for a client, listen on port " + port + "...");
            socket.ReceiveMessageFrom(receiveBuffer, 0, receiveBufferLength, ref socketFlags, ref clientEndpoint, out ipPacketInformation);
            
            

           //String receiveText = System.Text.ASCIIEncoding.Unicode.GetString(receiveBuffer);
            //Console.WriteLine("Received message: " + receiveText);
            
            if (receiveBuffer[0] == SocketCommunicationProtocol.SEARCH_FOR_NODES)
            {
                Console.WriteLine("Known Protocol: SEARCH_FOR_NODES");
                showInformation(((IPEndPoint)clientEndpoint).Address, ((IPEndPoint)clientEndpoint).Port);

                Console.WriteLine("Send answer to client...");
                SendAnswer(((IPEndPoint)clientEndpoint).Address, ((IPEndPoint)clientEndpoint).Port);
            }
            else if (receiveBuffer[0] == SocketCommunicationProtocol.START_WCF_SERVICE)
            {
                Console.WriteLine("Known Protocol: START_WCF_SERVICE");
                showInformation(((IPEndPoint)clientEndpoint).Address, ((IPEndPoint)clientEndpoint).Port);
                startWcfService();
            }
            else
            {
                Console.WriteLine("Unknown Protocol");
            }
        }

        public void startWcfService()
        {
            // Start WCF Service
            string address = "http://localhost:8733/VoronoiGenerationService";
            Uri uri = new Uri("http://localhost:8733/VoronoiGenerationService");

            using (ServiceHost host = new ServiceHost(typeof(VoronoiGenerationService), uri))
            {
               // host.AddServiceEndpoint(typeof(IVoronoiGenerationService), new BasicHttpBinding(), address);
                host.Open();

                Console.WriteLine("Service Started");
                Console.ReadLine();
            }
        }

        private void SendAnswer(IPAddress ipAddress, int port)
        {
            var destinationendpoint = new IPEndPoint(ipAddress, port);

            NodeInfos node = ReadNodeInfos();

            byte[] nodeByte = StructureToByteArray(node,SocketCommunicationProtocol.READY_FOR_WORK);
            
            socket.SendTo(nodeByte, destinationendpoint);
            Console.WriteLine("Answer: READY_FOR_WORK + NodeInfos");
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

        private void showInformation(IPAddress ipAddress, int port)
        {
            Console.WriteLine("Information of client:");
            Console.WriteLine("IP-Adresse ist: " + ipAddress + ":" + port);

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
