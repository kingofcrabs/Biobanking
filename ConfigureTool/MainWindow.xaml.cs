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
        Dictionary<string, MType> key_Type = new Dictionary<string, MType>();
        Dictionary<string, string> currentSettings = new Dictionary<string, string>();
        DataTable tbl = new DataTable("Customers");
        DescriptionHelper descriptionHelper = new DescriptionHelper();
        string currentKey = "";
        public MainWindow()
        {
            InitializeComponent();
            listView.SelectionChanged += ListView_SelectionChanged;
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                tbl.Columns.Add("Name", typeof(string));
                tbl.Columns.Add("Value", typeof(string));
                ChangeSetting();
            }
            catch(Exception ex)
            {
                SetInfo(ex.Message, true);
            }
            
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView.SelectedIndex == -1)
                return;
            var dt = (DataTable)listView.DataContext;
            txtCurrentVal.Text = (string)dt.Rows[listView.SelectedIndex][1];
            currentKey = (string)dt.Rows[listView.SelectedIndex][0];
            if (descriptionHelper[currentKey] == null)
                throw new Exception("无法找到对应的说明！");
            SetInfo(descriptionHelper[currentKey]);
        }

     

        private void SetInfo(string message, bool isError = false)
        {
            txtInfo.Text = message;
            txtInfo.Foreground = isError ? Brushes.Red : Brushes.Black;
        }

        private void ChangeSetting()
        {
            tbl.Rows.Clear();
            bool configSettings = (bool)rdbConfigSettings.IsChecked;
            bool labwareSettings = (bool)rdbLabwareSettings.IsChecked;
            bool pipettingSettings = (bool)rdbPipettingSettings.IsChecked;
            currentSettings = settings.Load(configSettings, labwareSettings, pipettingSettings);
            
            key_Type = TypeConstrain.ExtractTypeInfo(currentSettings);
            this.listView.DataContext = CreateDataTable(currentSettings);
        }

     
        DataTable CreateDataTable(Dictionary<string,string> dict)
        {
           
            ;
            foreach(var pair in dict)
            {
                tbl.Rows.Add(pair.Key, pair.Value);
            }
            return tbl;
        }
        private void rdb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChangeSetting();
            }
            catch (Exception ex)
            {
                SetInfo(ex.Message, true);
            }
        }

        private void btnModify_Click(object sender, RoutedEventArgs e)
        {

            if (listView.SelectedIndex == -1)
                return;
            if (currentKey == "")
                return;
            if(!TypeConstrain.IsExpectedType(txtCurrentVal.Text, key_Type[currentKey]))
            {
                SetInfo(string.Format("类型不对，期望的类型是{0}：", key_Type[currentKey]), true);
                return;
            }
            currentSettings[currentKey] = txtCurrentVal.Text;
            tbl.Rows[listView.SelectedIndex][1] = txtCurrentVal.Text;

            bool isConfigSettings = (bool)rdbConfigSettings.IsChecked;
            bool isLabwareSettings = (bool)rdbLabwareSettings.IsChecked;
            bool isPipettingSettings = (bool)rdbPipettingSettings.IsChecked;
            settings.Save(isConfigSettings, isLabwareSettings, isPipettingSettings, currentSettings);
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

      
    }
}
