using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestClientServer
{
    public static class SocketCommunicationProtocol
    {
        public static string SEARCH_FOR_NODES = "HELLO. SEARCHING FOR DISTRIBUTED SYSTEM!";
        public static string READY_FOR_WORK = "HELLO. READY FOR DISTRIBUTED SYSTEM!";
    }
    class SocketCommunicationListener
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
            
            String receiveText = System.Text.ASCIIEncoding.Unicode.GetString(receiveBuffer);
            Console.WriteLine("Received message: " + receiveText);

            if (receiveText.Contains(SocketCommunicationProtocol.SEARCH_FOR_NODES))
            {
                Console.WriteLine("Known Protocol");
                showInformation(((IPEndPoint)clientEndpoint).Address, ((IPEndPoint)clientEndpoint).Port);
                
                Console.WriteLine("Send answer to client...");
                SendAnswer(((IPEndPoint)clientEndpoint).Address, ((IPEndPoint)clientEndpoint).Port);
            }
            else
            {
                Console.WriteLine("Unknown Protocol");
            }
        }

        private void SendAnswer(IPAddress ipAddress, int port)
        {
            var destinationendpoint = new IPEndPoint(ipAddress, port);

            socket.SendTo(System.Text.ASCIIEncoding.Unicode.GetBytes(SocketCommunicationProtocol.READY_FOR_WORK), destinationendpoint);
            Console.WriteLine("Answer:" + SocketCommunicationProtocol.READY_FOR_WORK);
        }

        private void showInformation(IPAddress ipAddress, int port)
        {
            Console.WriteLine("Information of client:");
            Console.WriteLine("IP-Adresse ist: " + ipAddress + ":" + port);

        }

    }
}
