using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
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
        public List<BenTools.Mathematics.Vector> Vectors { get; set; }
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
            List<BenTools.Mathematics.Vector> vectors = new List<BenTools.Mathematics.Vector>
            {
                new BenTools.Mathematics.Vector(2)
            };
            VectorList list = new VectorList();
            list.Vectors = vectors;
            return list;
        }
    }
}
