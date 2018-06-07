using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AVS_WorldGeneration
{
    class SocketCommunicationSender
    {
        private Socket m_cSocket;
        private byte[] m_abReceiveBuffer;
        private int m_nSourcePort = 55261;
        private int m_nReceiveBufferLength;
        private EndPoint m_cServerEndpoint;
        private int m_nDestPort;
        private IPAddress m_cDestIPAddress;
        private EndPoint m_cClientEndpoint;
        private SocketAsyncEventArgs m_cArgs;
        private List<Helper.Node> m_acReceivedItems = new List<Helper.Node>();

        public SocketCommunicationSender(IPAddress cIPAddress, int nPort, int nReceiveBufferLength)
        {
            m_cDestIPAddress = cIPAddress;
            m_nDestPort = nPort;
            m_nReceiveBufferLength = nReceiveBufferLength;
            m_abReceiveBuffer = new byte[nReceiveBufferLength];

            m_cServerEndpoint = new IPEndPoint(cIPAddress, nPort);
            m_cClientEndpoint = new IPEndPoint(IPHelper.GetLocalIPAddress(), m_nSourcePort);

            m_cSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_cSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            m_cSocket.Bind(m_cClientEndpoint);

        }
        public Socket Send()
        {
            byte[] abSendContent = new byte[m_nReceiveBufferLength];
            abSendContent = System.Text.ASCIIEncoding.Unicode.GetBytes(Helper.SocketCommunicationProtocol.SEARCH_FOR_NODES);

            m_cArgs = new SocketAsyncEventArgs();
            m_cArgs.RemoteEndPoint = m_cClientEndpoint;
            m_cArgs.SetBuffer(m_abReceiveBuffer, 0, m_nReceiveBufferLength);
            m_cArgs.Completed += Args_Completed;
            
            m_cSocket.ReceiveMessageFromAsync(m_cArgs);
            m_cSocket.SendTo(abSendContent, m_cServerEndpoint);
            
            Thread.Sleep(10000);

            (Application.Current.MainWindow as MainWindow).acAvailableNodes = new List<Helper.Node>(m_acReceivedItems);
            (Application.Current.MainWindow as MainWindow).UpdateNodeList();
            (Application.Current.MainWindow as MainWindow).btnConntectToNodes.IsEnabled = true;

            return m_cSocket;
        }

        private void Args_Completed(object cSender, SocketAsyncEventArgs cArgs)
        {
            m_cArgs = new SocketAsyncEventArgs();
            m_cArgs.RemoteEndPoint = m_cClientEndpoint;
            m_cArgs.SetBuffer(m_abReceiveBuffer, 0, m_nReceiveBufferLength);
            m_cArgs.Completed += Args_Completed;
            m_cSocket.ReceiveMessageFromAsync(m_cArgs);

            if (System.Text.ASCIIEncoding.Unicode.GetString(cArgs.Buffer).Contains(Helper.SocketCommunicationProtocol.READY_FOR_WORK))
            {
                m_acReceivedItems.Add(new Helper.Node() { bInUse = true, sIPAddress = (((IPEndPoint)cArgs.RemoteEndPoint).Address.ToString() + ":" + ((IPEndPoint)cArgs.RemoteEndPoint).Port.ToString()), nCores = 0, nProcessorsPhysical = 0, nProcessorsLogical = 0, nThreads = 0 });
            }
        }
    }
}