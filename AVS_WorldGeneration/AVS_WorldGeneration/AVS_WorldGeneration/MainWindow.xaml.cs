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
using System.Net;

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

        private Logging m_cSystemLog;// = Log;
        private VoronoiProgress m_cUpdatePB;// = IncreaseProgress;

        #endregion

        #region VARIABLES - Voronoi
        
        private VoronoiGraph m_cVoronoi;

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
        

        #endregion

        #region VARIABLES - Network Distribution


        private DistributionManager m_cDistributionManager;

        #endregion

        #region General Main Window

        public MainWindow()
        {
            InitializeComponent();

            m_cSystemLog = Log;
            m_cUpdatePB = IncreaseProgress;

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

            m_cDistributionManager = new DistributionManager(m_dMinimum, m_dMaximum);
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

        public void IncreaseProgress(double dValue)
        {
            pbGenerateVoronoi.Value += dValue;
        }

        #endregion

        #region Tab World Generation

        private void BtnGenerateVoronoi_Click(object sender, RoutedEventArgs e)
        {
            int nSeed = 0;
            if (!int.TryParse((Application.Current.MainWindow as MainWindow).tbxSeed.Text, out nSeed))
            {
                Dispatcher.CurrentDispatcher.Invoke(m_cSystemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Seed contains illegal characters!", LogLevel.WARN });
                return;
            }
            double dVoronoiCount = 0;
            if (!double.TryParse((Application.Current.MainWindow as MainWindow).tbxVoronoiLoopCount.Text, out dVoronoiCount))
            {
                Dispatcher.CurrentDispatcher.Invoke(m_cSystemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { "Loop count contains illegal characters!", LogLevel.WARN });
                return;
            }

            m_cDistributionManager.DistributeWork(nSeed, dVoronoiCount);
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
            /*
            if (m_bNetworkDistribution)
            {
                List<IPAddress> acAddresses = new List<IPAddress>();
                //List<WcfCommunication.VoronoiData> acData = new List<WcfCommunication.VoronoiData>();

                int nSingleCount = Convert.ToInt32(dVoronoiCount / m_cNetworkManager.acDistributors.Count);
                m_dProgressStepForNetworkDistribution = (double)nSingleCount;

                foreach (Helper.Distributor cDist in m_cNetworkManager.acDistributors)
                {
                    acAddresses.Add(cDist.cAddress);

                    WcfCommunication.VoronoiData cData = new WcfCommunication.VoronoiData();

                    //cData.Minimum = m_dMinimum;
                    //cData.Maximum = m_dMaximum;
                    //cData.Threads = cDist.nCores;
                    //cData.Count = nSingleCount;

                    //acData.Add(cData);
                }

                //ServiceCallHelper.RunDistribution(acAddresses, 8733, acData);
            }
            else
            {

                m_akVectors.Add(new BenTools.Mathematics.Vector(new double[] { dMinimum, dMinimum }));
                m_akVectors.Add(new BenTools.Mathematics.Vector(new double[] { dMinimum, dMaximum }));
                m_akVectors.Add(new BenTools.Mathematics.Vector(new double[] { dMaximum, dMinimum }));
                m_akVectors.Add(new BenTools.Mathematics.Vector(new double[] { dMaximum, dMaximum }));
            }
            */
        }

        public void GenerateVoronoiEnd()
        {
            Application.Current.Dispatcher.Invoke(new Action(() => AddLog("End process: Randomizing Vectors", LogLevel.INFO)));
            Application.Current.Dispatcher.Invoke(new Action(() => AddLog("Start process: Generate Voronoi", LogLevel.INFO)));

            List<BenTools.Mathematics.Vector> acFinalList = new List<BenTools.Mathematics.Vector>();

            for(int i = 0; i < m_cDistributionManager.acVectors.Count; i++)
            {
                acFinalList.AddRange(m_cDistributionManager.acVectors[i]);
            }

            m_cVoronoi = Fortune.ComputeVoronoiGraph(acFinalList);
            Application.Current.Dispatcher.Invoke(new Action(() => AddLog("End process: Generate Voronoi", LogLevel.INFO)));
            LogVoronoiProgress(100.0 - pbGenerateVoronoi.Value);
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

            AddLog("Start process: Draw Voronoi", LogLevel.INFO);
            MeshGeometry3D kMesh = new MeshGeometry3D();

            List<VoronoiEdge> acEdges = new List<VoronoiEdge>();
            
            foreach (VoronoiEdge cEdge in m_cVoronoi.Edges)
            {
                acEdges.Add(cEdge);
            }

            AddLog("Start process: Draw Edges", LogLevel.INFO);
            for (int i = 0; i < acEdges.Count; i++)
            {
                if (acEdges[i].VVertexA == Fortune.VVUnkown || acEdges[i].VVertexA == Fortune.VVInfinite)
                {
                    AddLog("VVertexA unknown or infinite", LogLevel.ERROR);
                    continue;
                }
                Point3D p0 = new Point3D(acEdges[i].VVertexA[0], acEdges[i].VVertexA[1], 0.0);

                if (acEdges[i].VVertexB == Fortune.VVUnkown || acEdges[i].VVertexB == Fortune.VVInfinite)
                {
                    AddLog("VVertexB unknown or infinite", LogLevel.ERROR);
                    continue;
                }
                Point3D p1 = new Point3D(acEdges[i].VVertexB[0], acEdges[i].VVertexB[1], 0.0);

                if (acEdges[i].FixedPoint == Fortune.VVUnkown || acEdges[i].FixedPoint == Fortune.VVInfinite)
                {
                    AddLog("FixedPoint unknown or infinite", LogLevel.ERROR);
                    continue;
                }

                if ((bool)cbClearEdges.IsChecked)
                {
                    if (acEdges[i].VVertexA[0] > m_dMaximum || acEdges[i].VVertexA[0] < m_dMinimum ||
                   acEdges[i].VVertexA[1] > m_dMaximum || acEdges[i].VVertexA[1] < m_dMinimum ||
                   acEdges[i].VVertexB[0] > m_dMaximum || acEdges[i].VVertexB[0] < m_dMinimum ||
                   acEdges[i].VVertexB[1] > m_dMaximum || acEdges[i].VVertexB[1] < m_dMinimum ||
                   acEdges[i].FixedPoint[0] > m_dMaximum || acEdges[i].FixedPoint[0] < m_dMinimum ||
                   acEdges[i].FixedPoint[1] > m_dMaximum || acEdges[i].FixedPoint[1] < m_dMinimum)
                    {
                        AddLog("Some Vertex out of range", LogLevel.DEBUG);
                        continue;
                    }
                }
                Point3D p2 = new Point3D(acEdges[i].FixedPoint[0], acEdges[i].FixedPoint[1], 0.0);

                Helper.AddTriangle(kMesh, p0, p1, p2);
            }
            AddLog("End process: Draw Edges", LogLevel.INFO);

            DiffuseMaterial kSurfaceMaterial = new DiffuseMaterial(Brushes.Orange);
            GeometryModel3D kSurfaceModel = new GeometryModel3D(kMesh, kSurfaceMaterial);
            kSurfaceModel.BackMaterial = kSurfaceMaterial;
            m_kMainModel3DGroup.Children.Add(kSurfaceModel);

            if ((bool)cbDrawWireframe.IsChecked)
            {
                AddLog("Start process: Draw Wireframe", LogLevel.INFO);
                MeshGeometry3D kWireframe = kMesh.ToWireframe(0.005);
                //DiffuseMaterial kWireframeMaterial = new DiffuseMaterial(Brushes.Red);
                MaterialGroup kWireframeMaterial = new MaterialGroup();
                kWireframeMaterial.Children.Add(new DiffuseMaterial(Brushes.Black));
                kWireframeMaterial.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(200, 0, 0))));
                GeometryModel3D kWireframeModel = new GeometryModel3D(kWireframe, kWireframeMaterial);
                m_kMainModel3DGroup.Children.Add(kWireframeModel);
                AddLog("End process: Draw Wireframe", LogLevel.INFO);
            }

            ModelVisual3D kModelVisual = new ModelVisual3D();
            kModelVisual.Content = m_kMainModel3DGroup;
            
            vpOutputView.Children.Clear();
            GC.Collect();
            vpOutputView.Children.Add(kModelVisual);
            AddLog("End process: Draw Voronoi", LogLevel.INFO);
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
            int nThreads = 0;
            if(!int.TryParse(tbxThreadCount.Text, out nThreads))
            {
                //HAT NICHT GEKLAPPT TODO: FEHLERMELDUNG ANZEIGEN
            }
            else
            {
                m_cDistributionManager.UpdateOwnThreads(nThreads);
            }
        }

        private void BtnSearchForNodes_Click(object sender, RoutedEventArgs e)
        {
            lbxNodes.ItemsSource = null;
            m_cDistributionManager.SearchNodes();
        }
        
        public void TglNode_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox cbxCheck = (sender as System.Windows.Controls.CheckBox);

            m_cDistributionManager.acAvailableNodes.Find(o => o.sIPAddress == cbxCheck.Tag.ToString()).bInUse = (bool)cbxCheck.IsChecked;
            UpdateNodeList();
        }

        public void PlusNode_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btnPlus = (sender as System.Windows.Controls.Button);

            if (m_cDistributionManager.acAvailableNodes.Find(o => o.sIPAddress == btnPlus.Tag.ToString()).bThreads < 99)
            {
                m_cDistributionManager.acAvailableNodes.Find(o => o.sIPAddress == btnPlus.Tag.ToString()).bThreads += 1;
            }
            UpdateNodeList();
        }

        public void MinusNode_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btnMinus = (sender as System.Windows.Controls.Button);

            if(m_cDistributionManager.acAvailableNodes.Find(o => o.sIPAddress == btnMinus.Tag.ToString()).bThreads > 1)
            {
                m_cDistributionManager.acAvailableNodes.Find(o => o.sIPAddress == btnMinus.Tag.ToString()).bThreads -= 1;
            }
            UpdateNodeList();
        }

        public void UpdateNodeList()
        {
            lbxNodes.ItemsSource = null;
            lbxNodes.Items.Clear();
            lbxNodes.ItemsSource = m_cDistributionManager.acAvailableNodes;
        }
        
        public void AddLog(string sMessage, LogLevel eLogLevel)
        {
            Dispatcher.CurrentDispatcher.Invoke(m_cSystemLog, System.Windows.Threading.DispatcherPriority.Background, new object[] { sMessage, eLogLevel });
        }

        private double m_dProgressStepForNetworkDistribution;

        public void AddNetworkResult(List<BenTools.Mathematics.Vector> acResult)
        {/*
            VoronoiProgress updatePB = IncreaseProgress;
            pbGenerateVoronoi.Dispatcher.Invoke(updatePB, System.Windows.Threading.DispatcherPriority.Background, new object[] { m_dProgressStepForNetworkDistribution });
            lock (m_cObjThreadLocking)
            {
                m_acVectors.Add(acResult);
            }*/
        }
        
        #endregion

        #region Log System

        public void LogVoronoiProgress(double dStep)
        {
            pbGenerateVoronoi.Dispatcher.Invoke(m_cUpdatePB, System.Windows.Threading.DispatcherPriority.Background, new object[] { dStep });
        }

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
