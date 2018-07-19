using AVS_WorldGeneration.WcfCommunication;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AVS_NodeCommunication
{
    partial class NodeCommunicator : ServiceBase
    {
        public ServiceHost serviceHost = null;
        private SocketCommunicationListener m_cServerSocket;
        
        public NodeCommunicator()
        {
            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists("AVS_NodeCommunicatorLog_Source"))
                System.Diagnostics.EventLog.CreateEventSource("AVS_NodeCommunicatorLog_Source", "AVS_NodeCommunicatorLog");

            eventLog1.Source = "AVS_NodeCommunicatorLog_Source";
            eventLog1.Log = "AVS_NodeCommunicatorLog";

            eventLog1.WriteEntry("Log created - constructor done!");
        }
        
        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("OnStart called");
            if (serviceHost != null)
            {
                serviceHost.Close();
            }
            m_cServerSocket = new SocketCommunicationListener(7345, 1024);
            eventLog1.WriteEntry("SocketCommunicationListener created");
            m_cServerSocket.Listen(eventLog1);
            eventLog1.WriteEntry("OnStart finished");
        }
        
        protected override void OnContinue()
        {
            base.OnContinue();
            eventLog1.WriteEntry("Working");
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("OnStop");
        }

    }
}
