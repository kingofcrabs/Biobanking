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
using System.IO;
using SampleInfo.Properties;
using System.Configuration;
using System.Data;
using Settings;
using System.Reflection;


namespace SampleInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int sampleCount = 16, plasmaMaxCount,buffyMaxCount;
        bool bok = false;

        string xmlFolder = Utility.GetExeFolder();
        string sLabwareSettingFileName;
        string sPipettingFileName;
        //string sTubeSettingsFileName;

        LabwareSettings labwareSettings = new LabwareSettings();
        PipettingSettings pipettingSettings = new PipettingSettings();
        //TubeSettings tubeSettings = new TubeSettings();
        int maxSampleCount = 0;
        bool buffyStandalone = false;
        PlateViewer plateViewer;
        public MainWindow()
        {
            plateViewer = new PlateViewer(new Size(400,300), new Size(30, 40));
            InitializeComponent();
            string exePath = "";
            try
            {
                sLabwareSettingFileName = xmlFolder + "\\labwareSettings.xml";
                sPipettingFileName = xmlFolder + "\\pipettingSettings.xml";
                //sTubeSettingsFileName = xmlFolder + stringRes.tubeSettingFileName;
                exePath = Utility.GetExeFolder() + "Biobanking.exe";
                Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
                Utility.WriteExecuteResult(false,"result.txt");
                maxSampleCount = int.Parse(config.AppSettings.Settings[stringRes.maxSampleCount].Value);
                string s = "";
                plasmaMaxCount = int.Parse(config.AppSettings.Settings["PlasmaMaxCount"].Value);
                buffyMaxCount = int.Parse(config.AppSettings.Settings["BuffyMaxCount"].Value);
                gridParent.Children.Add(plateViewer);
                plateViewer.SetSelectedWellID(1);
                plateViewer.OnSelectedWellChanged += PlateViewer_OnSelectedWellChanged;
                if (!File.Exists(sLabwareSettingFileName))
                {
                    SetInfo("LabwareSettings xml does not exist! at : " + sLabwareSettingFileName, Colors.Red);
                    return;
                }

                if (!File.Exists(sPipettingFileName))
                {
                    SetInfo("PipettingSettings xml does not exist! at : " + sPipettingFileName, Colors.Red);
                    return;
                }
                s = File.ReadAllText(sPipettingFileName);
                pipettingSettings = Utility.Deserialize<PipettingSettings>(s);
                s = File.ReadAllText(sLabwareSettingFileName);
                labwareSettings = Utility.Deserialize<LabwareSettings>(s);
                buffyStandalone = pipettingSettings.buffyStandalone;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + exePath);
            }
        }

        private void PlateViewer_OnSelectedWellChanged(string sNewWell)
        {
            txtWellDesc.Text = sNewWell;
            pipettingSettings.startWell = Utility.ParseWellID(sNewWell,labwareSettings);
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            int tmpSampleCount,tmpPlasmaCount;
            try
            {
                bok = int.TryParse(txtSampleCount.Text, out tmpSampleCount);
                if (!bok)
                    SetInfo("样品数量必须为数字！", Colors.Red);
                if (tmpSampleCount <= 0 || tmpSampleCount > maxSampleCount)
                {
                    SetInfo(string.Format("样品数量必须介于1和{0}之间",maxSampleCount),Colors.Red);
                    bok = false;
                }

                if (!bok)
                {
                    txtSampleCount.Text = sampleCount.ToString();
                    return;
                }
                sampleCount = tmpSampleCount;

                bok = int.TryParse(txtPlasmaCount.Text, out tmpPlasmaCount);
                if (!bok)
                {
                    SetInfo("Plasma份数必须为数字！", Colors.Red);
                    return;
                }
                if (tmpPlasmaCount < 0 || tmpPlasmaCount > plasmaMaxCount)
                {
                    SetInfo("Plasma份数必须介于0到"+plasmaMaxCount+"之间",Colors.Red);
                    return;
                }

                int tmpVolume;
                bok = int.TryParse(txtVolume.Text, out tmpVolume);
                if (!bok)
                {
                    SetInfo("Plasma体积为数字！", Colors.Red);
                    return;
                }
                if (tmpVolume != 0)
                {
                    if (tmpVolume <= 50 || tmpPlasmaCount > 2000)
                    {
                        SetInfo("Plasma体积必须介于50ul到2000ul之间", Colors.Red);
                        return;
                    }
                }
                

                int tmpBuffySliceCount;
                bok = int.TryParse(txtbuffySliceCnt.Text, out tmpBuffySliceCount);
                if (!bok)
                {
                    SetInfo("Buffy份数必须为数字！", Colors.Red);
                    return;
                }
                if (tmpBuffySliceCount < 0 || tmpBuffySliceCount > buffyMaxCount)
                {
                    SetInfo("Buffy份数必须介于0到" + buffyMaxCount + "之间", Colors.Red);
                    return;
                }

                int tmpBuffyVolume;
                bok = int.TryParse(txtBuffyVolume.Text, out tmpBuffyVolume);
                if (!bok)
                {
                    SetInfo("Buffy体积必须为数字！", Colors.Red);
                    return;
                }
                if (tmpBuffySliceCount > 0)
                {
                    if (tmpBuffyVolume <= 200 || tmpBuffyVolume > 1000)
                    {
                        SetInfo("Buffy体积数必须介于200ul到1000ul之间", Colors.Red);
                        return;
                    }
                }



                //if (tmpBuffySliceCount > 0 && (bool)rdbRedCell.IsChecked)
                //{
                //    SetInfo("红细胞没有Buffy", Colors.Red);
                //    txtbuffySliceCnt.Text = "0";
                //    return;
                //}
                int regionCnt = (tmpBuffySliceCount + tmpPlasmaCount) * 8;
                int tmpStartWellID = pipettingSettings.startWell;
                while (tmpStartWellID > regionCnt)
                {
                    tmpStartWellID -= regionCnt;
                }

                if(labwareSettings.dstLabwareColumns == 1)
                {
                    Console.WriteLine("不支持到管子");
                    //return;
                }

                if(  (pipettingSettings.startWell - 1) % (tmpPlasmaCount+ tmpBuffySliceCount) !=0)
                {
                    SetInfo("开始孔位不能被plasma加白膜份数整除",Colors.Red);
                    return;
                }



                pipettingSettings.plasmaGreedyVolume = tmpVolume;
                pipettingSettings.dstPlasmaSlice = tmpPlasmaCount;
                pipettingSettings.dstbuffySlice = tmpBuffySliceCount;
                pipettingSettings.buffyVolume = tmpBuffyVolume;
                File.WriteAllText(Utility.GetOutputFolder() + "racksCount.txt", $"{(tmpSampleCount+16-1)/16}");
                File.WriteAllText(Utility.GetOutputFolder() + "usePosID.txt", $"{(bool)chkUsePosID.IsChecked}");

                File.WriteAllText(Utility.GetOutputFolder() + "SampleCount.txt", txtSampleCount.Text);
                //string bloodType = GetBloodType();
                //File.WriteAllText(Utility.GetBloodTypeFile(),bloodType);
                SaveSettings();
               
                int destLabwareNeeded = Utility.CalculateDestLabwareNeededCnt(sampleCount, labwareSettings, pipettingSettings, buffyStandalone);
                File.WriteAllText(Utility.GetOutputFolder() + "dstLabwareNeededCnt.txt", destLabwareNeeded.ToString());
                Utility.WriteExecuteResult(true, "result.txt");
                //if (ConfigurationManager.AppSettings["ShowMessage"]  != null && bool.Parse(ConfigurationManager.AppSettings["ShowMessage"]))
                //    MessageBox.Show(string.Format("Need {0} plates for {1}!", destLabwareNeeded, bloodType));
           }
           catch (Exception ex)
           {
                SetInfo(ex.Message + ex.StackTrace, Colors.Red);
                return;
           }

            Utility.Write2File(Utility.GetOutputFolder() + "totalSlice.txt",
                (pipettingSettings.dstPlasmaSlice + pipettingSettings.dstbuffySlice).ToString());
            this.Close();
        }

        //private string GetBloodType()
        //{
        //    if ((bool)rdbPlasma.IsChecked)
        //        return "Plasma";
        //    else if ((bool)rdbSerum.IsChecked)
        //        return "Serum";
        //    else
        //        return "RedCell";
        //}

  
        private void SetInfo(string p, Color color)
        {
            if (txtInfo == null)
                return;
            txtInfo.Background = new SolidColorBrush(Colors.White);
            txtInfo.Text = p;
            txtInfo.Foreground = new SolidColorBrush(color);
        }



        private void Window_Closed(object sender, EventArgs e)
        {
         
        }

        private void SaveSettings()
        {
            Utility.WriteExecuteResult(sampleCount);
            Utility.SaveSettings(pipettingSettings, sPipettingFileName);
            //Utility.SaveSettings(tubeSettings, sTubeSettingsFileName);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            lblVersion.Content = version;
            try
            {
                txtSampleCount.Text = Utility.ReadFolder(stringRes.SampleCountFile);
            }
            catch (Exception)
            {
                txtSampleCount.Text = "1";
            }
            
            txtPlasmaCount.Text = pipettingSettings.dstPlasmaSlice.ToString();
            txtVolume.Text = pipettingSettings.plasmaGreedyVolume.ToString();
            txtBuffyVolume.Text = pipettingSettings.buffyVolume.ToString();
            txtbuffySliceCnt.Text = pipettingSettings.dstbuffySlice.ToString();

            //InitListView();
            //lstRadius.IsEnabled = false;
            //chkHasBuffy.IsChecked = Convert.ToBoolean(Utility.ReadFolder(stringRes.hasBuffyFile));
            //chkNucleinExtraction.IsChecked= Convert.ToBoolean(Utility.ReadFolder(stringRes.DoNucleinExtractionFile));
        }

        //private void RadioButton_Checked(object sender, RoutedEventArgs e)
        //{
        //     if((bool)rdbSerum.IsChecked)
        //     {
        //         lblBloodSlice.Content = "血清份数：";
        //         lblBloodVolume.Content = "血清体积(ul)：";
        //         txtbuffySliceCnt.Text = "0";
        //         txtbuffySliceCnt.IsEnabled = false;
        //         txtBuffyVolume.IsEnabled = false;
        //     }
        //     else if ((bool)rdbPlasma.IsChecked)
        //     {
        //         lblBloodSlice.Content = "血浆份数：";
        //         lblBloodVolume.Content = "血浆体积(ul)：";
        //         txtbuffySliceCnt.Text = "1";
        //         txtbuffySliceCnt.IsEnabled = true;
        //         txtBuffyVolume.IsEnabled = true;
        //     }
            
        //}


        //private void InitListView()
        //{

        //    DataTable tbl = new DataTable("template");
        //    tbl.Columns.Add("radius", typeof(string));
        //    tbl.Columns.Add("distance", typeof(string));
        //    tbl.Columns.Add("startPos", typeof(string));
           
        //    for (int i = 0; i < tubeSettings.Settings.Count; i++)
        //    {
        //        object[] objs = new object[3];
        //        objs[0] = tubeSettings.Settings[i].r_mm;
        //        objs[1] = tubeSettings.Settings[i].msdZDistance;
        //        objs[2] = tubeSettings.Settings[i].msdStartPositionAboveBuffy;
        //        tbl.Rows.Add(objs);
        //    }
        //    lstSampleSettings.ItemsSource = tbl.DefaultView;
        //    lstSampleSettings.SelectedIndex = tubeSettings.selectIndex;
        //}

    }
}
