using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace AVS_WorldGeneration
{
    public static class Helper
    {
        public static List<LogItem> akLogger = new List<LogItem>();

        private static Dictionary<Point3D, int> m_dicPoints = new Dictionary<Point3D, int>();
        
        public static MeshGeometry3D ToWireframe(this MeshGeometry3D kMesh, double dThickness)
        {
            Dictionary<int, int> dicAlreadyDrawn = new Dictionary<int, int>();
            MeshGeometry3D kWireframe = new MeshGeometry3D();

            for (int i = 0; i < kMesh.TriangleIndices.Count; i += 3)
            {
                try
                {
                    int nIndex1 = kMesh.TriangleIndices[i];
                    int nIndex2 = kMesh.TriangleIndices[i + 1];
                    int nIndex3 = kMesh.TriangleIndices[i + 2];

                    AddTriangleWireframeSegment(kMesh, kWireframe, dicAlreadyDrawn, nIndex1, nIndex2, dThickness);
                    AddTriangleWireframeSegment(kMesh, kWireframe, dicAlreadyDrawn, nIndex2, nIndex3, dThickness);
                    AddTriangleWireframeSegment(kMesh, kWireframe, dicAlreadyDrawn, nIndex3, nIndex1, dThickness);
                }
                catch
                {

                }
            }

            return kWireframe;
        }

        public static void AddTriangleWireframeSegment(MeshGeometry3D kMesh, MeshGeometry3D kWireframe, Dictionary<int, int> dicAlreadyDrawn, int nIndex1, int nIndex2, double dThickness)
        {
            if (nIndex1 > nIndex2)
            {
                int nTemp = nIndex1;
                nIndex1 = nIndex2;
                nIndex2 = nTemp;
            }
            // TODO: Check if * * is right and not * +
            int nSegmentID = nIndex1 * kMesh.Positions.Count * nIndex2;

            if (dicAlreadyDrawn.ContainsKey(nSegmentID))
                return;

            dicAlreadyDrawn.Add(nSegmentID, nSegmentID);

            AddWireframeSegment(kWireframe, kMesh.Positions[nIndex1], kMesh.Positions[nIndex2], dThickness);
        }

        public static void AddWireframeSegment(MeshGeometry3D kMesh, Point3D kP1, Point3D kP2, double dThickness, bool bExtend = false)
        {
            Vector3D kVectorUp = new Vector3D(0, 1, 0);
            Vector3D kSegmentVector = kP2 - kP1;
            kSegmentVector.Normalize();

            if (Math.Abs(Vector3D.DotProduct(kVectorUp, kSegmentVector)) > 0.9)
                kVectorUp = new Vector3D(1, 0, 0);

            AddWireframeSegment(kMesh, kP1, kP2, kVectorUp, dThickness, bExtend);
        }
        
        public static void AddWireframeSegment(MeshGeometry3D kMesh, Point3D kP1, Point3D kP2, Vector3D kUp, double dThickness, bool bExtend)
        {
            Vector3D kSegmentVector = kP2 - kP1;

            if (bExtend)
            {
                Vector3D kN = Vector3D.Multiply(kSegmentVector, dThickness / 2.0);
                kP1 -= kN;
                kP2 += kN;
            }

            Vector3D kScaledUpVector = Vector3D.Multiply(kUp, dThickness / 2.0);
            Vector3D kScaledPerpendicularVector = Vector3D.CrossProduct(kSegmentVector, kScaledUpVector);
            kScaledPerpendicularVector = Vector3D.Multiply(kScaledPerpendicularVector, dThickness / 2.0f);

            Point3D kP1pp = kP1 + kScaledUpVector + kScaledPerpendicularVector;
            Point3D kP1mp = kP1 - kScaledUpVector + kScaledPerpendicularVector;
            Point3D kP1pm = kP1 + kScaledUpVector - kScaledPerpendicularVector;
            Point3D kP1mm = kP1 - kScaledUpVector - kScaledPerpendicularVector;
            Point3D kP2pp = kP2 + kScaledUpVector + kScaledPerpendicularVector;
            Point3D kP2mp = kP2 - kScaledUpVector + kScaledPerpendicularVector;
            Point3D kP2pm = kP2 + kScaledUpVector - kScaledPerpendicularVector;
            Point3D kP2mm = kP2 - kScaledUpVector - kScaledPerpendicularVector;

            AddTriangle(kMesh, kP1pp, kP1mp, kP2mp);
            AddTriangle(kMesh, kP1pp, kP2mp, kP2pp);

            AddTriangle(kMesh, kP1pp, kP2pp, kP2pm);
            AddTriangle(kMesh, kP1pp, kP2pm, kP1pm);

            AddTriangle(kMesh, kP1pm, kP2pm, kP2mm);
            AddTriangle(kMesh, kP1pm, kP2mm, kP1mm);

            AddTriangle(kMesh, kP1mm, kP2mm, kP2mp);
            AddTriangle(kMesh, kP1mm, kP2mp, kP1mp);

            AddTriangle(kMesh, kP1pp, kP1pm, kP1mm);
            AddTriangle(kMesh, kP1pp, kP1mm, kP1mp);

            AddTriangle(kMesh, kP2pp, kP2mp, kP2mm);
            AddTriangle(kMesh, kP2pp, kP2mm, kP2pm);
        }

        public static void AddTriangle(MeshGeometry3D kMesh, Point3D kPoint1, Point3D kPoint2, Point3D kPoint3)
        {
            int nIndex1 = AddPoint(kMesh.Positions, kPoint1);
            int nIndex2 = AddPoint(kMesh.Positions, kPoint2);
            int nIndex3 = AddPoint(kMesh.Positions, kPoint3);

            kMesh.TriangleIndices.Add(nIndex1);
            kMesh.TriangleIndices.Add(nIndex2);
            kMesh.TriangleIndices.Add(nIndex3);
        }

        public static int AddPoint(Point3DCollection points, Point3D point)
        {
            /*
            for(int i = 0; i < points.Count; i++)
            {
                if ((point.X == points[i].X) && (point.Y == points[i].Y) && (point.Z == points[i].Z))
                    return i;
            }
            points.Add(point);
            return points.Count - 1;
            */
            if (m_dicPoints.ContainsKey(point))
                return m_dicPoints[point];

            points.Add(point);
            m_dicPoints.Add(point, points.Count - 1);
            return points.Count - 1;
        }

        public static double F(double dX, double dZ)
        {
            const double dTwoPI = 2 * 3.14159265;
            double dR2 = dX * dX + dZ * dZ;
            double dR = Math.Sqrt(dR2);
            double dTheta = Math.Atan2(dZ, dX);

            return Math.Exp(-dR2) * Math.Sin(dTwoPI * dR) * Math.Cos(3 * dTheta);
        }

        public static class SocketCommunicationProtocol
        {
            public static byte SEARCH_FOR_NODES = 127;
            public static byte READY_FOR_WORK = 93;
            public static byte START_WCF_SERVICE = 101;
            public static byte GENERATE_VORONOI = 156;
            public static byte SEND_VECTORS_BACK = 189;
        }

        public class Node
        {
            public bool bInUse { get; set; }
            public string sIPAddress { get; set; }
            public int nCores { get; set; }
            public int nProcessorsPhysical { get; set; }
            public int nProcessorsLogical { get; set; }
            public int nThreads { get; set; }
        }

        public struct NodeInfos
        {
            public byte bCores { get; set; }
            public byte bProcessorsPhysical { get; set; }
            public byte bProcessorsLogical { get; set; }
        }

        public struct Distributor
        {
            public IPAddress cAddress;
            public int nCores;
        }
    }
}
