using Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarcodeReader
{
    public partial class MainForm : Form
    {
        SerialPort serialPort;
        bool programModify = false;
        int totalSampleCnt = 0;
        int tubeID = 1;
        bool autoNext = false;
        List<string> simulateBarcodes = new List<string>();
        public MainForm()
        {
            InitializeComponent();
            WriteResult(false);
            this.FormClosed += MainForm_FormClosed;
            this.Load += MainForm_Load;
            lblVersion.Text = string.Format("版本号:{0}", strings.version);
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            bool isSimulation = bool.Parse(ConfigurationManager.AppSettings["Simulation"]);
            if(isSimulation)
            {
                for(int i = 0;i< 32;i++)
                {
                    simulateBarcodes.Add(string.Format("{0}", i+1));
                }
                simulateBarcodes[4] = "";
                simulateBarcodes[5] = "";
                simulateBarcodes[10] = "2";
            }
            else
            {
                string sPortNum = ConfigurationManager.AppSettings["PortNum"];
                serialPort = new SerialPort("COM" + sPortNum);
                serialPort.Open();
                serialPort.DataReceived += serialPort_DataReceived;
                totalSampleCnt = int.Parse(Utility.ReadFolder(stringRes.SampleCountFile));
                txtSampleCnt.Text = totalSampleCnt.ToString();
                txtSampleCnt.Enabled = false;
                btnSet.Enabled = false;
                InitDataGridView();
            }
            dataGridView.CellValueChanged += DataGridView_CellValueChanged;
            autoNext = bool.Parse(ConfigurationManager.AppSettings["AutoNext"]);
        }

        private void DataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (programModify)
                return;
            if (e.ColumnIndex == -1)
                return;
            if (e.ColumnIndex >= dataGridView.Rows[0].Cells.Count)
                return;
            int grid = e.ColumnIndex + 1;
            if(e.ColumnIndex *16 + e.RowIndex+1 > totalSampleCnt)
                return;

            var cell = dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
            string newBarcode = cell.Value.ToString();

            bool bok = false;
            string errMsg = "";
            Dictionary<int, List<string>> eachGridBarcodes = GetEachGridBarcodes();
            bok = IsValidBarcode(newBarcode, eachGridBarcodes, grid, e.RowIndex, ref errMsg);
            if (bok)
            {
                btnConfirm.Enabled = true;
                cell.Style.BackColor = Color.Orange;
                //cell.ReadOnly = true;
                string hint = string.Format("Grid:{0} 第{1}个样品管的条码得到修复。"
                    , grid, e.RowIndex + 1);
            }
            else
            {
                AddErrorInfo("修复失败，原因是： " + errMsg);
            }
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                CheckBarcodes();
            }
            catch (Exception ex)
            {
                AddErrorInfo(ex.Message);
                return;
            }
            AddInfo("检查通过！");
            //var eachGridBarcodes = GetEachGridBarcodes();
            WriteResult(true);
            WriteBarcodes();
            ProcessHelper.CloseWaiter();
            this.Close();
            //btnConfirm.Enabled = false;
        }

        private void WriteBarcodes()
        {
            string exePath = Utility.GetExeFolder() + "Biobanking.exe";
            Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
            string srcBarcodeFile = config.AppSettings.Settings["SrcBarcodeFile"].Value;
            var eachGridBarcodes = GetEachGridBarcodes();
            List<string> barcodes = new List<string>(); 

            foreach(var pair in eachGridBarcodes)
            {
                //pair.Value.ForEach()
                barcodes.AddRange(pair.Value);
            }
            File.WriteAllLines(srcBarcodeFile, barcodes);
        }

        private void CheckBarcodes()
        {
            var eachGridBarcodes = GetEachGridBarcodes();
            string errMsg = "";
            for(int r = 0; r< dataGridView.Rows.Count; r++)
            {
                for(int c = 0; c < dataGridView.Rows[r].Cells.Count;c++)
                {
                    if (c * 16 + r + 1 > totalSampleCnt)
                        continue;
                    var cellVal = dataGridView[c, r].Value;
                    string barcode = cellVal == null? "": cellVal.ToString();
                    bool bok = IsValidBarcode(barcode, eachGridBarcodes, c + 1, r, ref errMsg);
                    if (!bok)
                        throw new Exception(errMsg);
                }
            }
            int totalBarcodeCnt = 0;
            foreach(var pair in eachGridBarcodes)
            {
                totalBarcodeCnt += pair.Value.Count;
            }
            if(totalBarcodeCnt < totalSampleCnt)
            {
                throw new Exception(string.Format("只有{0}个样品被赋予条码!", totalBarcodeCnt));
            }
        }

        private void btnSimulateBarcode_Click(object sender, EventArgs e)
        {
            tubeID++;
           
            OnNewBarcode(simulateBarcodes[tubeID - 1]);
        }

        private void OnNewBarcode(string newBarcode)
        {
                txtLog.AppendText(newBarcode + "\r\n");
                var gridID = (tubeID + 15) / 16;
                int rowIndex = tubeID - (gridID - 1) * 16 - 1;
                programModify = true;
                UpdateGridCell(gridID, rowIndex, newBarcode);
                programModify = false;
        }
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string newBarcode = serialPort.ReadLine();
            this.Invoke(new Action(delegate ()
            {
                if(dataGridView.Rows.Count == 0)
                {
                    AddErrorInfo("请先设置样本数！");
                    return;
                }
                OnNewBarcode(newBarcode);
            }));
        }

        private void UpdateGridCell(int gridID, int rowIndex, string barcode)
        {
            dataGridView.Rows[rowIndex].Cells[gridID-1].Value = barcode;
            string errMsg = "";
            var eachGridBarcodes = GetEachGridBarcodes();
            bool bValid = IsValidBarcode(barcode, eachGridBarcodes, gridID, rowIndex, ref errMsg);
            dataGridView.Rows[rowIndex].Cells[gridID-1].Style.BackColor = bValid ? Color.LightGreen : Color.Red;
            if(!bValid)
            {
                AddErrorInfo(errMsg);
                return;
            }
            else if(autoNext)
            {
                tubeID++;
            }
        }
     
        private Dictionary<int, List<string>> GetEachGridBarcodes()
        {
            Dictionary<int, List<string>> eachGridBarcodes = new Dictionary<int, List<string>>();
            int columnCnt = dataGridView.Rows[0].Cells.Count;
            for (int c = 0; c < dataGridView.Rows[0].Cells.Count; c++)
            {
                eachGridBarcodes.Add(c + 1, new List<string>());
                for (int r = 0; r < dataGridView.Rows.Count; r++)
                {
                    int curTubeID = c * 16 + r + 1;
                    if (curTubeID > totalSampleCnt)
                        break;
                    var cellVal = /**/dataGridView.Rows[r].Cells[c].Value;
                    eachGridBarcodes[c + 1].Add(cellVal == null ? "": cellVal.ToString());
                }
            }
            return eachGridBarcodes;
        }

        private bool IsValidBarcode(string currentBarcode,Dictionary<int,List<string>>eachGridBarcodes, int gridID, int rowIndex, ref string errMsg)
        {
            if(currentBarcode == "")
            {
                errMsg = string.Format("Grid{0}中第{1}个条码为空！", gridID, rowIndex + 1);
                return false;
            }
            foreach (var pair in eachGridBarcodes)
            {
                int tmpGrid = pair.Key;
                var tmpBarcodes = pair.Value;

                
                for( int r = 0; r< tmpBarcodes.Count; r++)
                {
                    if (tmpGrid == gridID && rowIndex == r) //dont compare to itself
                        continue;
                    if (tmpBarcodes[r] == currentBarcode)
                    {
                        errMsg = string.Format("Grid{0}中第{1}个条码:{2}在Grid{3}中已经存在！",
                                       gridID,
                                       rowIndex + 1,
                                       currentBarcode,
                                       tmpGrid);
                        return false;
                    }
                }
            }
            return true;
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

        private void InitDataGridView()
        {
            dataGridView.AllowUserToAddRows = false;
            dataGridView.EnableHeadersVisualStyles = false;
            dataGridView.Columns.Clear();
            List<string> strs = new List<string>();
            int gridCnt = (totalSampleCnt + 15) / 16;

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
        


        void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Pipeserver.Close();
        }

        internal void ExecuteCommand(string sCommand)
        {
            if (sCommand.Contains("close"))
            {
                this.Close();
                return;
            }
            if (sCommand == "read")
            {
                tubeID++;
                txtLog.AppendText("read");
            }
        }

        void CreateNamedPipeServer()
        {
            Pipeserver.owner = this;
            ThreadStart pipeThread = new ThreadStart(Pipeserver.createPipeServer);
            Thread listenerThread = new Thread(pipeThread);
            listenerThread.SetApartmentState(ApartmentState.STA);
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        private void WriteResult(bool bok)
        {
            string folder = Utility.GetOutputFolder();
            string resultFile = folder + "barcodeResult.txt";
            File.WriteAllText(resultFile, bok.ToString());
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            tubeID--;
            var gridID = (tubeID + 15) / 16;
            int rowIndex = tubeID - (gridID - 1) * 16 - 1;
            UpdateGridCell(gridID, rowIndex, "");
            
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            try
            {
               CheckTotalSample();
               InitDataGridView();
               tubeID = 1;
            }
            catch(Exception ex)
            {
                AddErrorInfo(ex.Message);
            }
        }

        private void CheckTotalSample()
        {
            totalSampleCnt = int.Parse(txtSampleCnt.Text);
            if (totalSampleCnt <= 0)
                throw new Exception("样本数不得小于0!");
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            richTextInfo.Clear();
        }
    }

}
