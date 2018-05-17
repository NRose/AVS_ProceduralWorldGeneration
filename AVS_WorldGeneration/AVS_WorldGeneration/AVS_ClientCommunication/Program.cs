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
            //Byte[] rawIp = { 192, 168, 0, 200 };
            //Byte[] rawMask = { 255, 255, 0, 00 };

            //Console.WriteLine(Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString());
            //Console.WriteLine(BroadcastHelper.GetLocalIPAddress());

            IPAddress ipAddress = IPHelper.GetLocalIPAddress();
            IPAddress subnetMask = IPHelper.GetLocalSubmask();

            String query = "HELLO. SEARCHING FOR DISTRIBUTED SYSTEM!";
            int length = 1024;
            byte[] sendContent = new Byte[length];
            sendContent = System.Text.ASCIIEncoding.Unicode.GetBytes(query);

            IPAddress broadcast = IPHelper.GetBroadcastAddress(ipAddress, subnetMask);
            Console.WriteLine(broadcast);

            try
            {
                Console.WriteLine("Send broadcast");

                SocketCommunicationSender serverSocket = new SocketCommunicationSender(broadcast, 7345, 1024);
                
                Socket socket = serverSocket.Send();


                //clientSocket.Listen();
                //udpSocket = IPHelper.SendBroadcastPacketToBroadcastIp(broadcast, 7345, sendContent);
                //udpSocket.Bind(new IPEndPoint())
                //Byte[] receiveBuffer = new Byte[length];

                //udpSocket.Receive(receiveBuffer);

                //String receiveText = System.Text.ASCIIEncoding.Unicode.GetString(receiveBuffer);
                //Console.WriteLine("Received message: " + receiveText);

                //Thread.Sleep(10000);

                //sendContent = System.Text.ASCIIEncoding.Unicode.GetBytes("HELLO. SEARCHING FOR DISTRIBUTED SYSTEM!");
                //udpSocket = IPHelper.SendBroadcastPacketToBroadcastIp(broadcast, 7345, sendContent);
                



                //Console.WriteLine("Send");

                ////udpSocket.Receive(new Byte[10], SocketFlags.None);
                //socketArgs = new SocketAsyncEventArgs();
                //socketArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
                //udpSocket.ReceiveAsync(socketArgs);



                Console.ReadLine();


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
