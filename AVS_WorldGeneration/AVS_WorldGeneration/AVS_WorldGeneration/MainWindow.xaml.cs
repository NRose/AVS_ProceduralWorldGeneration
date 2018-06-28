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
using System.Windows.Threading;
using System.Management;
using Fluent;
using System.IO;
using System.ComponentModel;

namespace AVS_WorldGeneration
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        #region DELEGATES

        delegate void VoronoiProgress(double bValue);
        delegate void Logging(string sLogMessage, LogLevel eLogLevel);

        #endregion

        #region VARIABLES - Voronoi

        private List<List<BenTools.Mathematics.Vector>> m_acVectors = new List<List<BenTools.Mathematics.Vector>>();
        private Random m_kRnd = new Random();
        private int m_nSeed = 0;
        private double[] m_dVector;
        private VoronoiGraph m_kVoronoi;

        private const double m_dMinimum = -1.0;
        private const double m_dMaximum = 1.0;

        #endregion

        #region VARIABLES - 3D

        private Model3DGroup m_kMainModel3DGroup = new Model3DGroup();
        private PerspectiveCamera m_kMainCamera;

        private double m_dCameraPhi = Math.PI / 6.0;
        private double m_dCameraTheta = Math.PI / 6.0;
        private double m_dCameraR = 3.0;

        private const double m_dCameraDPhi = 0.1;
        private const double m_dCameraDTheta = 0.1;
        private const double m_dCameraDR = 0.1;

        #endregion

        #region VARIABLES - Local Distribution
        
        private int m_nPhysicalProcessors = 0;
        private int m_nCores = 0;
        private int m_nLogicalProcessors = 0;
        private int m_nThreadsInUse = 1;

        private List<uint> m_acCurrentCPUFrequency;
        private List<uint> m_acMaxCPUFrequency;

        private double m_dVoronoiCount_Threaded = 0.0;
        private int m_nVoronoiFinished_Threaded = 0;

        #endregion

        #region VARIABLES - Network Distribution

        private Object m_cObjThreadLocking = new Object();
        private Object m_cObjProgressbarLocking = new Object();

        private NetworkManager m_cNetworkManager;
        public List<Helper.Node> acAvailableNodes;
        
        #endregion

        #region General Main Window

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

            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
            {
                m_nPhysicalProcessors = int.Parse(item["NumberOfProcessors"].ToString());
                m_nLogicalProcessors = int.Parse(item["NumberOfLogicalProcessors"].ToString());
            }
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                m_nCores += int.Parse(item["NumberOfCores"].ToString());
            }

            m_acCurrentCPUFrequency = new List<uint>();
            m_acMaxCPUFrequency = new List<uint>();

            for(int i = 0; i < m_nPhysicalProcessors; i++)
            {
                string sCPU = "Win32_Processor.DeviceID='CPU" + i.ToString() + "'";
                using (ManagementObject item = new ManagementObject(sCPU))
                {
                    m_acCurrentCPUFrequency.Add((uint)(item["CurrentClockSpeed"]));
                    m_acMaxCPUFrequency.Add((uint)(item["MaxClockSpeed"]));
                }
            }
            tllbSystemInfo.Content = "Number of physical processors:\t" + m_nPhysicalProcessors.ToString() + "\nNumber of cores:\t\t\t" + m_nCores.ToString() + "\nNumber of logical processors:\t" + m_nLogicalProcessors.ToString() + "\nNumber of threads in use:\t\t" + m_nThreadsInUse.ToString();

            m_cNetworkManager = new NetworkManager();
            acAvailableNodes = new List<Helper.Node>();
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

            for (double x = dXMin; x <= dXMax - dX; x += dX)
            {
                for (double z = dZMin; z <= dZMax - dZ; z += dZ)
                {
                    Point3D p00 = new Point3D(x, Helper.F(x, z), z);
                    Point3D p10 = new Point3D(x + dX, Helper.F(x + dX, z), z);
                    Point3D p01 = new Point3D(x, Helper.F(x, z + dZ), z + dZ);
                    Point3D p11 = new Point3D(x + dX, Helper.F(x + dX, z + dZ), z + dZ);

                    Helper.AddTriangle(kMesh, p00, p01, p11);
                    Helper.AddTriangle(kMesh, p00, p11, p10);
                }
            }

            DiffuseMaterial kSurfaceMaterial = new DiffuseMaterial(Brushes.Orange);
            GeometryModel3D kSurfaceModel = new GeometryModel3D(kMesh, kSurfaceMaterial);
            kSurfaceModel.BackMaterial = kSurfaceMaterial;
            kModelGroup.Children.Add(kSurfaceModel);
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

        private void IncreaseProgress(double dValue)
        {
            pbGenerateVoronoi.Value += dValue;
        }

        #endregion

        #region Tab World Generation

        private void BtnGenerateVoronoi_Click(object sender, RoutedEventArgs e)
        {
            GenerateVoronoi();
        }

        private void BtnDrawVoronoi_Click(object sender, RoutedEventArgs e)
        {
            DrawVoronoi();
        }

        private void BtnClearVoronoi_Click(object sender, RoutedEventArgs e)
        {
            ClearVoronoi();
        }

        private void GenerateVoronoi()
        {
            Logging systemLog = this.Log;
            int nSeed = 0;
            if (!int.TryParse(tbxSeed.Text, out nSeed))
            {
                Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Seed contains illegal characters!", LogLevel.WARN });
                return;
            }
            m_nSeed = nSeed;
            m_kRnd = new Random(m_nSeed);

            VoronoiProgress updatePB = IncreaseProgress;

            double dVoronoiCount = 0;
            if (!double.TryParse(tbxVoronoiLoopCount.Text, out dVoronoiCount))
            {
                Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Loop count contains illegal characters!", LogLevel.WARN });
                return;
            }

            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Start process: Generate World", LogLevel.INFO });
            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Info: Seed: " + m_nSeed.ToString(), LogLevel.INFO });
            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Info: Loop count: " + dVoronoiCount.ToString(), LogLevel.INFO });

            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Info: Physical processors: " + m_nPhysicalProcessors.ToString(), LogLevel.INFO });
            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Info: Cores: " + m_nCores.ToString(), LogLevel.INFO });
            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Info: Logical processors: " + m_nLogicalProcessors.ToString(), LogLevel.INFO });
            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Info: Threads in use: " + m_nThreadsInUse.ToString(), LogLevel.INFO });

            for(int nCurrent = 0; nCurrent < m_acCurrentCPUFrequency.Count; nCurrent++)
            {
                Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Info: CPU" + nCurrent + " current frequency: " + m_acCurrentCPUFrequency[nCurrent].ToString(), LogLevel.INFO });
                Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Info: CPU" + nCurrent + " maximal frequency: " + m_acMaxCPUFrequency[nCurrent].ToString(), LogLevel.INFO });
            }

            double dOneStep = 100.0 / (dVoronoiCount + (dVoronoiCount * 0.33f));
            m_acVectors = new List<List<BenTools.Mathematics.Vector>>();
            /*m_akVectors.Add(new BenTools.Mathematics.Vector(new double[] { dMinimum, dMinimum }));
            m_akVectors.Add(new BenTools.Mathematics.Vector(new double[] { dMinimum, dMaximum }));
            m_akVectors.Add(new BenTools.Mathematics.Vector(new double[] { dMaximum, dMinimum }));
            m_akVectors.Add(new BenTools.Mathematics.Vector(new double[] { dMaximum, dMaximum }));*/
            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Start process: Randomizing Vectors", LogLevel.INFO });

            if (m_nThreadsInUse > 1)
            {
                m_dVoronoiCount_Threaded = dVoronoiCount / m_nThreadsInUse;
                double dExtra = dVoronoiCount % m_nThreadsInUse;

                for (int i = 0; i < m_nThreadsInUse; i++)
                {
                    Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Start process: Thread " + (i + 1).ToString() + "/" + m_nThreadsInUse.ToString() + " Generation", LogLevel.INFO });
                    BackgroundWorker cWorker = new BackgroundWorker
                    {
                        WorkerReportsProgress = false,
                        WorkerSupportsCancellation = false
                    };
                    cWorker.DoWork += GenerateVoronoiVectors;
                    cWorker.RunWorkerCompleted += GenerateVoronoiVectorsFinished;

                    cWorker.RunWorkerAsync(dVoronoiCount / m_nThreadsInUse + dExtra);
                    dExtra = 0.0;
                }
            }
            else
            {
                Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Start process: Main Thread Generation", LogLevel.INFO });
                m_acVectors.Add(new List<BenTools.Mathematics.Vector>());
                for (int i = 0; i < dVoronoiCount; i++)
                {
                    m_dVector = new double[] { m_kRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum, m_kRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum };
                    m_acVectors[0].Add(new BenTools.Mathematics.Vector(m_dVector));
                    pbGenerateVoronoi.Dispatcher.Invoke(updatePB, System.Windows.Threading.DispatcherPriority.Background, new object[] { dOneStep });
                }
                m_nVoronoiFinished_Threaded++;
                Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "End process: Main Thread Generation", LogLevel.INFO });
            }
            BackgroundWorker cWorkerMain = new BackgroundWorker
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false
            };
            cWorkerMain.DoWork += GenerateVoronoiThread;

            cWorkerMain.RunWorkerAsync(dVoronoiCount);
        }

        private void GenerateVoronoiEnd()
        {
            Logging systemLog = this.Log;

            VoronoiProgress updatePB = IncreaseProgress;

            Application.Current.Dispatcher.Invoke(new Action(() => AddLog("End process: Randomizing Vectors", LogLevel.INFO)));

            //m_akVectors = m_akVectors.OrderBy(o => o[0]).ThenBy(o => o[1]).ToList();
            Application.Current.Dispatcher.Invoke(new Action(() => AddLog("Start process: Generate Voronoi", LogLevel.INFO)));

            List<BenTools.Mathematics.Vector> acFinalList = new List<BenTools.Mathematics.Vector>();

            for(int i = 0; i < m_acVectors.Count; i++)
            {
                acFinalList.AddRange(m_acVectors[i]);
            }

            m_kVoronoi = Fortune.ComputeVoronoiGraph(acFinalList);
            Application.Current.Dispatcher.Invoke(new Action(() => AddLog("End process: Generate Voronoi", LogLevel.INFO)));
            pbGenerateVoronoi.Dispatcher.Invoke(updatePB, System.Windows.Threading.DispatcherPriority.Background, new object[] { (100.0 - pbGenerateVoronoi.Value) });
            if (!btnClearVoronoi.IsEnabled)
            {
                btnDrawVoronoi.IsEnabled = true;
            }
            vpOutputView.Focus();
            Application.Current.Dispatcher.Invoke(new Action(() => AddLog("End process: Generate World", LogLevel.INFO)));
        }

        private void DrawVoronoi()
        {
            btnDrawVoronoi.IsEnabled = false;
            Logging systemLog = this.Log;

            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Start process: Draw Voronoi", LogLevel.INFO });
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

            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Start process: Draw Edges", LogLevel.INFO });
            for (int i = 0; i < akEdges.Count; i++)
            {
                if (akEdges[i].VVertexA == Fortune.VVUnkown || akEdges[i].VVertexA == Fortune.VVInfinite)
                {
                    Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "VVertexA unknown or infinite", LogLevel.ERROR });
                    continue;
                }
                Point3D p0 = new Point3D(akEdges[i].VVertexA[0], akEdges[i].VVertexA[1], 0.0);

                if (akEdges[i].VVertexB == Fortune.VVUnkown || akEdges[i].VVertexB == Fortune.VVInfinite)
                {
                    Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "VVertexB unknown or infinite", LogLevel.ERROR });
                    continue;
                }
                Point3D p1 = new Point3D(akEdges[i].VVertexB[0], akEdges[i].VVertexB[1], 0.0);

                if (akEdges[i].FixedPoint == Fortune.VVUnkown || akEdges[i].FixedPoint == Fortune.VVInfinite)
                {
                    Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "FixedPoint unknown or infinite", LogLevel.ERROR });
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
                        Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Some Vertex out of range", LogLevel.DEBUG });
                        continue;
                    }
                }
                Point3D p2 = new Point3D(akEdges[i].FixedPoint[0], akEdges[i].FixedPoint[1], 0.0);

                Helper.AddTriangle(kMesh, p0, p1, p2);
            }
            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "End process: Draw Edges", LogLevel.INFO });

            DiffuseMaterial kSurfaceMaterial = new DiffuseMaterial(Brushes.Orange);
            GeometryModel3D kSurfaceModel = new GeometryModel3D(kMesh, kSurfaceMaterial);
            kSurfaceModel.BackMaterial = kSurfaceMaterial;
            m_kMainModel3DGroup.Children.Add(kSurfaceModel);

            if ((bool)cbDrawWireframe.IsChecked)
            {
                Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Start process: Draw Wireframe", LogLevel.INFO });
                MeshGeometry3D kWireframe = kMesh.ToWireframe(0.005);
                //DiffuseMaterial kWireframeMaterial = new DiffuseMaterial(Brushes.Red);
                MaterialGroup kWireframeMaterial = new MaterialGroup();
                kWireframeMaterial.Children.Add(new DiffuseMaterial(Brushes.Black));
                kWireframeMaterial.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(200, 0, 0))));
                GeometryModel3D kWireframeModel = new GeometryModel3D(kWireframe, kWireframeMaterial);
                m_kMainModel3DGroup.Children.Add(kWireframeModel);
                Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "End process: Draw Wireframe", LogLevel.INFO });
            }

            ModelVisual3D kModelVisual = new ModelVisual3D();
            kModelVisual.Content = m_kMainModel3DGroup;
            
            vpOutputView.Children.Clear();
            GC.Collect();
            vpOutputView.Children.Add(kModelVisual);
            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "End process: Draw Voronoi", LogLevel.INFO });
            btnClearVoronoi.IsEnabled = true;
        }

        private void ClearVoronoi()
        {
            btnClearVoronoi.IsEnabled = false;
            Logging systemLog = this.Log;

            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Start process: Clear Voronoi", LogLevel.INFO });

            m_kMainModel3DGroup = new Model3DGroup();
            vpOutputView.Children.Clear();
            GC.Collect();
            DefineLights();

            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "End process: Clear Voronoi", LogLevel.INFO });
            btnDrawVoronoi.IsEnabled = true;
        }

        #endregion

        #region Tab Distirbution

        private void BtnUseThreads_Click(object sender, RoutedEventArgs e)
        {
            if(!int.TryParse(tbxThreadCount.Text, out m_nThreadsInUse))
            {
                //HAT NICHT GEKLAPPT TODO: FEHLERMELDUNG ANZEIGEN
            }
            else
            {
                tllbSystemInfo.Content = "Number of physical processors:\t" + m_nPhysicalProcessors.ToString() + "\nNumber of cores:\t\t\t" + m_nCores.ToString() + "\nNumber of logical processors:\t" + m_nLogicalProcessors.ToString() + "\nNumber of threads in use:\t\t" + m_nThreadsInUse.ToString();
            }
        }

        private void BtnSearchForNodes_Click(object sender, RoutedEventArgs e)
        {
            lbxNodes.ItemsSource = null;
            m_cNetworkManager.InitializeNetworkManager();
        }
        
        public void TglNode_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox cbxCheck = (sender as System.Windows.Controls.CheckBox);

            acAvailableNodes.Find(o => o.sIPAddress == cbxCheck.Tag.ToString()).bInUse = (bool)cbxCheck.IsChecked;
            UpdateNodeList();
        }

        public void PlusNode_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btnPlus = (sender as System.Windows.Controls.Button);

            acAvailableNodes.Find(o => o.sIPAddress == btnPlus.Tag.ToString()).nThreads += 1;
            if (acAvailableNodes.Find(o => o.sIPAddress == btnPlus.Tag.ToString()).nThreads > 99)
            {
                acAvailableNodes.Find(o => o.sIPAddress == btnPlus.Tag.ToString()).nThreads = 99;
            }
            UpdateNodeList();
        }

        public void MinusNode_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btnMinus = (sender as System.Windows.Controls.Button);

            acAvailableNodes.Find(o => o.sIPAddress == btnMinus.Tag.ToString()).nThreads -= 1;
            if(acAvailableNodes.Find(o => o.sIPAddress == btnMinus.Tag.ToString()).nThreads < 0)
            {
                acAvailableNodes.Find(o => o.sIPAddress == btnMinus.Tag.ToString()).nThreads = 0;
            }
            UpdateNodeList();
        }

        public void UpdateNodeList()
        {
            lbxNodes.ItemsSource = null;
            lbxNodes.Items.Clear();
            lbxNodes.ItemsSource = acAvailableNodes;
        }

        private void GenerateVoronoiThread(object sender, DoWorkEventArgs e)
        {
            while (m_nVoronoiFinished_Threaded < m_nThreadsInUse)
            {
                Thread.Sleep(1);
            }
            Application.Current.Dispatcher.Invoke(new Action(() => GenerateVoronoiEnd()));
        }

        private void GenerateVoronoiVectors(object sender, DoWorkEventArgs e)
        {
            VoronoiProgress updatePB = IncreaseProgress;
            double dOneStep = 100.0 / m_nThreadsInUse / (double)e.Argument;//(m_dVoronoiCount_Threaded + (m_dVoronoiCount_Threaded * 0.33f));
            List<BenTools.Mathematics.Vector> acTempList = new List<BenTools.Mathematics.Vector>();

            for (int i = 0; i < (double)e.Argument; i++)
            {
                m_dVector = new double[] { m_kRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum, m_kRnd.NextDouble() * (m_dMaximum - m_dMinimum) + m_dMinimum };
                acTempList.Add(new BenTools.Mathematics.Vector(m_dVector));
                pbGenerateVoronoi.Dispatcher.Invoke(updatePB, System.Windows.Threading.DispatcherPriority.Background, new object[] { dOneStep });
            }

            lock(m_cObjThreadLocking)
            {
                m_acVectors.Add(acTempList);
            }
        }

        private void GenerateVoronoiVectorsFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            m_nVoronoiFinished_Threaded++;
            if (e.Cancelled)
            {
                Application.Current.Dispatcher.Invoke(new Action(() => AddLog("End process: Thread " + m_nVoronoiFinished_Threaded.ToString() + "/" + m_nThreadsInUse.ToString() + " Generation", LogLevel.WARN)));
            }
            else if (e.Error != null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() => AddLog("End process: Thread " + m_nVoronoiFinished_Threaded.ToString() + "/" + m_nThreadsInUse.ToString() + " Generation", LogLevel.ERROR)));
            }
            else
            {
                Application.Current.Dispatcher.Invoke(new Action(() => AddLog("End process: Thread " + m_nVoronoiFinished_Threaded.ToString() + "/" + m_nThreadsInUse.ToString() + " Generation", LogLevel.INFO)));
            }
        }
        
        private void AddLog(string sMessage, LogLevel eLogLevel)
        {
            Logging systemLog = this.Log;
            Dispatcher.CurrentDispatcher.Invoke(systemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { sMessage, eLogLevel });
        }

        #endregion

        #region Log System

        private void BtnClearLogPreview_Click(object sender, RoutedEventArgs e)
        {
            Logging systemLog = this.Log;
            Dispatcher.CurrentDispatcher.Invoke(systemLog, DispatcherPriority.Background, args: new object[] { "::CLEAR::LOG::PREVIEW::", LogLevel.NONE });
            UpdateLogPreview();
        }

        private void BtnExportLog_Click(object sender, RoutedEventArgs e)
        {
            string sLogFileName = "LogFile_" + DateTime.Now.ToShortDateString().Replace(".", "-") + "_" + DateTime.Now.ToLongTimeString().Replace(":", "-") + ".txt";
            StreamWriter cSW = new StreamWriter(sLogFileName);

            foreach (LogItem cItem in lbxLog.ItemsSource)
            {
                cSW.WriteLine(cItem.sLogMessage);
            }

            cSW.Close();
            lbxLog.ItemsSource = null;
            lbxLog.Items.Clear();
            sbiLastLog.Value = sLogFileName + " successfully saved!     ";
        }

        private void UpdateLogPreview(object sender, RoutedEventArgs e)
        {
            UpdateLogPreview();
        }

        public void UpdateLogPreview()
        {
            if (lbxLog == null || sbiLastLog == null)
            {
                return;
            }

            List<LogItem> akTempLogs = new List<LogItem>();

            for (int i = 0; i < Helper.akLogger.Count; i++)
            {
                switch (Helper.akLogger[i].eLogLevel)
                {
                    case LogLevel.NONE:
                        if ((bool)cbLogNone.IsChecked)
                            akTempLogs.Add(Helper.akLogger[i]);
                        else if (Helper.akLogger[i].sLogMessage == "[NONE]\t\t--- ::CLEAR::LOG::PREVIEW:: ---")
                        {
                            lbxLog.ItemsSource = null;
                            lbxLog.Items.Clear();

                            lbxLog.ItemsSource = akTempLogs;
                            return;
                        }
                        break;
                    case LogLevel.DEBUG:
                        if ((bool)cbLogDebug.IsChecked)
                            akTempLogs.Add(Helper.akLogger[i]);
                        break;
                    case LogLevel.INFO:
                        if ((bool)cbLogInfo.IsChecked)
                            akTempLogs.Add(Helper.akLogger[i]);
                        break;
                    case LogLevel.WARN:
                        if ((bool)cbLogWarn.IsChecked)
                            akTempLogs.Add(Helper.akLogger[i]);
                        break;
                    case LogLevel.ERROR:
                        if ((bool)cbLogError.IsChecked)
                            akTempLogs.Add(Helper.akLogger[i]);
                        break;
                    case LogLevel.CRITICAL:
                        if ((bool)cbLogCritical.IsChecked)
                            akTempLogs.Add(Helper.akLogger[i]);
                        break;
                    case LogLevel.FATAL:
                        if ((bool)cbLogFatal.IsChecked)
                            akTempLogs.Add(Helper.akLogger[i]);
                        break;
                    default:
                        break;
                }
            }
            lbxLog.ItemsSource = null;
            lbxLog.Items.Clear();

            lbxLog.ItemsSource = akTempLogs;
            sbiLastLog.Value = "Last log message:     " + akTempLogs[0].sLogMessage + "     ";
        }

        public void Log(string sLogMessage, LogLevel eLogLevel)
        {
            List<LogItem> akTempLogs = Helper.akLogger;
            Helper.akLogger = new List<LogItem>();
            Helper.akLogger.Add(new LogItem(sLogMessage, eLogLevel));
            Helper.akLogger.AddRange(akTempLogs);
            UpdateLogPreview();
        }

        #endregion
    }
}
