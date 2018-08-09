using AVS_WorldGeneration.WcfCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AVS_WorldGeneration
{
    class WcfServiceCallback : ICallbackContract
    {
        public void OnGenerationFinished(List<List<double[]>> acVectors)
        {
            List<BenTools.Mathematics.Vector> acResult = new List<BenTools.Mathematics.Vector>();

            foreach(List<double[]> acThreadedVectors in acVectors)
            {
                foreach(double[] adTuple in acThreadedVectors)
                {
                    acResult.Add(new BenTools.Mathematics.Vector(adTuple));
                }
            }

            (Application.Current.MainWindow as MainWindow).AddNetworkResult(acResult);
        }
    }
}
