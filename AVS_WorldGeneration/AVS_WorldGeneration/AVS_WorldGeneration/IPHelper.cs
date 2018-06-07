using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AVS_WorldGeneration
{
    static class IPHelper
    {
        //TODO: Port und Content anpassen
        private static int m_cPort = 4321;
        private static byte[] m_abContent = Encoding.ASCII.GetBytes("Service Request");
        
        public static void SendBroadcastPacket()
        {
            NetworkInterface[] acNetworkInterface = NetworkInterface.GetAllNetworkInterfaces();

            // testing with network interface is active, preconditioned the communication interface is the only one
            NetworkInterface cNetworkInterface = null;
            foreach (NetworkInterface cInterface in acNetworkInterface)
            {
                if (cInterface.OperationalStatus == OperationalStatus.Up)
                {
                    cNetworkInterface = cInterface;
                    break;
                }
            }
            if (cNetworkInterface == null)
            {
                throw new NetworkInformationException();
            }

            IPAddress cIPAddress = null;
            IPAddress cSubnetMask = null;
            foreach (UnicastIPAddressInformation cIP in cNetworkInterface.GetIPProperties().UnicastAddresses)
            {
                if (!cIP.IPv4Mask.Equals(IPAddress.Parse("0.0.0.0")))
                {
                    cIPAddress = cIP.Address;
                    cSubnetMask = cIP.IPv4Mask;
                }
            }
            if (cNetworkInterface == null)
            {
                throw new NetworkInformationException();
            }

            IPAddress cBroadcast = IPHelper.GetBroadcastAddress(cIPAddress, cSubnetMask);
            IPHelper.SendBroadcastPacketToBroadcastIp(cBroadcast, m_cPort, m_abContent);
        }

        public static IPAddress GetBroadcastAddress(IPAddress cIPAddress, IPAddress cSubnetMask)
        {
            byte[] abIPAddress = cIPAddress.GetAddressBytes();
            byte[] abSubnetMask = cSubnetMask.GetAddressBytes();
            if (abIPAddress.Length != abSubnetMask.Length)
                throw new ArgumentException("Both IP address and subnet mask should be of the same length");
            var cResult = new byte[abIPAddress.Length];
            for (int i = 0; i < cResult.Length; i++)
                cResult[i] = (byte)(abIPAddress[i] | (abSubnetMask[i] ^ 255));
            return new IPAddress(cResult);
        }

        public static Socket SendBroadcastPacketToBroadcastIp(IPAddress cBoradcast, int nDestPort, byte[] abContent)
        {
            Socket cSocket = null;

            var destinationEndpoint = new IPEndPoint(cBoradcast, nDestPort);
            cSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            cSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

            cSocket.SendTo(abContent, destinationEndpoint);

            return cSocket;
        }

        public static IPAddress GetLocalIPAddress()
        {
            NetworkInterface[] acNetworkInterface = NetworkInterface.GetAllNetworkInterfaces();

            NetworkInterface cNetworkInterface = null;
            foreach (NetworkInterface cInterface in acNetworkInterface)
            {
                if (cInterface.OperationalStatus == OperationalStatus.Up)
                {
                    cNetworkInterface = cInterface;
                    break;
                }
            }
            if (cNetworkInterface == null)
            {
                throw new NetworkInformationException();
            }

            IPAddress cIPAddress = null;

            foreach (UnicastIPAddressInformation cIP in cNetworkInterface.GetIPProperties().UnicastAddresses)
            {
                if (!cIP.IPv4Mask.Equals(IPAddress.Parse("0.0.0.0")))
                {
                    cIPAddress = cIP.Address;
                }
            }
            if (cNetworkInterface == null)
            {
                throw new NetworkInformationException();
            }
            return cIPAddress;
        }

        public static IPAddress GetLocalSubmask()
        {
            NetworkInterface[] acNetworkInterface = NetworkInterface.GetAllNetworkInterfaces();

            NetworkInterface cNetworkInterface = null;
            foreach (NetworkInterface cInterface in acNetworkInterface)
            {
                if (cInterface.OperationalStatus == OperationalStatus.Up)
                {
                    cNetworkInterface = cInterface;
                    break;
                }
            }
            if (cNetworkInterface == null)
            {
                throw new NetworkInformationException();
            }

            IPAddress cSubnetMask = null;
            foreach (UnicastIPAddressInformation cIP in cNetworkInterface.GetIPProperties().UnicastAddresses)
            {
                if (!cIP.IPv4Mask.Equals(IPAddress.Parse("0.0.0.0")))
                {
                    cSubnetMask = cIP.IPv4Mask;
                }
            }
            if (cNetworkInterface == null)
            {
                throw new NetworkInformationException();
            }
            return cSubnetMask;
        }
    }
}
