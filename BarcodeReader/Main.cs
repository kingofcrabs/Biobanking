﻿using Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarcodeReader
{
//latest
    public partial class MainForm : Form
    {
        SerialPort serialPort;
        bool programModify = false;
        int totalSampleCnt = 0;
        int tubeID = 0;
        int gridCnt = 0;
        List<string> simulateBarcodes = new List<string>();
        DataGridViewCell lastCell;
        public MainForm()
        {
            InitializeComponent();
            WriteResult(false);
            this.FormClosed += MainForm_FormClosed;
            this.Load += MainForm_Load;
            dataGridView.KeyUp += DataGridView_KeyUp;
        }

       

        private void DataGridView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (dataGridView.CurrentCell.RowIndex == 15 && dataGridView.CurrentCell.Value != null)
                {
                    var columnIndex = dataGridView.CurrentCell.ColumnIndex;
                    if( columnIndex != gridCnt-1)
                        dataGridView.CurrentCell = dataGridView.Rows[0].Cells[columnIndex+1];
                }
            }
          
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            WriteResult(false);
            lblVersion.Text = $"版本号:{AssemblyVersion}";
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
            string file = config.AppSettings.Settings["SrcBarcodeFile"].Value;
            var eachGridBarcodes = GetEachGridBarcodes();
            List<string> allBarcodes = new List<string>();
            foreach(var gridBarcodes in eachGridBarcodes)
            {
                allBarcodes.AddRange(gridBarcodes.Value);
            }
            File.WriteAllLines(file, allBarcodes);
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

        private bool IsValidBarcode(string currentBarcode,Dictionary<int,List<string>>eachGridBarcodes, int gridID, int rowIndex, ref string errMsg)
        {
            if(currentBarcode == "" || currentBarcode == "***" ||
                currentBarcode == "$$$")
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
                    //if (tmpBarcodes[r] == currentBarcode)
                    //{
                    //    errMsg = string.Format("Grid{0}中第{1}个条码:{2}在Grid{3}中已经存在！",
                    //                   gridID,
                    //                   rowIndex + 1,
                    //                   currentBarcode,
                    //                   tmpGrid);
                    //    return false;
                    //}
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
            string smpCntFile = ConfigurationManager.AppSettings["SampleCntFile"];
            totalSampleCnt = int.Parse(File.ReadAllText(smpCntFile));
            txtSampleCnt.Text = totalSampleCnt.ToString();
            gridCnt = (totalSampleCnt + 15) / 16;

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
            lastCell = dataGridView.Rows[0].Cells[0];
            
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
        private void btnClear_Click(object sender, System.EventArgs e)
        {
            richTextInfo.Text = "";
        }

       
        private void btnDelete_Click(object sender, System.EventArgs e)
        {
            UpdateGridCell(dataGridView.CurrentCell.ColumnIndex + 1, dataGridView.CurrentCell.RowIndex, "");
        }
        private void btnImportLastTime_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "请选择文件";
            dialog.RestoreDirectory = true;
            dialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            if (dialog.ShowDialog() != DialogResult.OK)
                return;
            string selectfile = dialog.FileName;
            List<string> strs = File.ReadAllLines(selectfile).ToList();
            tubeID = 1;
            foreach (var barcode in strs)
            {
                OnNewBarcode(barcode);
                if (totalSampleCnt == tubeID)
                    break;
                tubeID++;
            }
        }
        private void btnImport_Click(object sender, EventArgs e)
        {
            string sFile = ConfigurationManager.AppSettings["PosIDFile"];
            List<string> contents = File.ReadAllLines(sFile).ToList();
            contents = contents.Where(x => x != "").ToList();
            List<string> barcodes = new List<string>();
            string firstLine = contents[1];
            string[] strs = firstLine.Split(';');
            var grid = int.Parse(strs[0]);
            contents.RemoveAt(0);
            tubeID = 1;

            foreach (string s in contents)
            {
                var barcode = Parse(s);
                OnNewBarcode(barcode);
                if (totalSampleCnt == tubeID)
                    break;
                tubeID++;
                
            }
            
        }

        private string Parse(string s)
        {
            string[] strs = s.Split(';');
            return strs.Last();
        }

        
    }

}
