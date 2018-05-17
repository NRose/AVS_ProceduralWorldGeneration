using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WcfServiceLibrary1
{
   public static class BroadcastHelper
   {
      //TODO: Port und Content anpassen
      private static int PORT = 4321;
      private static byte[] CONTENT = Encoding.ASCII.GetBytes("Service Request");


      public static void SendBroadcastPacket()
      {
         NetworkInterface[] nets = NetworkInterface.GetAllNetworkInterfaces();

         // testing with network interface is active, preconditioned the communication interface is the only one
         NetworkInterface netInterface = null;
         foreach (NetworkInterface net in nets)
         {
            if (net.OperationalStatus == OperationalStatus.Up)
            {
               netInterface = net;
               break;
            }
         }
         if (netInterface == null)
         {
            throw new NetworkInformationException();
         }

         IPAddress ipAddress = null;
         IPAddress netmask = null;
         foreach (UnicastIPAddressInformation ip in netInterface.GetIPProperties().UnicastAddresses)
         {
            if (!ip.IPv4Mask.Equals(IPAddress.Parse("0.0.0.0")))
            {
               ipAddress = ip.Address;
               netmask = ip.IPv4Mask;
            }
         }
         if (netInterface == null)
         {
            throw new NetworkInformationException();
         }

         IPAddress broadcast = BroadcastHelper.GetBroadcastAddress(ipAddress, netmask);
         BroadcastHelper.SendBroadcastPacketToBroadcastIp(broadcast, PORT, CONTENT);
      }

      private static IPAddress GetBroadcastAddress(IPAddress ipAddress, IPAddress subnetMask)
      {
         byte[] ipAdressBytes = ipAddress.GetAddressBytes();
         byte[] subnetMaskBytes = subnetMask.GetAddressBytes();
         if (ipAdressBytes.Length != subnetMaskBytes.Length)
            throw new ArgumentException("Both IP address and subnet mask should be of the same length");
         var result = new byte[ipAdressBytes.Length];
         for (int i = 0; i < result.Length; i++)
            result[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
         return new IPAddress(result);
      }

      private static void SendBroadcastPacketToBroadcastIp(IPAddress broadcastIp, int destinationPort, byte[] content)
      {
         Socket socket = null;
         try
         {
            var destinationEndpoint = new IPEndPoint(broadcastIp, destinationPort);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            socket.SendTo(content, destinationEndpoint);
         }
         finally
         {
            if (socket != null)
            {
               socket.Close();
            }
         }
      }
   }
}
