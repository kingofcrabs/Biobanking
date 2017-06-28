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
        //int setSampleCnt = 0;
        public Main()
        {
            InitializeComponent();
            WriteResult(false);
            version.Text = strings.version;
            //string sampleCntFile = (Utility.GetOutputFolder() + "SampleCount.txt");
            //if(File.Exists(sampleCntFile))
            //{
            //    setSampleCnt = int.Parse(File.ReadAllText(sampleCntFile));
            //    lblSetCnt.Text = setSampleCnt.ToString();
            //}
            //else
            //{
            //    AddErrorInfo("未找到病人数量！");
            //    btnImport.Enabled = false;
            //}
            
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
            //patientInfos = patientInfos.Take(setSampleCnt).ToList();
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
            string resultFile = folder + "result.txt";
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
            string header = strs.First();
            strs = strs.Skip(1).ToList();
            Dictionary<string, int> info_ColumnIndex = ParseInfoColumn(header);
            int idIndex = info_ColumnIndex["ID"];
            int seqNoIndex = info_ColumnIndex["seqNo"];
            int nameIndex = info_ColumnIndex["name"];
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

                if (tempStrs[seqNoIndex] == "")
                {
                    throw new Exception(string.Format("No seqNo found for in string {0}!", s));
                }
                if (tempStrs[idIndex] == "")
                    throw new Exception(string.Format("No patient ID found for sequence NO: {0}!",tempStrs[0]));
                patientInfos.Add(new PatientInfo(tempStrs[idIndex], tempStrs[nameIndex], tempStrs[seqNoIndex]));
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
            //string sampleCntFile = (Utility.GetOutputFolder() + "SampleCount.txt");
            //if(File.Exists(sampleCntFile))
            //{
            //    setSampleCnt = int.Parse(File.ReadAllText(sampleCntFile));
            //    if (patientInfos.Count < setSampleCnt)
            //    {
            //        throw new Exception(string.Format("条码数小于设定样本数：{0}", setSampleCnt));
            //    }
            //}
            btnOk.Enabled = true;
            AddInfo("导入成功，请校验！");
        }

        private Dictionary<string, int> ParseInfoColumn(string header)
        {
            Dictionary<string, int> column_Index = new Dictionary<string, int>();
            string[] strs = header.Split('\t');
            Dictionary<string, string> Chinese_English = new Dictionary<string, string>() { };
            Chinese_English.Add("检验流水号","seqNo");
            Chinese_English.Add("姓名","name");
            Chinese_English.Add("病人ID","ID");
            for(int i = 0 ; i< strs.Length; i++)
            {
                string key = strs[i].Trim();
                if(Chinese_English.ContainsKey(key))
                {
                    string english = Chinese_English[key];
                    column_Index.Add(english, i);
                }
            }
            return column_Index;
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
