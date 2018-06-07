using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WCFConnectionService
{
    public class ConnectionService : IConnectionService
    {
        public bool GetStatus()
        {
            return true;
        }

        public bool StartVoronoiService()
        {
            //TODO: start VoronoiGenerationService
            return true;
        }
    }
}
