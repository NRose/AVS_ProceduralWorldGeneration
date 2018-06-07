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
    class Program
    {
        static void Main(string[] args)
        {

            List<IPAddress> ipAddresses = new List<IPAddress>();
            int port = 8733;

            byte[] address = { 139, 6, 65, 51 };
            ipAddresses.Add(new IPAddress(address));
            VoronoiData data = new VoronoiData
            {
                Count = 1,
                Progress = 22,
                Minimum = 1,
                Maximum = 10
            };

            Console.Write(ServiceCallHelper.RunDistribution(ipAddresses, port, new List<VoronoiData> { data }));
            

        }

       
    }
}
