using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AVS_WorldGeneration
{
    class NetworkManager
    {
        private Socket m_cUDPSocket = null;
        private SocketAsyncEventArgs m_cSocketArgs = null;

        public void InitializeNetworkManager()
        {
            IPAddress cIPAddress = IPHelper.GetLocalIPAddress();
            IPAddress cSubnetMask = IPHelper.GetLocalSubmask();
            
            int nLength = 1024;
            byte[] abContent = new byte[nLength];
            abContent[0] = Helper.SocketCommunicationProtocol.SEARCH_FOR_NODES;

            IPAddress cBroadcast = IPHelper.GetBroadcastAddress(cIPAddress, cSubnetMask);

            try
            {
                SocketCommunicationSender cServerSocket = new SocketCommunicationSender(cBroadcast, 7345, 1024);
                Socket m_cUDPSocket = cServerSocket.Send();
            }
            finally
            {
                if (m_cUDPSocket != null)
                {
                    m_cUDPSocket.Close();
                }
            }
        }
    }
}
