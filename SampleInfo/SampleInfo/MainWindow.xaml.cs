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

        LabwareSettings labwareSettings = new LabwareSettings();
        PipettingSettings pipettingSettings = new PipettingSettings();
        //TubeSettings tubeSettings = new TubeSettings();
        int maxSampleCount = 0;
        bool buffyStandalone = false;
        
        public MainWindow()
        {
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + exePath);
            }
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

                string desc = "Plasma";
                bok = int.TryParse(txtPlasmaCount.Text, out tmpPlasmaCount);
                if (!bok)
                {
                    SetInfo(desc + "份数必须为数字！", Colors.Red);
                    return;
                }
                if (tmpPlasmaCount <= 0 || tmpPlasmaCount > plasmaMaxCount)
                {
                    SetInfo(desc + "份数必须介于1到"+plasmaMaxCount+"之间",Colors.Red);
                    return;
                }

                int tmpVolume;
                bok = int.TryParse(txtVolume.Text, out tmpVolume);
                if (!bok)
                {
                    SetInfo(desc + "体积为数字！", Colors.Red);
                    return;
                }
                
                if (tmpVolume != 0)
                {
                    if (tmpVolume < 50 || tmpPlasmaCount > 2000)
                    {
                        SetInfo(desc +"体积必须介于50ul到2000ul之间", Colors.Red);
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
                    if (tmpBuffyVolume < 200 || tmpBuffyVolume > 1000)
                    {
                        SetInfo("Buffy体积数必须介于200ul到1000ul之间", Colors.Red);
                        return;
                    }
                }

                string startWellID = txtStartWellID.Text;
                int dstPlateStartWellID = ParseWellID(startWellID);
                if (dstPlateStartWellID <= 0 || dstPlateStartWellID > 96)
                {
                    SetInfo(string.Format("Well ID:{0} is invalid.", dstPlateStartWellID), Colors.Red);
                    return;
                }
                    

                pipettingSettings.plasmaGreedyVolume = tmpVolume;
                pipettingSettings.dstPlasmaSlice = tmpPlasmaCount;
                pipettingSettings.dstbuffySlice = tmpBuffySliceCount;
                pipettingSettings.buffyVolume = tmpBuffyVolume;
                

                File.WriteAllText(Utility.GetOutputFolder() + "SampleCount.txt", txtSampleCount.Text);
                SaveSettings();
               
                int destLabwareNeeded = Utility.CalculateDestLabwareNeededCnt(sampleCount, labwareSettings, pipettingSettings, buffyStandalone);
                File.WriteAllText(Utility.GetOutputFolder() + "dstLabwareNeededCnt.txt", destLabwareNeeded.ToString());

                File.WriteAllText(Utility.GetOutputFolder() + "buffySliceCnt.txt", pipettingSettings.dstbuffySlice.ToString());

                File.WriteAllText(Utility.GetOutputFolder() + "startWellID.txt", txtStartWellID.Text);

                Utility.WriteExecuteResult(true, "result.txt");
             
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

        internal static int GetWellID(int colIndex, int rowIndex)
        {
            int _row = 8;
            return colIndex * _row + rowIndex + 1;
        }
        public static int ParseWellID(string sWellID)
        {
            sWellID = sWellID.Trim();
            if (sWellID.Length > 3)
                throw new Exception("WellID length must <=3!");

            if (sWellID.All(x => Char.IsDigit(x)))
            {
                return int.Parse(sWellID);
            }
            sWellID = sWellID.ToUpper();
            char first = sWellID.First();
            if (char.IsLetter(first))
            {
                string remain = sWellID.Substring(1);
                int rowIndex = (int)(first - 'A');
                int colIndex = int.Parse(remain) - 1;
                return GetWellID(colIndex, rowIndex);
            }
            else
                throw new Exception("Invalid WellID, must be digital or string likes A01!");
        }
  
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
            lblVersion.Content = 0.15;
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
            txtStartWellID.Text = "A1";
        }

        

    }
}
