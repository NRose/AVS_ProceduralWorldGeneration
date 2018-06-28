using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TestClientServer
{
    class Program
    {
        static void Main(string[] args)
        {
            SocketCommunicationListener serverSocket = new SocketCommunicationListener(7345,1024);

            while (true) {
                serverSocket.Listen();
            }
            

        }
    }
}
