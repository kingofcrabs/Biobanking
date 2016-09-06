using Settings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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

namespace TubeChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PlateViewer plateViewer;
        List<string> barcodeFilePaths;
        List<string> usedBarcodes;
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            barcodeFilePaths = GetBarcodeFiles();
            usedBarcodes = GetUsedBarcodes();
            lstFiles.ItemsSource = barcodeFilePaths;
           
        }

        private List<string> GetUsedBarcodes()
        {
            string trackInfoFile = Utility.GetOutputFolder() + "trackinfo.xml";
            string xml = File.ReadAllText(trackInfoFile);
            var trackInfos = Utility.Deserialize<List<TrackInfo>>(xml);
            var barcodes = new List<string>();
            trackInfos.ForEach(x => barcodes.Add(x.dstBarcode));
            return barcodes;
        }

        private List<string> GetBarcodeFiles()
        {
            string exePath = Utility.GetExeFolder() + "Biobanking.exe";
            Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
            var folder = config.AppSettings.Settings["DstBarcodeFolder"].Value;
            DirectoryInfo di = new DirectoryInfo(folder);
            return di.EnumerateFiles("*.csv").Select(x=>x.FullName).ToList();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            plateViewer = new PlateViewer(new Size(600, 400), new Size(30, 40));
            canvas.Children.Add(plateViewer);
            //List<int> wellIDs = new List<int>()
            //{
            //    1,2,3,5,8,12,20,89
            //};
            //plateViewer.SetEmpty(wellIDs);
        }

        private void lstFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstFiles.SelectedIndex == -1)
                return;
            var strs = File.ReadAllLines(lstFiles.SelectedItem.ToString()).Skip(1).ToList();
            Dictionary<int, string> well_Barcode = new Dictionary<int, string>();
            List<int> notUsedWells = new List<int>();
            int wellID = 1;
            foreach (var s in strs)
            {
                var subStrs = s.Split(',');
                var barcode = subStrs[GlobalVars.Instance.FileStruct.dstBarcodeIndex].Trim();
                if (!usedBarcodes.Contains(barcode))
                    notUsedWells.Add(wellID);
                wellID++;
            }
            plateViewer.SetNotUsed(notUsedWells);
        }
    }
}
