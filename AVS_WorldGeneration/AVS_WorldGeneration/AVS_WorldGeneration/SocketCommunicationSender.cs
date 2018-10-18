using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;
using static AVS_WorldGeneration.Helper;
using System.Windows.Threading;
using System.Diagnostics;

namespace AVS_WorldGeneration
{
    class SocketCommunicationSender
    {
        /*
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
        private bool m_bSocketClosed = false;

        public SocketCommunicationSender(IPAddress cIPAddress, int nPort, int nReceiveBufferLength, bool bCloseOldOne = false)
        {
            if(m_bSocketClosed)
            {
                m_cSocket.Close();
            }
            m_bSocketClosed = false;

            if(bCloseOldOne)
            {
                if(m_cSocket != null)
                {
                    m_cSocket.Close();
                }
            }

            m_cDestIPAddress = cIPAddress;
            m_nDestPort = nPort;
            m_nReceiveBufferLength = nReceiveBufferLength;
            m_abReceiveBuffer = new byte[nReceiveBufferLength];

            m_cServerEndpoint = new IPEndPoint(cIPAddress, nPort);
            m_cClientEndpoint = new IPEndPoint(IPHelper.GetLocalIPAddress(), m_nSourcePort);

            m_cSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_cSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            m_cSocket.Bind(m_cClientEndpoint);

        }

        public Socket Send(byte bCommand, bool bWaitForAnswer = true, bool bWaitForService = true)
        {
            if (bWaitForService)
            {
                WaitForServices();
            }
            else
            {
                m_bSocketClosed = false;
            }

            byte[] abSendContent = new byte[m_nReceiveBufferLength];
            abSendContent[0] = bCommand;

            m_cArgs = new SocketAsyncEventArgs();
            m_cArgs.RemoteEndPoint = m_cClientEndpoint;
            m_cArgs.SetBuffer(m_abReceiveBuffer, 0, m_nReceiveBufferLength);
            if (bWaitForAnswer)
            {
                m_cArgs.Completed += Args_Completed;
                m_cSocket.ReceiveMessageFromAsync(m_cArgs);
            }
            
            m_cSocket.SendTo(abSendContent, m_cServerEndpoint);

            Debug.WriteLine("SENDED CMD");
            // HIER WAREN WIR! :-)

            return m_cSocket;
        }

        private async void WaitForServices()
        {
            for (int nProgress = 0; nProgress < 1000; nProgress++)
            {
                await Task.Delay(10);
                (Application.Current.MainWindow as MainWindow).pbSearchForNodes.Value += 0.1f;
            }
            Debug.WriteLine("Socket will close");
            m_bSocketClosed = true;
            (Application.Current.MainWindow as MainWindow).acAvailableNodes = new List<Helper.Node>(m_acReceivedItems);
            (Application.Current.MainWindow as MainWindow).UpdateNodeList();
            (Application.Current.MainWindow as MainWindow).btnConntectToNodes.IsEnabled = true;
        }
        
        private void Args_Completed(object cSender, SocketAsyncEventArgs cArgs)
        {
            Debug.WriteLine("ARGS COMPLETED");
            if (m_bSocketClosed && cArgs.Buffer[0] != SocketCommunicationProtocol.SEND_VECTORS_BACK)
            {
                Debug.WriteLine("Socket already closed");
                m_cSocket.Close();
                m_bSocketClosed = false;
                return;
            }
            m_cArgs = new SocketAsyncEventArgs();
            m_cArgs.RemoteEndPoint = m_cClientEndpoint;
            m_cArgs.SetBuffer(m_abReceiveBuffer, 0, m_nReceiveBufferLength);
            m_cArgs.Completed += Args_Completed;
            m_cSocket.ReceiveMessageFromAsync(m_cArgs);

            byte[] bytearray = cArgs.Buffer;
            byte bHeaderProtocol = cArgs.Buffer[0];
            
            if (bHeaderProtocol == SocketCommunicationProtocol.READY_FOR_WORK)
            {
                NodeInfos nodeInfos = new Helper.NodeInfos();
                Int32 nlen = BitConverter.ToInt32(bytearray, 1);
                IntPtr i = Marshal.AllocHGlobal(nlen);
                Marshal.Copy(bytearray, 5, i, nlen);
                nodeInfos = (NodeInfos)Marshal.PtrToStructure(i, nodeInfos.GetType());
                Marshal.FreeHGlobal(i);

                m_acReceivedItems.Add(new Helper.Node() { bInUse = true, sIPAddress = (((IPEndPoint)cArgs.RemoteEndPoint).Address.ToString() + ":" + ((IPEndPoint)cArgs.RemoteEndPoint).Port.ToString()), nCores = nodeInfos.bCores, nProcessorsPhysical = nodeInfos.bProcessorsPhysical, nProcessorsLogical = nodeInfos.bProcessorsLogical, nThreads = nodeInfos.bCores });
            }
            else if (bHeaderProtocol == SocketCommunicationProtocol.SEND_VECTORS_BACK)
            {
                NodeResult cResult = new NodeResult();
                Int32 nlen = BitConverter.ToInt32(bytearray, 1);
                IntPtr i = Marshal.AllocHGlobal(nlen);
                Marshal.Copy(bytearray, 5, i, nlen);
                cResult = (NodeResult)Marshal.PtrToStructure(i, cResult.GetType());
                Marshal.FreeHGlobal(i);
                Debug.WriteLine("Ausgabe von Send Vectors Back: " + cResult.bAnswer.ToString());
            }
        }

        private void ByteArrayToStructure(byte[] bytearray, ref NodeInfos obj, out byte protocol)
        {
            protocol = bytearray[0];
            Int32 nlen = BitConverter.ToInt32(bytearray, 1);
            IntPtr i = Marshal.AllocHGlobal(nlen);
            Marshal.Copy(bytearray, 5, i, nlen);
            obj = (NodeInfos)Marshal.PtrToStructure(i, obj.GetType());
            Marshal.FreeHGlobal(i);
        }*/
    }
}