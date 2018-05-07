using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace AVS_NodeCommunication
{
    public partial class Service1 : ServiceBase
    {
        /*
         * Klasse
IP Liste
Anzahl Prozessoren
Anzahl Cores
Anzahl Threads
IP Addresse
Computername
         */
        private SocketAsyncEventArgs socketArgs;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            String query = "Hallo";
            byte length = (byte)System.Text.ASCIIEncoding.Unicode.GetByteCount(query);
            byte[] receiveBuffer = new Byte[length];

            Socket socket = null;

            Byte[] rawIp = { 127, 0, 0, 1 };
            IPAddress ipAddress = new IPAddress(rawIp);

            var destinationEndpoint = new IPEndPoint(ipAddress,7345);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

            socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            socket.Receive(receiveBuffer,SocketFlags.None);
            Console.WriteLine("received");

        }

        protected override void OnStop()
        {
        }
    }
}
