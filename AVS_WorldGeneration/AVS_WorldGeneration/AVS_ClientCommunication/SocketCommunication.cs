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

        public Socket Send()
        {
            Console.WriteLine("Start Client!");

            byte[] sendContent = new Byte[receiveBufferLength];
            sendContent = System.Text.ASCIIEncoding.Unicode.GetBytes(SocketCommunicationProtocol.SEARCH_FOR_NODES);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = clientEndpoint;
            args.SetBuffer(receiveBuffer, 0, receiveBufferLength);

            Console.WriteLine("Start Listen for answer on ip:" + ((IPEndPoint)clientEndpoint).Address + " and port: " + ((IPEndPoint)clientEndpoint).Port);
            socket.ReceiveMessageFromAsync(args);
            Console.WriteLine("Send request to ip: " + ((IPEndPoint)serverEndpoint).Address + " and port: " + ((IPEndPoint)serverEndpoint).Port);

            socket.SendTo(sendContent, serverEndpoint);
            
            Console.WriteLine("Waiting for an answer ...");
            Thread.Sleep(100);
            
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

            return socket;
        } 

        private void showInformation(IPPacketInformation ipPacketInformation)
        {
            Console.WriteLine("Information of client:");
            Console.WriteLine("IP-Adresse ist: " + ipPacketInformation.Address + ":" + ipPacketInformation.Interface);
            Console.WriteLine("Send Answer to client ...");
        }
    }
}
