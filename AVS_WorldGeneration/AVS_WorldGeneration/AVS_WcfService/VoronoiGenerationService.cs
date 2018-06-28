using System;
using System.Collections.Generic;
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
        public float Progress { get; set; }
        [DataMember]
        public double Minimum { get; set; }
        [DataMember]
        public double Maximum { get; set; }
    }

    [DataContract]
    public class VectorList
    {
        [DataMember]
        public List<double[]> Vectors { get; set; }
    }
    
        [ServiceContract]
    public interface IVoronoiGenerationService
    {
        [OperationContract]
        VectorList RandomiseVectors(VoronoiData data); //int nCount, float fProgress, double dMinimum, double dMaximum
    }

    public class VoronoiGenerationService : IVoronoiGenerationService
    {

        public VectorList RandomiseVectors(VoronoiData data)
        {
            List<double[]> vectors = new List<double[]>();
            vectors.Add(new double[] { 1.0, 2.0 });
            VectorList list = new VectorList();
            list.Vectors = vectors;
            return list;
        }
        /*
        public static void Configure(ServiceConfiguration config)
        {
            //    ServiceEndpoint se = new ServiceEndpoint(new ContractDescription("IVoronoiGenerationService"), new BasicHttpBinding(), new EndpointAddress("http://localhost:8733/VoronoiGenerationService"));
            //se.Behaviors.Add(new MyEndpointBehavior());
            //config.AddServiceEndpoint(se);
            //    config.Description.Endpoints.Add(se);
            //    config.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true });
            //    config.Description.Behaviors.Add(new ServiceDebugBehavior { IncludeExceptionDetailInFaults = true });
            config.LoadFromConfiguration(ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap { ExeConfigFilename = @"D:\Documents\Studium\AVS\AVS_ProceduralWorldGeneration\AVS_WorldGeneration\AVS_WorldGeneration\AVS_WcfService\App.config" }, ConfigurationUserLevel.None));
        }
        */
    }
}
