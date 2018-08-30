using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;

namespace AVS_WorldGeneration.WcfCommunication
{
    [DataContract]
    public class VoronoiData
    {
        [DataMember]
        public int Count { get; set; }
        [DataMember]
        public int Threads { get; set; }
        [DataMember]
        public double Minimum { get; set; }
        [DataMember]
        public double Maximum { get; set; }
    }

    [DataContract]
    public class VectorList
    {
        [DataMember]
        public List<List<double[]>> Vectors { get; set; }
    }
    
    public interface ICallbackContract
    {
        [OperationContract]
        void OnGenerationFinished(List<List<double[]>> acVectors);
    }

    [ServiceContract(CallbackContract = typeof(ICallbackContract))] //Duplexvertrag
    public interface IVoronoiGenerationService
    {
        [OperationContract]
        void RandomiseVectors(VoronoiData data);
    }

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class VoronoiGenerationService : IVoronoiGenerationService
    {
        public static ICallbackContract cCallback;

        private int m_nThreadsInUse;
        private double[] m_dVector;
        private Random m_kRnd = new Random();
        private double m_dMinimum = -1.0;
        private double m_dMaximum = 1.0;
        private Object m_cObjThreadLocking = new Object();
        private int m_nVoronoiFinished_Threaded = 0;
        private VectorList m_acVectors;

        private System.Diagnostics.EventLog cEventLog;
        
        public void RandomiseVectors(VoronoiData data)
        {
            if (!System.Diagnostics.EventLog.SourceExists("AVS_WPF_VoronoiService_Source"))
            {
                System.Diagnostics.EventLog.CreateEventSource("AVS_WPF_VoronoiService_Source", "AVS_WPF_VoronoiServiceLog");
            }
            cEventLog = new System.Diagnostics.EventLog("AVS_WPF_VoronoiServiceLog");

            cEventLog.Source = "AVS_WPF_VoronoiService_Source";
            cEventLog.Log = "AVS_WPF_VoronoiServiceLog";

            cEventLog.WriteEntry("Log created - randomising vectors started!");

            double dVoronoiCount_Threaded = data.Count / data.Threads;
            double dExtra = data.Count % data.Threads;
            m_nThreadsInUse = data.Threads;
            m_dMinimum = data.Minimum;
            m_dMaximum = data.Maximum;
            m_acVectors = new VectorList();
            cCallback = OperationContext.Current.GetCallbackChannel<ICallbackContract>();

            for (int i = 0; i < data.Threads; i++)
            {
                cEventLog.WriteEntry("Worker (" + i.ToString() + "/" + data.Threads.ToString() + ") starting. . .");
                BackgroundWorker cWorker = new BackgroundWorker
                {
                    WorkerReportsProgress = false,
                    WorkerSupportsCancellation = false
                };
                cWorker.DoWork += GenerateVoronoiVectors;
                cWorker.RunWorkerCompleted += GenerateVoronoiVectorsFinished;

                cWorker.RunWorkerAsync(data.Count / data.Threads + dExtra);
                dExtra = 0.0;
                cEventLog.WriteEntry("Worker " + i.ToString() + " started!");
            }
        }
        
        private void GenerateVoronoiVectors(object sender, DoWorkEventArgs e)
        {
            double dOneStep = 100.0 / m_nThreadsInUse / (double)e.Argument;
            List<double[]> acTempList = new List<double[]>();

            for (int i = 0; i < (double)e.Argument; i++)
            {
                m_dVector = new double[] { m_kRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum, m_kRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum };
                acTempList.Add(m_dVector);
            }

            lock (m_cObjThreadLocking)
            {
                m_acVectors.Vectors.Add(acTempList);
            }
        }

        private void GenerateVoronoiVectorsFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            m_nVoronoiFinished_Threaded++;
            cEventLog.WriteEntry("Worker (" + m_nVoronoiFinished_Threaded.ToString() + "/" + m_nThreadsInUse.ToString() + " ended!");

            if (m_nVoronoiFinished_Threaded >= m_nThreadsInUse)
            {
                cEventLog.WriteEntry("WCF Calculation finished - Callback called!");
                cCallback.OnGenerationFinished(m_acVectors.Vectors);
            }
        }
    }
}
