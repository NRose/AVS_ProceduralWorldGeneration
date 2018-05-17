using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BroadcastListenerService
{
    /// <summary>
    /// Service to listen for broadcast service requests and answer on it.
    /// </summary>
    public partial class BroadcastListenerService : ServiceBase
    {
        private System.ComponentModel.IContainer components;
        private System.Diagnostics.EventLog eventLog1;
        private int eventId = 1;

        public BroadcastListenerService()
        {
            InitializeComponent();
            this.eventLog1 = new EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("MySource"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "MySource", "MyNewLog");
            }
            this.eventLog1.Source = "MySource";
            this.eventLog1.Log = "MyNewLog";
        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("In OnStart");

            // Set up a timer to trigger every minute.  
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 60000; // 60 seconds  
            timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("In onStop.");
        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            // TODO: Insert monitoring activities here.  
            eventLog1.WriteEntry("Monitoring the System", EventLogEntryType.Information, eventId++);
        }

        /// <summary>
        /// Generating a socket for listening to incoming 
        /// </summary>
        private static void ReceiveBroadcastPacketToBroadcastIp()
        {
            Socket socket = null;
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);
            try
            {
                socket = new Socket(ipAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(localEndPoint);
                socket.Listen(100);
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
