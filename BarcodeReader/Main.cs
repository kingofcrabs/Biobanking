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
        int tubeID = 0;
        
        List<string> simulateBarcodes = new List<string>();
        public MainForm()
        {
            InitializeComponent();
            this.FormClosed += MainForm_FormClosed;
            this.Load += MainForm_Load;
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            bool isSimulation = bool.Parse(ConfigurationManager.AppSettings["Simulation"]);
            if(isSimulation)
            {
                for(int i = 0;i< 16;i++)
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
            }
            dataGridView.CellValueChanged += DataGridView_CellValueChanged;
            InitDataGridView();
            
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
                cell.ReadOnly = true;
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
            }
            ProcessHelper.CloseWaiter();
            btnConfirm.Enabled = false;
        }

        private void CheckBarcodes()
        {
            //List<string> aheadBarcodes = barcodes.Take(rowIndex).ToList();
            //foreach (var pair in eachGridBarcodes)
            //{
            //    int tmpGrid = pair.Key;
            //    var tmpBarcode = pair.Value;
            //    if (tmpGrid >= grid)
            //        continue;
            //    if (tmpBarcode.Contains(sCurrentBarcode))
            //    {
            //        errMsg = string.Format("Grid{0}中第{1}个条码:{2}在Grid{3}中已经存在！",
            //        grid,
            //        rowIndex + 1,
            //        sCurrentBarcode,
            //        tmpGrid);
            //        return false;
            //    }
            //}
        }

        private void btnSimulateBarcode_Click(object sender, EventArgs e)
        {
            tubeID++;
            OnNewBarcode(simulateBarcodes[tubeID - 1]);
        }

        private void OnNewBarcode(string newBarcode)
        {
            
            txtLog.AppendText(newBarcode+"\r\n");
           

            var gridID = (tubeID + 15) / 16;
            int rowIndex = tubeID - (gridID - 1) * 16 -1;
            programModify = true;
            UpdateGridCell(gridID, rowIndex, newBarcode);
            programModify = false;
        }
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string newBarcode = serialPort.ReadLine();
            OnNewBarcode(newBarcode);
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

        private bool IsValidBarcode(string currentBarcode,Dictionary<int,List<string>>eachGridBarcodes, int grid, int rowIndex, ref string errMsg)
        {
            if(currentBarcode == "")
            {
                errMsg = string.Format("Grid{0}中第{1}个条码为空！", grid + 1, rowIndex + 1);
                return false;
            }
            foreach (var pair in eachGridBarcodes)
            {
                int tmpGrid = pair.Key;
                var tmpBarcodes = pair.Value;
                if (tmpGrid > grid)
                    continue;
                for( int r = 0; r< rowIndex; r++)
                {
                    if(tmpBarcodes[r] == currentBarcode)
                    {
                        errMsg = string.Format("Grid{0}中第{1}个条码:{2}在Grid{3}中已经存在！",
                                       grid,
                                       rowIndex + 1,
                                       currentBarcode,
                                       tmpGrid);
                        return false;
                    }
                }
            }
            return true;
        }

        //private bool IsValidBarcode(string newBarcode,ref string msg)
        //{
        //    bool alreadyExist = BarcodeAlreadyExists(newBarcode);
        //    if(alreadyExist)
        //    {
        //        msg = "条码已经存在！";
        //    }
        //    if(newBarcode == "")
        //    {
        //        msg = "条码不得为空！";
        //    }
        //    return !alreadyExist && newBarcode != "";
        //}

        private void AddErrorInfo(string txt)
        {
            richTextInfo.SelectionColor = Color.Red;
            richTextInfo.AppendText(txt + "\r\n");
        }

        private void InitDataGridView()
        {
            dataGridView.AllowUserToAddRows = false;
            dataGridView.EnableHeadersVisualStyles = false;
            dataGridView.Columns.Clear();
            List<string> strs = new List<string>();
            totalSampleCnt = int.Parse(Utility.ReadFolder(stringRes.SampleCountFile));
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

            //for(int r = 0; r < dataGridView.Rows.Count; r++)
            //{
            //    for( int c = 0; c < dataGridView.Rows[0].Cells.Count; c++)
            //    {
            //        dataGridView.Rows[r].Cells[c].Value = "";
            //    }
            //}
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

        
    }

}
