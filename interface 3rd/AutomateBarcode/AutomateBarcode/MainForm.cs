using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.IO;

namespace AutomateBarcode
{
    public partial class MainForm : Form
    {
        PrintForm printForm = new PrintForm();
        InputBarcodeForm inputBarcodeForm = new InputBarcodeForm();
        public MainForm()
        {
            InitializeComponent();
            this.Load += new EventHandler(MainForm_Load);
            
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                ReadResultFile();
                ReadOrgBarcodes();
#if DEBUG
                //ReadExpectedBarcodes();
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取结果文件失败,原因：" + ex.Message + ex.StackTrace);
                this.Close();
                return;
            }
            
            printForm.TopLevel = false;
            inputBarcodeForm.TopLevel = false;
            printForm.FormClosed += new FormClosedEventHandler(printForm_FormClosed);
            inputBarcodeForm.FormClosed += new FormClosedEventHandler(inputBarcodeForm_FormClosed);
            this.Controls.Add(printForm);
            printForm.Show();
        }

        void inputBarcodeForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Close();
        }

        

        private void ReadOrgBarcodes()
        {
            string sOrgBarcodesFile = ConfigurationManager.AppSettings["orgBarcodesFile"];
            string[] orgBarcodes = File.ReadAllLines(sOrgBarcodesFile);
            Global.Instance.srcBarcodes = orgBarcodes.ToList();
        }
        private void ReadResultFile()
        {
            string sResultFile = ConfigurationManager.AppSettings["resultFile"];
            string sContent = File.ReadAllText(sResultFile,Encoding.Default);
            RunResult runResult = Utility.Deserialize<RunResult>(sContent);
            Global.Instance.SetResult(runResult);
        }
        private int GetRealPlasmaSlice(string s)
        {
            string[] strs = s.Split(';');
            return int.Parse(strs[1]);
        }

        private void GetCommonSetting(string s)
        {
            string[] strs = s.Split(';');
            Global.Instance.plasmaSlice = int.Parse(strs[2]);
            Global.Instance.buffySlice = int.Parse(strs[3]);
        }
        void printForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!printForm.bok)
                this.Close();

            this.Controls.Remove(printForm);
            this.Controls.Add(inputBarcodeForm);
            inputBarcodeForm.Show();
        }

        

    }

    struct CellPos
    {
        public int rowIndex;
        public int colIndex;
        public CellPos(int r, int c)
        {
            rowIndex = r;
            colIndex = c;
        }
    }
}
