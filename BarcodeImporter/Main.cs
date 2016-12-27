using BarcodeImporter.Properties;
using Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BarcodeImporter
{
    public partial class Main : Form
    {

        List<PatientInfo> patientInfos = new List<PatientInfo>();
        public Main()
        {
            InitializeComponent();
            WriteResult(false);
            version.Text = strings.version;
        }


        private void InitDataGridView(int totalCnt)
        {
            dataGridView.AllowUserToAddRows = false;
            dataGridView.EnableHeadersVisualStyles = false;
            dataGridView.Columns.Clear();
            List<string> strs = new List<string>();

            int gridCnt = (totalCnt + 15) / 16;

            int srcStartGrid = 1;
            for (int i = 0; i < gridCnt; i++)
            {
                DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
                column.HeaderText = string.Format("条{0}", srcStartGrid + i);
                column.HeaderCell.Style.BackColor = Color.LightSeaGreen;
                dataGridView.Columns.Add(column);
                dataGridView.Columns[i].SortMode = DataGridViewColumnSortMode.Programmatic;
            }
            dataGridView.RowHeadersWidth = 80;
            for (int i = 0; i < 16; i++)
            {
                dataGridView.Rows.Add(strs.ToArray());
                dataGridView.Rows[i].HeaderCell.Value = string.Format("行{0}", i + 1);
            }
        }
        private void btnAbort_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            WriteResult(true);
            WritePatientInfos();
            this.Close();
        }
        private void WritePatientInfos()
        {
            string exePath = Utility.GetExeFolder() + "Biobanking.exe";
            Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
            string file = config.AppSettings.Settings["SrcBarcodeFile"].Value;
            List<string> strs = new List<string>();
            patientInfos.ForEach(x=>strs.Add(Format(x)));
            File.WriteAllLines(file, strs);
        }

        private string Format(PatientInfo x)
        {
            return string.Format("{0},{1},{2}", x.id, x.name, x.seqNo);
        }
        private void WriteResult(bool bok)
        {
            string folder = Utility.GetOutputFolder();
            string resultFile = folder + "barcodeResult.txt";
            File.WriteAllText(resultFile, bok.ToString());
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            try
            {
                ImportImpl();
            }
            catch(Exception ex)
            {
                btnOk.Enabled = false;
                AddErrorInfo(ex.Message + ex.StackTrace);
            }
            
           
        }

        private void ImportImpl()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "请选择文件";
            dialog.RestoreDirectory = true;
            dialog.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            if (dialog.ShowDialog() != DialogResult.OK)
                return;
            string selectfile = dialog.FileName;
            List<string> strs = File.ReadAllLines(selectfile,Encoding.Default).ToList();
            patientInfos.Clear();
            strs = strs.Skip(1).ToList();
            foreach (string s in strs)
            {
                if (s == "")
                    break;
                char chSplit = ',';
                if (!s.Contains(','))
                {
                    if (s.Contains('\t'))
                        chSplit = '\t';
                }

                string[] tempStrs = s.Split(chSplit);
                if (tempStrs[0] == "")
                    break;
                patientInfos.Add(new PatientInfo(tempStrs[4], tempStrs[1], tempStrs[0]));
            }

            InitDataGridView(patientInfos.Count);
            int maxAllowed = int.Parse(ConfigurationManager.AppSettings["maxSampleCnt"]);
            if(patientInfos.Count > maxAllowed)
                throw new Exception(string.Format("一共有{0}个样品，超过最大允许数{1}",patientInfos.Count,maxAllowed));

            for (int i = 0; i < patientInfos.Count; i++)
            {
                int tubeID = i + 1;
                var gridID = (tubeID + 15) / 16;
                int rowIndex = tubeID - (gridID - 1) * 16 - 1;
                UpdateGridCell(gridID, rowIndex, patientInfos[i]);
            }
            lblTotalCnt.Text = patientInfos.Count.ToString();
            string sampleCntFile = (Utility.GetOutputFolder() + "SampleCount.txt");
            if(File.Exists(sampleCntFile))
            {
                int cnt = int.Parse(File.ReadAllText(sampleCntFile));
                if(cnt != int.Parse(lblTotalCnt.Text))
                {
                    throw new Exception(string.Format("条码数不等于设定样本数：{0}",cnt));
                }
            }
            btnOk.Enabled = true;
            AddInfo("导入成功，请校验！");
        }


        private void AddErrorInfo(string txt)
        {
            richTextInfo.SelectionColor = Color.Red;
            richTextInfo.AppendText(txt + "\r\n");
        }

        private void AddInfo(string txt)
        {
            richTextInfo.SelectionColor = Color.Green;
            richTextInfo.AppendText(txt + "\r\n");
        }

        private void UpdateGridCell(int gridID, int rowIndex,PatientInfo patientInfo)
        {
            dataGridView.Rows[rowIndex].Cells[gridID - 1].Value = patientInfo.seqNo + "_" + patientInfo.name;
        }
    }

 
}
