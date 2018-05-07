using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace AVS_WorldGeneration.WcfCommunication
{
    // HINWEIS: Mit dem Befehl "Umbenennen" im Menü "Umgestalten" können Sie den Schnittstellennamen "IService1" sowohl im Code als auch in der Konfigurationsdatei ändern.
    [DataContract]
    public class TestData
    {
        [DataMember]
        public String Name { get; set; }
        [DataMember]
        public String Value { get; set; }
    }

    [ServiceContract]
    public interface IVoronoiGenerationService
    {
        [OperationContract]
        String sendData(TestData data);
    }

    public class VoronoiGenerationService : IVoronoiGenerationService
    {
        #region IDataService Members
        public String sendData(TestData data)
        {
           return "Send Data with name " + data.Name + "and value " + data.Value;
        }
        #endregion
    }
}
