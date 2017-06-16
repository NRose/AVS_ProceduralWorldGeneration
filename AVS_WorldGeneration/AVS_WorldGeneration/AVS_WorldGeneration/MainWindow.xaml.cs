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
        private const double m_dMinimum = -1.0;
        private const double m_dMaximum = 1.0;

        #region Voronoi Stuff
        private List<BenTools.Mathematics.Vector> m_akVectors = new List<BenTools.Mathematics.Vector>();
        private Random m_kRnd = new Random();
        private int m_nSeed = 0;
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
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            m_nSeed = m_kRnd.Next();
            m_kRnd = new Random(m_nSeed);
            tbxSeed.Text = m_nSeed.ToString();

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
        delegate void Logging(string sLogMessage, LogLevel eLogLevel);

        private void GenerateVoronoi()
        {
            Logging systemLog = this.Log;

            Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Start process: Generate Voronoi", LogLevel.INFO });
            int nSeed = 0;
            if(!int.TryParse(tbxSeed.Text, out nSeed))
            {
                Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Seed contains illegal characters!", LogLevel.WARN });
                return;
            }
            m_nSeed = nSeed;
            m_kRnd = new Random(m_nSeed);
            
            VoronoiProgress updatePB = pbGenerateVoronoi.SetValue;

            double dVoronoiCount = 0;
            if (!double.TryParse(tbxVoronoiLoopCount.Text, out dVoronoiCount))
            {
                Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Loop count contains illegal characters!", LogLevel.WARN });
                return;
            }
            double dOneStep = 100.0 / (dVoronoiCount + (dVoronoiCount * 0.33f));
            double dValue = 0.0;
            m_akVectors = new List<BenTools.Mathematics.Vector>();
            pbGenerateVoronoi.Dispatcher.Invoke(updatePB, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, dValue});
            /*m_akVectors.Add(new BenTools.Mathematics.Vector(new double[] { dMinimum, dMinimum }));
            m_akVectors.Add(new BenTools.Mathematics.Vector(new double[] { dMinimum, dMaximum }));
            m_akVectors.Add(new BenTools.Mathematics.Vector(new double[] { dMaximum, dMinimum }));
            m_akVectors.Add(new BenTools.Mathematics.Vector(new double[] { dMaximum, dMaximum }));*/
            Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Start process: Randomizing Vectors", LogLevel.INFO });
            for (int i = 0; i < dVoronoiCount; i++)
            {
                m_dVector = new double[] { m_kRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum, m_kRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum };
                m_akVectors.Add(new BenTools.Mathematics.Vector(m_dVector));
                dValue += dOneStep;
                pbGenerateVoronoi.Dispatcher.Invoke(updatePB, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, dValue });
            }
            Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "End process: Randomizing Vectors", LogLevel.INFO });
            //m_akVectors = m_akVectors.OrderBy(o => o[0]).ThenBy(o => o[1]).ToList();
            m_kVoronoi = Fortune.ComputeVoronoiGraph(m_akVectors);
            pbGenerateVoronoi.Dispatcher.Invoke(updatePB, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, 100.0 });
            btnDrawVoronoi.IsEnabled = true;
            vpOutputView.Focus();
            Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "End process: Generate Voronoi", LogLevel.INFO });
        }

        private void DrawVoronoi()
        {
            Logging systemLog = this.Log;

            Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Start process: Draw Voronoi", LogLevel.INFO });
            MeshGeometry3D kMesh = new MeshGeometry3D();

            /*
            m_kVoronoi.Vertizes.OrderBy(o => o[0]);
            m_kVoronoi.Vertizes.OrderByDescending(o => o[1]);
            foreach (BenTools.Mathematics.Vector kVector in m_kVoronoi.Vertizes)
            {
                Point3D p0 = new Point3D(kVector[0], kVector[1], 0.0f);
                kMesh.TriangleIndices.Add(Helper.AddPoint(kMesh.Positions, p0));
                
                Point3D p0 = new Point3D(kEdge.LeftData[0], kEdge.LeftData[1], 0.0f);
                Point3D p1 = new Point3D(kEdge.RightData[0], kEdge.RightData[1], 0.0f);
                kMesh.TriangleIndices.Add(Helper.AddPoint(kMesh.Positions, p0));
                kMesh.TriangleIndices.Add(Helper.AddPoint(kMesh.Positions, p1));
            }
            */
            List<VoronoiEdge> akEdges = new List<VoronoiEdge>();
            
            foreach (VoronoiEdge kEdge in m_kVoronoi.Edges)
            {
                akEdges.Add(kEdge);
            }

            Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Start process: Draw Edges", LogLevel.INFO });
            for (int i = 0; i < akEdges.Count; i++)
            {
                if (akEdges[i].VVertexA == Fortune.VVUnkown || akEdges[i].VVertexA == Fortune.VVInfinite)
                {
                    Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "VVertexA unknown or infinite", LogLevel.ERROR });
                    continue;
                }
                Point3D p0 = new Point3D(akEdges[i].VVertexA[0], akEdges[i].VVertexA[1], 0.0);

                if (akEdges[i].VVertexB == Fortune.VVUnkown || akEdges[i].VVertexB == Fortune.VVInfinite)
                {
                    Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "VVertexB unknown or infinite", LogLevel.ERROR });
                    continue;
                }
                Point3D p1 = new Point3D(akEdges[i].VVertexB[0], akEdges[i].VVertexB[1], 0.0);

                if (akEdges[i].FixedPoint == Fortune.VVUnkown || akEdges[i].FixedPoint == Fortune.VVInfinite)
                {
                    Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "FixedPoint unknown or infinite", LogLevel.ERROR });
                    continue;
                }

                if ((bool)cbClearEdges.IsChecked)
                {
                    if (akEdges[i].VVertexA[0] > m_dMaximum || akEdges[i].VVertexA[0] < m_dMinimum ||
                   akEdges[i].VVertexA[1] > m_dMaximum || akEdges[i].VVertexA[1] < m_dMinimum ||
                   akEdges[i].VVertexB[0] > m_dMaximum || akEdges[i].VVertexB[0] < m_dMinimum ||
                   akEdges[i].VVertexB[1] > m_dMaximum || akEdges[i].VVertexB[1] < m_dMinimum ||
                   akEdges[i].FixedPoint[0] > m_dMaximum || akEdges[i].FixedPoint[0] < m_dMinimum ||
                   akEdges[i].FixedPoint[1] > m_dMaximum || akEdges[i].FixedPoint[1] < m_dMinimum)
                    {
                        Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Some Vertex out of range", LogLevel.DEBUG });
                        continue;
                    }
                }
                Point3D p2 = new Point3D(akEdges[i].FixedPoint[0], akEdges[i].FixedPoint[1], 0.0);

                Helper.AddTriangle(kMesh, p0, p1, p2);
            }
            Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "End process: Draw Edges", LogLevel.INFO });

            DiffuseMaterial kSurfaceMaterial = new DiffuseMaterial(Brushes.Orange);
            GeometryModel3D kSurfaceModel = new GeometryModel3D(kMesh, kSurfaceMaterial);
            kSurfaceModel.BackMaterial = kSurfaceMaterial;
            m_kMainModel3DGroup.Children.Add(kSurfaceModel);

            if ((bool)cbDrawWireframe.IsChecked)
            {
                Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Start process: Draw Wireframe", LogLevel.INFO });
                MeshGeometry3D kWireframe = kMesh.ToWireframe(0.005);
                //DiffuseMaterial kWireframeMaterial = new DiffuseMaterial(Brushes.Red);
                MaterialGroup kWireframeMaterial = new MaterialGroup();
                kWireframeMaterial.Children.Add(new DiffuseMaterial(Brushes.Black));
                kWireframeMaterial.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(200, 0, 0))));
                GeometryModel3D kWireframeModel = new GeometryModel3D(kWireframe, kWireframeMaterial);
                m_kMainModel3DGroup.Children.Add(kWireframeModel);
                Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "End process: Draw Wireframe", LogLevel.INFO });
            }

            ModelVisual3D kModelVisual = new ModelVisual3D();
            kModelVisual.Content = m_kMainModel3DGroup;
            
            vpOutputView.Children.Clear();
            GC.Collect();
            vpOutputView.Children.Add(kModelVisual);
            Dispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "End process: Draw Voronoi", LogLevel.INFO });
        }

        private void BtnGenerateVoronoi_Click(object sender, RoutedEventArgs e)
        {
            GenerateVoronoi();
        }

        private void BtnDrawVoronoi_Click(object sender, RoutedEventArgs e)
        {
            DrawVoronoi();
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

                    Helper.AddTriangle(kMesh, p00, p01, p11);
                    Helper.AddTriangle(kMesh, p00, p11, p10);
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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                case Key.W:
                    m_dCameraPhi += m_dCameraDPhi;
                    if (m_dCameraPhi > Math.PI / 2.0)
                        m_dCameraPhi = Math.PI / 2.0;
                    break;
                case Key.Down:
                case Key.S:
                    m_dCameraPhi -= m_dCameraDPhi;
                    if (m_dCameraPhi < -Math.PI / 2.0)
                        m_dCameraPhi = -Math.PI / 2.0;
                    break;
                case Key.Left:
                case Key.A:
                    m_dCameraTheta += m_dCameraDTheta;
                    break;
                case Key.Right:
                case Key.D:
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

        public void Log(string sLogMessage, LogLevel eLogLevel)
        {
            List<LogItem> akTempLogs = Helper.akLogger;
            Helper.akLogger = new List<LogItem>();
            Helper.akLogger.Add(new LogItem(sLogMessage, eLogLevel));
            Helper.akLogger.AddRange(akTempLogs);

            lbxLog.ItemsSource = null;
            lbxLog.Items.Clear();

            lbxLog.ItemsSource = Helper.akLogger;
        }
    }
}
