using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace AVS_WorldGeneration
{
    class NetworkManager
    {
        public List<Helper.Distributor> acDistributors
        {
            get
            {
                return m_acDistributors;
            }
        }

        private Socket m_cUDPSocket = null;

        private List<Helper.Distributor> m_acDistributors;

        public void InitializeNetworkManager()
        {
            IPAddress cIPAddress = IPHelper.GetLocalIPAddress();
            IPAddress cSubnetMask = IPHelper.GetLocalSubmask();
            
            IPAddress cBroadcast = IPHelper.GetBroadcastAddress(cIPAddress, cSubnetMask);

            try
            {
                SocketCommunicationSender cServerSocket = new SocketCommunicationSender(cBroadcast, 7345, 1024);
                Socket m_cUDPSocket = cServerSocket.Send(Helper.SocketCommunicationProtocol.SEARCH_FOR_NODES);
            }
            finally
            {
                if (m_cUDPSocket != null)
                {
                    m_cUDPSocket.Close();
                }
            }
        }

        public void InitializeNetwork(List<Helper.Node> acNodes)
        {
            m_acDistributors = new List<Helper.Distributor>();

            foreach(Helper.Node cNode in acNodes)
            {
                if (cNode.bInUse)
                {
                    try
                    {
                        SocketCommunicationSender cServerSocket = new SocketCommunicationSender(IPAddress.Parse(cNode.sIPAddress.Split(':')[0]), 7345, 1024, true);
                        Socket m_cUDPSocket = cServerSocket.Send(Helper.SocketCommunicationProtocol.GENERATE_VORONOI, true, false);

                        Helper.Distributor cDist = new Helper.Distributor();
                        cDist.cAddress = IPAddress.Parse(cNode.sIPAddress.Split(':')[0]);
                        cDist.nCores = cNode.nCores;

                        m_acDistributors.Add(cDist);
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
    }
}
