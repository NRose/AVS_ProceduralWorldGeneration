using AVS_WorldGeneration.WcfCommunication;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;

namespace AVS_WorldGeneration
{
    /// <summary>
    /// Class that provides methods for calling a service.
    /// </summary>
    static class ServiceCallHelper
    {
        /// <summary>
        /// Method for running the VoronoiGenerationService on all given nodes.
        /// </summary>
        /// <param name="cIpAddresses">List of all given nodes where the service is running.</param>
        /// <param name="port">Port where the service is running.</param>
        /// <param name="voronoiData">Input data for voronoi generation.</param>
        /// <returns>A list with results on the distribution.</returns>

        public static List<double[]> RunDistribution(List<IPAddress> cIpAddresses, int nPort, List<VoronoiData> cVoronoiData)
        {
            List<double[]> cResult = new List<double[]>();

            // Create list of endpoint addresses
            List<EndpointAddress> cEpAdresses = new List<EndpointAddress>();

            foreach (IPAddress ip in cIpAddresses)
            {
                EndpointAddress cEndpoint = new EndpointAddress("http://" + ip.ToString() + ":" + nPort + "/VoronoiGenerationService");
                cEpAdresses.Add(cEndpoint);
            }

            // create channels to all nodes
            for (int i = 0; i < cEpAdresses.Count; i++)
            {
                EndpointAddress cEPA = cEpAdresses[i];
                VoronoiData cData = cVoronoiData[i];
                BasicHttpBinding bBinding = new BasicHttpBinding();

                try
                {
                    ChannelFactory<IVoronoiGenerationService> cChannelFactory = new ChannelFactory<IVoronoiGenerationService>(bBinding, cEPA);

                    IVoronoiGenerationService cWcfClient = cChannelFactory.CreateChannel();

                    List<double[]> cVectors = cWcfClient.RandomiseVectors(cData).Vectors;
                    cResult.AddRange(cVectors);
                    ((IClientChannel)cWcfClient).Close();
                }
                catch (EndpointNotFoundException e)
                {
                    //TODO: Feherbehandlung falls unter Ip-Adresse kein Service erreichbar ist
                }
            }
            return cResult;
        }
    }
}
