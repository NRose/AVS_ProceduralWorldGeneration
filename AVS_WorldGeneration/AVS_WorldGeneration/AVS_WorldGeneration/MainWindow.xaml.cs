using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BenTools.Mathematics;
using System.Threading;
using System.Windows.Media.Media3D;

namespace AVS_WorldGeneration
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Voronoi Stuff
        private List<BenTools.Mathematics.Vector> m_akVectors = new List<BenTools.Mathematics.Vector>();
        private Random m_kRnd = new Random();
        private double[] m_dVector;
        VoronoiGraph m_kVoronoi;
        #endregion

        #region 3D Stuff
        private Model3DGroup m_kMainModel3DGroup = new Model3DGroup();
        private PerspectiveCamera m_kMainCamera;

        private double m_dCameraPhi = Math.PI / 6.0;
        private double m_dCameraTheta = Math.PI / 6.0;
        private double m_dCameraR = 3.0;

        private const double m_dCameraDPhi = 0.1;
        private const double m_dCameraDTheta = 0.1;
        private const double m_dCameraDR = 0.1;

        private Dictionary<Point3D, int> m_dicPoints = new Dictionary<Point3D, int>();
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            m_kMainCamera = new PerspectiveCamera();
            m_kMainCamera.FieldOfView = 60;
            vpOutputView.Camera = m_kMainCamera;
            PositionCamera();

            DefineLights();
            /*
            DefineModel(m_kMainModel3DGroup);

            ModelVisual3D kModelVisual = new ModelVisual3D();
            kModelVisual.Content = m_kMainModel3DGroup;

            vpOutputView.Children.Add(kModelVisual);*/

        }

        delegate void VoronoiProgress(DependencyProperty dp, Object value);

        private void GenerateVoronoi()
        {
            VoronoiProgress updatePB = pbGenerateVoronoi.SetValue;
            double dVoronoiCount = 0;
            if (!double.TryParse(tbxVoronoiLoopCount.Text, out dVoronoiCount))
            {
                return;
            }
            double dOneStep = 100.0 / (dVoronoiCount + (dVoronoiCount * 0.33f));
            double dValue = 0.0;
            pbGenerateVoronoi.Dispatcher.Invoke(updatePB, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, dValue});
            for (int i = 0; i < dVoronoiCount; i++)
            {
                m_dVector = new double[] { m_kRnd.NextDouble(), m_kRnd.NextDouble() };
                m_akVectors.Add(new BenTools.Mathematics.Vector(m_dVector));
                dValue += dOneStep;
                pbGenerateVoronoi.Dispatcher.Invoke(updatePB, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, dValue });
            }

            m_kVoronoi = Fortune.ComputeVoronoiGraph(m_akVectors);
            pbGenerateVoronoi.Dispatcher.Invoke(updatePB, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, 100.0 });
            btnDrawVoronoi.IsEnabled = true;
            vpOutputView.Focus();
        }

        private void DrawVoronoi()
        {
            MeshGeometry3D kMesh = new MeshGeometry3D();

            foreach (BenTools.Mathematics.Vector kVector in m_kVoronoi.Vertizes)
            {
                Point3D p00 = new Point3D(kVector.data[0], 0.0, kVector.data[1]);
                kMesh.TriangleIndices.Add(AddPoint(kMesh.Positions, p00));
            }

            DiffuseMaterial kSurfaceMaterial = new DiffuseMaterial(Brushes.Orange);
            GeometryModel3D kSurfaceModel = new GeometryModel3D(kMesh, kSurfaceMaterial);
            kSurfaceModel.BackMaterial = kSurfaceMaterial;
            m_kMainModel3DGroup.Children.Add(kSurfaceModel);

            ModelVisual3D kModelVisual = new ModelVisual3D();
            kModelVisual.Content = m_kMainModel3DGroup;
            
            vpOutputView.Children.Clear();
            GC.Collect();
            vpOutputView.Children.Add(kModelVisual);
        }

        private void BtnGenerateVoronoi_Click(object sender, RoutedEventArgs e)
        {
            GenerateVoronoi();
        }

        private void BtnDrawVoronoi_Click(object sender, RoutedEventArgs e)
        {
            DrawVoronoi();
        }

        private int AddPoint(Point3DCollection points, Point3D point)
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

        private void DefineLights()
        {
            AmbientLight kAmbientLight = new AmbientLight(Colors.Gray);
            DirectionalLight kDirectionalLight = new DirectionalLight(Colors.Gray, new Vector3D(-1.0, -3.0, -2.0));
            m_kMainModel3DGroup.Children.Add(kAmbientLight);
            m_kMainModel3DGroup.Children.Add(kDirectionalLight);
        }

        private void DefineModel(Model3DGroup kModelGroup)
        {
            MeshGeometry3D kMesh = new MeshGeometry3D();

            const double dXMin = -1.5;
            const double dXMax = 1.5;
            const double dX = 0.05;
            const double dZMin = -1.5;
            const double dZMax = 1.5;
            const double dZ = 0.05;

            for(double x = dXMin; x <= dXMax - dX; x += dX)
            {
                for(double z = dZMin; z <= dZMax - dZ; z += dZ)
                {
                    Point3D p00 = new Point3D(x, F(x, z), z);
                    Point3D p10 = new Point3D(x + dX, F(x + dX, z), z);
                    Point3D p01 = new Point3D(x, F(x, z + dZ), z + dZ);
                    Point3D p11 = new Point3D(x + dX, F(x + dX, z + dZ), z + dZ);

                    AddTriangle(kMesh, p00, p01, p11);
                    AddTriangle(kMesh, p00, p11, p10);
                }
            }

            DiffuseMaterial kSurfaceMaterial = new DiffuseMaterial(Brushes.Orange);
            GeometryModel3D kSurfaceModel = new GeometryModel3D(kMesh, kSurfaceMaterial);
            kSurfaceModel.BackMaterial = kSurfaceMaterial;
            kModelGroup.Children.Add(kSurfaceModel);
        }

        private double F(double dX, double dZ)
        {
            const double dTwoPI = 2 * 3.14159265;
            double dR2 = dX * dX + dZ * dZ;
            double dR = Math.Sqrt(dR2);
            double dTheta = Math.Atan2(dZ, dX);

            return Math.Exp(-dR2) * Math.Sin(dTwoPI * dR) * Math.Cos(3 * dTheta);
        }

        private void AddTriangle(MeshGeometry3D kMesh, Point3D kPoint1, Point3D kPoint2, Point3D kPoint3)
        {
            int nIndex1 = AddPoint(kMesh.Positions, kPoint1);
            int nIndex2 = AddPoint(kMesh.Positions, kPoint2);
            int nIndex3 = AddPoint(kMesh.Positions, kPoint3);

            kMesh.TriangleIndices.Add(nIndex1);
            kMesh.TriangleIndices.Add(nIndex2);
            kMesh.TriangleIndices.Add(nIndex3);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    m_dCameraPhi += m_dCameraDPhi;
                    if (m_dCameraPhi > Math.PI / 2.0)
                        m_dCameraPhi = Math.PI / 2.0;
                    break;
                case Key.Down:
                    m_dCameraPhi -= m_dCameraDPhi;
                    if (m_dCameraPhi < -Math.PI / 2.0)
                        m_dCameraPhi = -Math.PI / 2.0;
                    break;
                case Key.Left:
                    m_dCameraTheta += m_dCameraDTheta;
                    break;
                case Key.Right:
                    m_dCameraTheta -= m_dCameraDTheta;
                    break;
                case Key.Add:
                case Key.OemPlus:
                    m_dCameraR -= m_dCameraDR;
                    if (m_dCameraR < m_dCameraDR)
                        m_dCameraR = m_dCameraDR;
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                    m_dCameraR += m_dCameraDR;
                    break;
                default:
                    break;
            }
            PositionCamera();
        }

        private void PositionCamera()
        {
            double dY = m_dCameraR * Math.Sin(m_dCameraPhi);
            double dHYP = m_dCameraR * Math.Cos(m_dCameraPhi);
            double dX = dHYP * Math.Cos(m_dCameraTheta);
            double dZ = dHYP * Math.Sin(m_dCameraTheta);

            m_kMainCamera.Position = new Point3D(dX, dY, dZ);
            m_kMainCamera.LookDirection = new Vector3D(-dX, -dY, -dZ);
            m_kMainCamera.UpDirection = new Vector3D(0, 1, 0);
        }
    }
}
