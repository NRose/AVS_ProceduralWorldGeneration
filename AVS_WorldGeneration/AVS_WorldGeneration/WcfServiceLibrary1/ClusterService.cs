using AVS_WorldGeneration.WcfCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace AVS_WorldGeneration.WcfCommunication
{
    // HINWEIS: Mit dem Befehl "Umbenennen" im Menü "Umgestalten" können Sie den Klassennamen "Service1" sowohl im Code als auch in der Konfigurationsdatei ändern.
    public class ClusterService : IClusterService
    {
        public void DoWork()
        {
            ServiceHost host = new ServiceHost(typeof(VoronoiGenerationService));
            
            host.Open();
        }
    }
}
