using AVS_WorldGeneration.WcfCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ServiceCallerApplication
{
    static class ServiceCallHelper
    {
        /// <summary>
        /// Method for running the VoronoiGenerationService on all given nodes.
        /// </summary>
        /// <param name="ipAddresses">List of all given nodes where the service is running.</param>
        /// <param name="port">Port where the service is running.</param>
        /// <returns>A list with results on the distribution.</returns>

        public static List<BenTools.Mathematics.Vector> RunDistribution(List<IPAddress> ipAddresses, int port)
        {
            List<BenTools.Mathematics.Vector> result = new List<BenTools.Mathematics.Vector>();

            // Create list of endpoint addresses
            List<EndpointAddress> epAdresses = new List<EndpointAddress>();

            foreach (IPAddress ip in ipAddresses)
            {
                EndpointAddress endpoint = new EndpointAddress("http://" + ip.ToString() + ":" + port + "/VoronoiGenerationService");
                epAdresses.Add(endpoint);
            }

            // create channels to all nodes
            foreach (EndpointAddress epa in epAdresses)
            {
                BasicHttpBinding binding = new BasicHttpBinding();

                try
                {
                    ChannelFactory<IVoronoiGenerationService> myChannelFactory = new ChannelFactory<IVoronoiGenerationService>(binding, epa);

                    // Create a channel.
                    IVoronoiGenerationService wcfClient = myChannelFactory.CreateChannel();
                    VoronoiData data = new VoronoiData
                    {
                        Count = 1,
                        Progress = 22,
                        Minimum = 1,
                        Maximum = 10
                    };
                    List<BenTools.Mathematics.Vector> vectors = wcfClient.RandomiseVectors(data).Vectors; // TODO: Entscheiden, welche Nodes welche Berechnungen durchführen
                    result.Concat(vectors);
                    ((IClientChannel)wcfClient).Close();
                }
                catch (EndpointNotFoundException e)
                {
                    //TODO: Feherbehandlung falls unter Ip-Adresse kein Service erreichbar ist
                }
            }

            return result;
        }
    }
}
