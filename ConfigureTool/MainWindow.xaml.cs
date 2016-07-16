using System;
using System.Collections.Generic;
using System.Data;
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

namespace ConfigureTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Setting settings = new Setting();
        Dictionary<string, Type> key_Type = new Dictionary<string, Type>();
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            throw new NotImplementedException();
        }

        private void rdb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChangeSetting();
            }
            catch(Exception ex)
            {
                SetInfo(ex.Message,true);
            }
        }

        private void btnModify_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SetInfo(string message, bool isError = false)
        {
            txtInfo.Text = message;
            txtInfo.Foreground = isError ? Brushes.Red : Brushes.Black;
        }

        private void ChangeSetting()
        {
            bool configSettings = (bool)rdbConfigSettings.IsChecked;
            bool labwareSettings = (bool)rdbLabwareSettings.IsChecked;
            bool pipettingSettings = (bool)rdbPipettingSettings.IsChecked;
            var dict = settings.Load(configSettings, labwareSettings, pipettingSettings);
            key_Type = TypeConstrain.ExtractTypeInfo(dict);
            this.listView.DataContext = CreateDataTable(dict);
        }

     
        DataTable CreateDataTable(Dictionary<string,string> dict)
        {
            DataTable tbl = new DataTable("Customers");

            tbl.Columns.Add("Name", typeof(string));
            tbl.Columns.Add("Value", typeof(string));
            foreach(var pair in dict)
            {
                tbl.Rows.Add(pair.Key, pair.Value);
            }
            return tbl;
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

      
    }
}
