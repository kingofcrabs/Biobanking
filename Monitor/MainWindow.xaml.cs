using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.Threading;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using Monitor.Properties;

namespace Monitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MonitorWindow : Window
    {
        StepViewModel stepViewModel = new StepViewModel();
        WorkTableUI workTableUI = null;
        PipettingUI pipettingUI = null;
        System.Timers.Timer timer = new System.Timers.Timer(600);
        StageInfo stageInfo = new StageInfo();
        TraceListener _textBoxListener;
        RunResult runResult;

        ProgressController prgController;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public MonitorWindow()
        {
            log.Info("Monitorwindow");
            
            InitializeComponent();
            try
            {
                lblVersion.Content = stringRes.version;
                lstSteps.DataContext = stepViewModel.StepsModel;
                CreateNamedPipeServer();
            
                runResult = RunResultReader.Read();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
            }
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            this.Closing += new System.ComponentModel.CancelEventHandler(MonitorWindow_Closing);
        }

        void MonitorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Trace.Listeners.Clear();
            Pipeserver.Close();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitWorkTableUI();
            AddTracer();
            log.Info("Server started");
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Start();
        }

        private void AddTracer()
        {
            _textBoxListener = new TextBoxTraceListener(txtLog);
            _textBoxListener.Name = "Textbox";
            _textBoxListener.TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ThreadId;
            Trace.Listeners.Add(_textBoxListener);
        }


        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (workTableUI == null)
                return;
            Action invalidateWorkTableUI = ()=>workTableUI.InvalidateVisual();
            Dispatcher.Invoke(invalidateWorkTableUI);

            if (pipettingUI == null)
                return;
            Action invalidatePipettingUI = () => pipettingUI.InvalidateVisual();
            Dispatcher.Invoke(invalidatePipettingUI);
        }


        private void InitWorkTableUI()
        {
            //package setting
            int smpCount = Helper.ReadSampleCount();
            txtSampleCount.Text = smpCount.ToString();
            PipettingSettings pipettingSettings = Utility.LoadPipettingSettings();

            txtPlasmaCount.Text = pipettingSettings.dstPlasmaSlice.ToString();//pipettingSettings.dstPlasmaSlice.ToString();
            txtBuffyCount.Text = pipettingSettings.dstbuffySlice.ToString();

            PackageInfo packageInfo = new PackageInfo(smpCount, runResult.plasmaTotalSlice, runResult.buffySlice);
            prgController = new ProgressController(txtThisStageUsed,
                txtThisStageRemaining,
                txtTotalUsed,
                txtTotalRemaining,
                prgThisStage,
                prgTotal,
                packageInfo);

            int wellsPerLabware = 16;
            int srcSmpGrids = int.Parse(ConfigurationManager.AppSettings["SrcSampleGrids"]);
            owenerDrawGrid.RowDefinitions[0].Height = new GridLength(this.ActualHeight * 0.8);
            workTableUI = new WorkTableUI(workTableGrid.ActualWidth, workTableGrid.ActualHeight, srcSmpGrids, wellsPerLabware, prgController);
            workTableGrid.Children.Add(workTableUI);

            stageInfo.curStage = Stage.Measure;
            stageInfo.farthestStage = Stage.Measure;
            lstSteps.SelectedIndex = 0;
        }

      

        private void workTableGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (workTableUI == null)
                return;
            workTableUI.BoundingSize = e.NewSize;
        }
        private void pipettingGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (pipettingUI == null)
                return;
            pipettingUI.BoundingSize = e.NewSize;
        }

        private void lstSteps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
        }
#region communication
        private void CreateNamedPipeServer()
        {
            Pipeserver.owner = this;
            Pipeserver.ownerInvoker = new Invoker(this);
            ThreadStart pipeThread = new ThreadStart(Pipeserver.createPipeServer);
            Thread listenerThread = new Thread(pipeThread);
            listenerThread.SetApartmentState(ApartmentState.STA);
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        internal void ExecuteCommand(string sCommand)
        {
            sCommand = sCommand.ToLower();
            log.Info(sCommand);
            runResult = RunResultReader.Read();
            if (sCommand.Contains("shutdown"))
            {
                this.Close();
                return;
            }
            string[] strs = sCommand.Split(';');
            string stage = strs[0];
            if (stage == Stage.Measure.ToString().ToLower())
            {
                bool bFinished = bool.Parse(strs[1]);
                if (bFinished)
                {
                    workTableUI.FinishedMeasure();
                    return;
                }
                int smpID = int.Parse(strs[2]);
                bool bGo2MeasurePos = bool.Parse(strs[3]);
                workTableUI.MoveTube(smpID, bGo2MeasurePos);
            }
            else if (stage == Stage.Pipetting.ToString().ToLower())
            {
                if (stageInfo.curStage == Stage.Measure)
                {
                    InitPipettingUI();
                    stageInfo.farthestStage = Stage.Pipetting;
                    stageInfo.curStage = Stage.Pipetting;
                    lstSteps.SelectedIndex = 1;
                    workTableUI.CurStage = Stage.Pipetting;
                }

                bool bFinished = false;
                bool isBool = bool.TryParse(strs[1], out bFinished);
                if (isBool && bFinished)
                {
                    pipettingUI.FinishedPipetting();
                    workTableUI.FinishedPipetting();
                    return;
                }
                int rackIndex = int.Parse(strs[1]);
                int batchIndex = int.Parse(strs[2]);
                int sliceIndex = int.Parse(strs[3]);
                pipettingUI.UpdatePipettingProgress(rackIndex, batchIndex, sliceIndex);
            }
        }

        private void InitPipettingUI()
        {
            int wellsPerLabware = 16;
            owenerDrawGrid.RowDefinitions[0].Height = new GridLength(this.ActualHeight * 0.6);
            pipettingUI = new PipettingUI(pipettingGrid.ActualWidth, pipettingGrid.ActualHeight, wellsPerLabware, prgController);
            pipettingUI.Visibility = System.Windows.Visibility.Visible;
            pipettingGrid.Children.Add(pipettingUI);
        }
#endregion

        private void lstSteps_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //var item = ItemsControl.ContainerFromElement(lstSteps, e.OriginalSource as DependencyObject) as ListBoxItem;
            //if (item != null)
            //{
            //    Stage stage2Go = ((StepDesc)item.Content).CorrespondingStage;
            //    if (stage2Go == stageInfo.curStage)
            //        return;

            //    if (stage2Go > stageInfo.farthestStage)
            //    {
            //        return;
            //    }
            //    else
            //    {
            //        workTableUI.CurStage = stage2Go;
            //        stageInfo.curStage = stage2Go;
            //        if (stage2Go == Stage.Measure)
            //        {
            //            pipettingUI.Visibility = System.Windows.Visibility.Hidden;
            //        }
            //        else
            //        {
            //            pipettingUI.Visibility = System.Windows.Visibility.Visible;
            //        }
            //    }
            //}
        }

    }


    class StageInfo
    {
        public Stage curStage;
        public Stage farthestStage;
    }
}
