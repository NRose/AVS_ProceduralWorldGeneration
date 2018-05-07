using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WcfServiceLibrary1;

namespace AVS_ClientCommunication
{
    class Program
    {
        static void Main(string[] args)
        {
            StuffClass cInstance = new StuffClass();
            cInstance.StartStuff();
        }
    }

    public class StuffClass
    {
        private Socket udpSocket = null;
        private SocketAsyncEventArgs socketArgs = null;

        public void StartStuff()
        {
            Byte[] rawIp = { 192, 168, 0, 200 };
            Byte[] rawMask = { 255, 255, 0, 00 };

            //Console.WriteLine(Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString());
            //Console.WriteLine(BroadcastHelper.GetLocalIPAddress());

            IPAddress ipAddress = IPHelper.GetLocalIPAddress();
            IPAddress subnetMask = IPHelper.GetLocalSubmask();

            String query = "Hallo";
            byte length = (byte)System.Text.ASCIIEncoding.Unicode.GetByteCount(query);
            byte[] sendContent = new Byte[length];

            IPAddress broadcast = IPHelper.GetBroadcastAddress(ipAddress, subnetMask);
            Console.WriteLine(broadcast);

            try
            {
                udpSocket = IPHelper.SendBroadcastPacketToBroadcastIp(broadcast, 7345, sendContent);
                Byte[] receiveBuffer = new Byte[12];

                Console.WriteLine("Send");

                //udpSocket.Receive(new Byte[10], SocketFlags.None);
                socketArgs = new SocketAsyncEventArgs();
                socketArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);

                Console.WriteLine("wait");
                Thread.Sleep(10000);

                udpSocket.ReceiveAsync(socketArgs);
                Console.WriteLine("Received");
                foreach(Byte line in receiveBuffer)
                {
                    Console.WriteLine(line);
                }

            }
            finally
            {
                if (udpSocket != null)
                {
                    udpSocket.Close();
                }
            }

            Console.ReadLine();
        }
    }

}
