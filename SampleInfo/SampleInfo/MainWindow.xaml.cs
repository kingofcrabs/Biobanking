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


namespace SampleInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int sampleCount = 16, plasmaMaxCount,buffyMaxCount;
        bool bok = false;
        int buffySliceCnt;
        int buffyVolume;

        string xmlFolder = ConfigurationManager.AppSettings[stringRes.xmlFolder];
        string sLabwareSettingFileName;
        string sPipettingFileName;

        LabwareSettings labwareSettings = new LabwareSettings();
        PipettingSettings pipettingSettings = new PipettingSettings();
        int maxSampleCount = 0;
        public MainWindow()
        {
            InitializeComponent();
            sLabwareSettingFileName = xmlFolder + "\\labwareSettings.xml";
            sPipettingFileName = xmlFolder + "\\pipettingSettings.xml";
            maxSampleCount = int.Parse(ConfigurationManager.AppSettings[stringRes.maxSampleCount]);
            string s = "";
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

            using (StreamReader sr = new StreamReader(sLabwareSettingFileName))
            {
                s = sr.ReadToEnd();
            }
            //labwareSettings = Utility.Deserialize<LabwareSettings>(s);
            plasmaMaxCount = int.Parse(ConfigurationManager.AppSettings["PlasmaMaxCount"]);
            buffyMaxCount = int.Parse(ConfigurationManager.AppSettings["BuffyMaxCount"]);
            using (StreamReader sr = new StreamReader(sPipettingFileName))
            {
                s = sr.ReadToEnd();
            }
            
            try
            {
                pipettingSettings = Utility.Deserialize<PipettingSettings>(s);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                if (tmpBuffyVolume <= 200 || tmpBuffyVolume > 1000)
                {
                    SetInfo("Buffy体积数必须介于200ul到1000ul之间", Colors.Red);
                    return;
                }
                pipettingSettings.plasmaGreedyVolume = tmpVolume;
                pipettingSettings.dstPlasmaSlice = tmpPlasmaCount;
                pipettingSettings.dstbuffySlice = tmpBuffySliceCount;
                pipettingSettings.buffyVolume = tmpBuffyVolume;
                string sRadius = lstRadius.SelectedItem.ToString();
                pipettingSettings.r_mm = double.Parse(sRadius);
         
               SaveSettings();
           }
           catch (Exception ex)
           {
               SetInfo(ex.Message, Colors.Red);
               return;
           }
           this.Close();
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

           
            //string s = Utility.Serialize(pipettingSettings);

            //using (StreamWriter sw = new StreamWriter(sPipettingFileName))
            //{
            //    sw.Write(s);
            //}
        }

        private void SaveSettings()
        {
            //Utility.WriteExecuteResult(HasBuffy, stringRes.hasBuffyFile);
            //Utility.WriteExecuteResult(DoNucleinExtraction, stringRes.DoNucleinExtractionFile);
            Utility.WriteExecuteResult(sampleCount);
            Utility.SaveSettings<PipettingSettings>(pipettingSettings, sPipettingFileName);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lblVersion.Content = stringRes.Version;
            try
            {
                txtSampleCount.Text = Utility.ReadFolder(stringRes.SampleCountFile);
            }
            catch (Exception ex)
            {
                txtSampleCount.Text = "1";
            }
            
            txtPlasmaCount.Text = pipettingSettings.dstPlasmaSlice.ToString();
            txtVolume.Text = pipettingSettings.plasmaGreedyVolume.ToString();
            txtBuffyVolume.Text = pipettingSettings.buffyVolume.ToString();
            txtbuffySliceCnt.Text = pipettingSettings.dstbuffySlice.ToString();
            string radiusFile = Utility.GetExeFolder() + "radius.txt";
            List<string> radiusList = File.ReadLines(radiusFile).ToList();
            foreach (string s in radiusList)
                lstRadius.Items.Add(s);
            lstRadius.SelectedIndex = 0;
            double r = pipettingSettings.r_mm;
            string sRadiusInPipettingSetting = r.ToString();
            for (int i = 0; i < lstRadius.Items.Count; i++)
            {
                if (lstRadius.Items[i].ToString() == sRadiusInPipettingSetting)
                {
                    lstRadius.SelectedIndex = i;
                    break;
                }
            }
            lstRadius.IsEnabled = false;

            //chkHasBuffy.IsChecked = Convert.ToBoolean(Utility.ReadFolder(stringRes.hasBuffyFile));
            //chkNucleinExtraction.IsChecked= Convert.ToBoolean(Utility.ReadFolder(stringRes.DoNucleinExtractionFile));
        }

    }
}
