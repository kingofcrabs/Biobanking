using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AutomateBarcode.TissueManagement;
using System.Configuration;
using System.IO;


namespace AutomateBarcode
{
    public partial class PrintForm : Form
    {
        public bool bok = false;
        int neededBarcodes = 0;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public PrintForm()
        {
            InitializeComponent();
            this.Load += new EventHandler(PrintForm_Load);
        }

        void PrintForm_Load(object sender, EventArgs e)
        {
            neededBarcodes = Global.Instance.smpPlasmaSlices.Sum(x => x.Value) +
                Global.Instance.buffySlice * Global.Instance.smpPlasmaSlices.Count;
            txtTotal.Text = neededBarcodes.ToString();
            if (neededBarcodes != 0)
            {
                btnPrintBarcodes.Enabled = true;
            }
        }

        private void SetInfo(string s, Color color)
        {
            txtInfo.Text = s;
            txtInfo.ForeColor = color;
        }

        private void ReadExpectedBarcodes()
        {
            string sExpectedBarcodesFile = ConfigurationManager.AppSettings["testExpectedBarcodesFile"];
            string[] expectedBarcodes = File.ReadAllLines(sExpectedBarcodesFile);
            Global.Instance.expectedBarcodes = expectedBarcodes.ToList();
        }

        private void btnPrintBarcodes_Click(object sender, EventArgs e)
        {
            bok = true;
            log.Info("Print barcode");
//#if DEBUG
//            ReadExpectedBarcodes();
//            btnNext.Enabled = true;
//            return;
//#endif
            
            try
            {
                PrintResult res = Global.Instance.server.PrintBarcodes(neededBarcodes);

                bok = (bool)res.bok;
                btnNext.Enabled = bok;
                if (!bok)
                {
                    string errMsg = res.errDescription;
                    log.Error(errMsg);
                    SetInfo(errMsg, Color.Red);
                    return;
                }
                
                Global.Instance.expectedBarcodes = res.barcodes.ToList();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                SetInfo(ex.Message, Color.Red);
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
