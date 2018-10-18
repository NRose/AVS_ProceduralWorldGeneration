using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AVS_WorldGeneration
{
    class DistributionManager
    {
        public List<Helper.Node> acAvailableNodes;
        public List<Helper.Node> acWorkingNodes;

        #region Broadcast Socket

        private Socket m_cBroadcastSocket;
        private EndPoint m_cBroadcastServerEndpoint;
        private EndPoint m_cBroadcastClientEndpoint;
        private byte[] m_abBroadcastReceiveBuffer;
        private int m_nBroadcastReceiveBufferLength;
        private SocketAsyncEventArgs m_cBroadcastArgs;
        private bool m_bBroadcastClose;

        #endregion

        public DistributionManager()
        {
            acAvailableNodes = new List<Helper.Node>();
            acWorkingNodes = new List<Helper.Node>();
        }

        public void SearchNodes()
        {
            try
            {
                m_nBroadcastReceiveBufferLength = 1024;
                m_abBroadcastReceiveBuffer = new byte[m_nBroadcastReceiveBufferLength];
                m_cBroadcastServerEndpoint = new IPEndPoint(IPHelper.GetBroadcastAddress(IPHelper.GetLocalIPAddress(), IPHelper.GetLocalSubmask()), 7345);
                m_cBroadcastClientEndpoint = new IPEndPoint(IPHelper.GetLocalIPAddress(), 55261);
                m_bBroadcastClose = false;

                m_cBroadcastSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                m_cBroadcastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                m_cBroadcastSocket.Bind(m_cBroadcastClientEndpoint);

                byte[] abBroadcastContent = new byte[m_nBroadcastReceiveBufferLength];
                abBroadcastContent[0] = Helper.SocketCommunicationProtocol.SEARCH_FOR_NODES;

                m_cBroadcastArgs = new SocketAsyncEventArgs();
                m_cBroadcastArgs.RemoteEndPoint = m_cBroadcastClientEndpoint;
                m_cBroadcastArgs.SetBuffer(m_abBroadcastReceiveBuffer, 0, m_nBroadcastReceiveBufferLength);

                m_cBroadcastArgs.Completed += BroadcastCompleted;

                m_cBroadcastSocket.ReceiveMessageFromAsync(m_cBroadcastArgs);
                m_cBroadcastSocket.SendTo(abBroadcastContent, m_cBroadcastServerEndpoint);

                BroadcastClose();
                Debug.WriteLine("Sended broadcast");
            }
            catch(Exception cException)
            {
                Debug.WriteLine("Couldn't create socket: " + cException.Message);
            }
        }

        public void DistributeWork()
        {

        }

        private async void BroadcastClose()
        {
            for (int nProgress = 0; nProgress < 1000; nProgress++)
            {
                await Task.Delay(10);
                (Application.Current.MainWindow as MainWindow).pbSearchForNodes.Value += 0.1f;
            }
            m_bBroadcastClose = true;
            (Application.Current.MainWindow as MainWindow).UpdateNodeList();
            (Application.Current.MainWindow as MainWindow).btnConntectToNodes.IsEnabled = true;
        }

        private void BroadcastCompleted(object cSender, SocketAsyncEventArgs cArgs)
        {
            Debug.WriteLine("Called Broadcast Completed");
            if (m_bBroadcastClose)
            {
                Debug.WriteLine("Close Broadcast Socket");
                m_cBroadcastSocket.Close();
                m_bBroadcastClose = false;
                return;
            }

            m_cBroadcastArgs = new SocketAsyncEventArgs();
            m_cBroadcastArgs.RemoteEndPoint = m_cBroadcastClientEndpoint;
            m_cBroadcastArgs.SetBuffer(m_abBroadcastReceiveBuffer, 0, m_nBroadcastReceiveBufferLength);
            m_cBroadcastArgs.Completed += BroadcastCompleted;
            m_cBroadcastSocket.ReceiveMessageFromAsync(m_cBroadcastArgs);

            byte bHeaderProtocol = cArgs.Buffer[0];
            Debug.WriteLine("Broadcast : " + bHeaderProtocol);
            if (bHeaderProtocol == Helper.SocketCommunicationProtocol.READY_FOR_WORK)
            {
                Debug.WriteLine("Try adding Node...");
                Helper.NodeInfos nodeInfos = new Helper.NodeInfos();
                Int32 nlen = BitConverter.ToInt32(cArgs.Buffer, 1);
                IntPtr i = Marshal.AllocHGlobal(nlen);
                Marshal.Copy(cArgs.Buffer, 5, i, nlen);
                nodeInfos = (Helper.NodeInfos)Marshal.PtrToStructure(i, nodeInfos.GetType());
                Marshal.FreeHGlobal(i);

                acAvailableNodes.Add(new Helper.Node((((IPEndPoint)cArgs.RemoteEndPoint).Address.ToString() + ":" + ((IPEndPoint)cArgs.RemoteEndPoint).Port.ToString()), nodeInfos));

                Debug.WriteLine("Added Node!");
            }
        }
        
    }
}
