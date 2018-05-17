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

        public static List<double[]> RunDistribution(List<IPAddress> ipAddresses, int port, List<VoronoiData> voronoiData)
        {
            List<double[]> result = new List<double[]>();

            // Create list of endpoint addresses
            List<EndpointAddress> epAdresses = new List<EndpointAddress>();

            foreach (IPAddress ip in ipAddresses)
            {
                EndpointAddress endpoint = new EndpointAddress("http://" + ip.ToString() + ":" + port + "/VoronoiGenerationService");
                epAdresses.Add(endpoint);
            }

            // create channels to all nodes
            for(int i = 0; i < epAdresses.Count; i++)
            {
                EndpointAddress epa = epAdresses[i];
                VoronoiData data = voronoiData[i];
                BasicHttpBinding binding = new BasicHttpBinding();

                try
                {
                    ChannelFactory<IVoronoiGenerationService> myChannelFactory = new ChannelFactory<IVoronoiGenerationService>(binding, epa);

                    // Create a channel.
                    IVoronoiGenerationService wcfClient = myChannelFactory.CreateChannel();

                    List<double[]> vectors = wcfClient.RandomiseVectors(data).Vectors;
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
