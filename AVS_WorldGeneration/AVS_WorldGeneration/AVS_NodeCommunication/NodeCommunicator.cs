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

        private ManualResetEvent m_cReset = new ManualResetEvent(false);
        private Thread m_cWorker;

        public NodeCommunicator()
        {
            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists("AVS_NodeCommunicatorLog_Source"))
                System.Diagnostics.EventLog.CreateEventSource("AVS_NodeCommunicatorLog_Source", "AVS_NodeCommunicatorLog");

            eventLog1.Source = "AVS_NodeCommunicatorLog_Source";
            eventLog1.Log = "AVS_NodeCommunicatorLog";

            eventLog1.WriteEntry("Log created - constructor done!");
        }

        private void ListenForCommand()
        {
            eventLog1.WriteEntry("Listening . . .");
            while (!m_cReset.WaitOne(0))
            {
                eventLog1.WriteEntry("While Start");
                m_cServerSocket.Listen(eventLog1);
                eventLog1.WriteEntry("While Continue");
                Thread.Sleep(1000);
                eventLog1.WriteEntry("While End");
            }
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
            /*
            serviceHost = new ServiceHost(typeof(VoronoiGenerationService));
            serviceHost.Open();
            eventLog1.WriteEntry("ServiceHost created");
            
            m_cWorker = new Thread(ListenForCommand);
            m_cWorker.Name = "AVS_PortListener";
            m_cWorker.IsBackground = true;
            m_cWorker.Start();
            */
            eventLog1.WriteEntry("OnStart finished");
        }
        
        protected override void OnContinue()
        {
            base.OnContinue();
            eventLog1.WriteEntry("Working");
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("OnStop called");
            m_cReset.Set();
            if(!m_cWorker.Join(3000))
            {
                m_cWorker.Abort();
            }
            eventLog1.WriteEntry("OnStop finished");
        }

    }
}
