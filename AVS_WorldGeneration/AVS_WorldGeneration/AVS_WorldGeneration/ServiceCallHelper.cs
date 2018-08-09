using AVS_WorldGeneration.WcfCommunication;
using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.Threading.Tasks;

namespace AVS_WorldGeneration
{
    /// <summary>
    /// Class that provides methods for calling a service.
    /// </summary>
    static class ServiceCallHelper
    {
        public static List<List<double[]>> acResults;
        public static bool bDistributionIsFinished = false;

        /// <summary>
        /// Method for running the VoronoiGenerationService on all given nodes.
        /// </summary>
        /// <param name="cIpAddresses">List of all given nodes where the service is running.</param>
        /// <param name="port">Port where the service is running.</param>
        /// <param name="voronoiData">Input data for voronoi generation.</param>
        /// <returns>A list with results on the distribution.</returns>
        public static async void RunDistribution(List<IPAddress> cIpAddresses, int nPort, List<VoronoiData> cVoronoiData)
        {
            acResults = new List<List<double[]>>();
            List<EndpointAddress> cEpAdresses = new List<EndpointAddress>();

            await Task.Run(delegate() { 
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
                    WSDualHttpBinding bBinding = new WSDualHttpBinding();

                    try
                    {
                        WcfServiceCallback cCallback = new WcfServiceCallback();
                        InstanceContext cInstanceContext = new InstanceContext(cCallback);

                        var cWcfService = new VoronoiWCFServiceReference.VoronoiGenerationServiceClient(cInstanceContext, bBinding, cEPA);
                        cWcfService.RandomiseVectors(cData);
                    }
                    catch (EndpointNotFoundException e)
                    {
                        //TODO: Feherbehandlung falls unter Ip-Adresse kein Service erreichbar ist
                    }
                }
            });
            bDistributionIsFinished = true;
        }
    }
}
