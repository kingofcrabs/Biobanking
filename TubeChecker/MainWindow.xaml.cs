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
        List<TrackInfo> trackInfos;
        
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
#if DEBUG
#else
            trackInfos = ReadTrackInfos();
            var plateBarcodes = trackInfos.Select(x => x.plateBarcode).Distinct().ToList();
            lstPlateBarcodes.ItemsSource = plateBarcodes;
#endif

        }

        private List<TrackInfo> ReadTrackInfos()
        {
            string trackInfoFile = Utility.GetOutputFolder() + "trackinfo.xml";
            string xml = File.ReadAllText(trackInfoFile);
            var trackInfos = Utility.Deserialize<List<TrackInfo>>(xml);
            return trackInfos;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            plateViewer = new PlateViewer(new Size(600, 400), new Size(30, 40));
            canvas.Children.Add(plateViewer);
            lstPlateBarcodes.SelectedIndex = 0;

#if DEBUG
            List<int> usedWellIDs = new List<int>()
            {
                1,2,3,4,9,10,11,12,17,18,19,20,25,26,27,28,33,34,35,36,41,42,43,44
            };
            List<int> allWellIDs = new List<int>();
            for (int i = 0; i < 96; i++)
                allWellIDs.Add(i + 1);
            var notUsedWells = allWellIDs.Except(usedWellIDs).ToList();
            plateViewer.SetNotUsed(notUsedWells);
#endif
            //List<int> wellIDs = new List<int>()
            //{
            //    1,2,3,5,8,12,20,89
            //};
            //plateViewer.SetEmpty(wellIDs);
        }



        private void lstPlateBarcodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstPlateBarcodes.SelectedIndex == -1)
               return;
            var thisPlateInfos = trackInfos.Where(x => x.plateBarcode == lstPlateBarcodes.SelectedItem.ToString()).ToList();
            List<int> usedWellIDs = new List<int>();
            foreach(var info in thisPlateInfos)
            {
                usedWellIDs.Add(ParseID(info.position));
            }
            List<int> notUsedWellIDs = new List<int>();
            for(int i = 0; i< 96; i++)
            {
                int id = i + 1;
                if(!usedWellIDs.Contains(id))
                    notUsedWellIDs.Add(id);
            }

            plateViewer.SetNotUsed(notUsedWellIDs);
        }

        private int ParseID(string position)
        {
            int rowIndex = position[0] - 'A';
            int colID = int.Parse(position.Substring(1));
            return (colID-1) * 8 + rowIndex + 1;
        }
    }
}
