using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WcfServiceLibrary1;

namespace AVS_ClientCommunication
{
    public static class SocketCommunicationProtocol
    {
        public static string SEARCH_FOR_NODES = "HELLO. SEARCHING FOR DISTRIBUTED SYSTEM!";
        public static string READY_FOR_WORK = "HELLO. READY FOR DISTRIBUTED SYSTEM!";
    }
    public class SocketCommunicationSender
    {
        private Socket socket;
        private byte[] receiveBuffer;
        private int srcPort = 55261;
        private int receiveBufferLength;
        private EndPoint serverEndpoint;
        private int destPort;
        private IPAddress destIpAddress;
        private EndPoint clientEndpoint;

        public SocketCommunicationSender(IPAddress ipAddress, int port, int receiveBufferLength)
        {
            this.destIpAddress = ipAddress;
            this.destPort = port;
            this.receiveBufferLength = receiveBufferLength;
            receiveBuffer = new Byte[receiveBufferLength];

            serverEndpoint = new IPEndPoint(ipAddress, port);
            clientEndpoint = new IPEndPoint(IPHelper.GetLocalIPAddress(), srcPort);

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            socket.Bind(clientEndpoint);

        }
        private SocketAsyncEventArgs args;
        public Socket Send()
        {
            Console.WriteLine("Start Client!");

            byte[] sendContent = new Byte[receiveBufferLength];
            sendContent = System.Text.ASCIIEncoding.Unicode.GetBytes(SocketCommunicationProtocol.SEARCH_FOR_NODES);

            args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = clientEndpoint;
            args.SetBuffer(receiveBuffer, 0, receiveBufferLength);
            args.Completed += Args_Completed;

            Console.WriteLine("Start Listen for answer on ip:" + ((IPEndPoint)clientEndpoint).Address + " and port: " + ((IPEndPoint)clientEndpoint).Port);
            socket.ReceiveMessageFromAsync(args);
            Console.WriteLine("Send request to ip: " + ((IPEndPoint)serverEndpoint).Address + " and port: " + ((IPEndPoint)serverEndpoint).Port);

            socket.SendTo(sendContent, serverEndpoint);

            Console.WriteLine("Waiting for an answer ...");
            Thread.Sleep(10000);
            Console.WriteLine("Should receive an answer ...");

            foreach (string sItem in m_asReceivedItems)
            {
                Console.WriteLine(sItem);
            }

            /*
            String receiveText = System.Text.ASCIIEncoding.Unicode.GetString(receiveBuffer);
            Console.WriteLine("Received message: " + receiveText);

            if (receiveText.Contains(SocketCommunicationProtocol.READY_FOR_WORK))
            {
                Console.WriteLine("Known Protocol");
            }
            else
            {
                Console.WriteLine("Unknown Protocol");
            }
            */
            return socket;
        }

        private List<string> m_asReceivedItems = new List<string>();

        private void Args_Completed(object sender, SocketAsyncEventArgs e)
        {
            args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = clientEndpoint;
            args.SetBuffer(receiveBuffer, 0, receiveBufferLength);
            args.Completed += Args_Completed;
            socket.ReceiveMessageFromAsync(args);

            if (System.Text.ASCIIEncoding.Unicode.GetString(e.Buffer).Contains(SocketCommunicationProtocol.READY_FOR_WORK))
            {
                m_asReceivedItems.Add(((IPEndPoint)e.RemoteEndPoint).Address.ToString() + ":" + ((IPEndPoint)e.RemoteEndPoint).Port.ToString());
                
            }
        }

        private void showInformation(IPPacketInformation ipPacketInformation)
        {
            Console.WriteLine("Information of client:");
            Console.WriteLine("IP-Adresse ist: " + ipPacketInformation.Address + ":" + ipPacketInformation.Interface);
            Console.WriteLine("Send Answer to client ...");
        }
    }
}
