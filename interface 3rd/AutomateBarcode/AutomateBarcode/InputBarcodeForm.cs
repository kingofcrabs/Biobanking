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
using System.Diagnostics;
using AutomateBarcode.TissueManagement;

namespace AutomateBarcode
{
    public partial class InputBarcodeForm : Form
    {

        Barcodes barcodes;
        DataGridViewCell selectedCell;
        Color selCellColor = Color.White;
        public int barcodeLen = 0;
        bool blockMessage = true;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public InputBarcodeForm()
        {
            InitializeComponent();
            this.Load += new EventHandler(InputBarcode_Load);
            dataGridView.CurrentCellChanged += new EventHandler(dataGridView_CurrentCellChanged);
        }

        void ShowEmptyTubeWarning()
        {
            SetInfo("当前选中的管子为空,请重新选择！", Color.Red);
        }

        void dataGridView_CurrentCellChanged(object sender, EventArgs e)
        {
            if (blockMessage)
                return;

            ClearInfo();
            if (dataGridView.CurrentCell == null)
                return;

            int rowIndex = dataGridView.CurrentCell.RowIndex;
            int columnIndex = dataGridView.CurrentCell.ColumnIndex;
            if (!barcodes.IsTubeExists(rowIndex, columnIndex))
            {
                ShowEmptyTubeWarning();
                return;
            }

            RecoverSelectedCell();//recover light blue backcolor to its original one
            selectedCell = dataGridView.CurrentCell;
            string errMsg = "";
            bool bConsistBarcode = barcodes.IsConsist(columnIndex,rowIndex,ref errMsg);

            if (!bConsistBarcode)
            {
                SetInfo(errMsg, Color.Red);
            }
        }

        private void RecoverSelectedCell()
        {
            if (selectedCell == null)
                return;

            if (selectedCell.Style.BackColor != Color.LightBlue)
                return;
            selectedCell.Style.BackColor = selCellColor;
        }

        void InputBarcode_Load(object sender, EventArgs e)
        {
            barcodes = new Barcodes();
            InitDataGridView();
            barcodeLen = int.Parse(ConfigurationManager.AppSettings["barcodeLen"]);

            for (int i = 0; i < barcodes.allRackSmpPlasmaSlices.Count; i++)
            {
                lstRackIDs.Items.Add(string.Format("架子{0}", i + 1));
            }
            lstRackIDs.SelectedIndex = 0;
            selectedCell = dataGridView.Rows[0].Cells[0];
        }

        private string GetSampleTypeName(int colIndex)
        {
            return colIndex < Global.Instance.plasmaSlice ? "Plasma" : "Buffy";
        }

        private void InitDataGridView()
        {
            dataGridView.AllowUserToAddRows = false;
            dataGridView.Columns.Clear();
            List<string> strs = new List<string>();
            int colNum = barcodes.GetColumnCnt();
            for (int j = 0; j < colNum; j++)
                strs.Add("");

            for (int i = 0; i < colNum; i++)
            {
                DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
                string typeName = GetSampleTypeName(i);
                column.HeaderText = string.Format("{0}{1}", typeName, 1 + i);
                dataGridView.Columns.Add(column);
                dataGridView.Columns[i].SortMode = DataGridViewColumnSortMode.Programmatic;
            }
            dataGridView.RowHeadersWidth = 80;
            int curRackSamples = barcodes.curRackPlasmaSlices.Count;
            for (int i = 0; i < curRackSamples; i++)
            {
                dataGridView.Rows.Add(strs.ToArray());
                dataGridView.Rows[i].HeaderCell.Value = string.Format("管{0}", i + 1);
            }


            //set disabled cell's background color to gray
            for (int i = 0; i < curRackSamples; i++)
            {
                //int plasmaSlices = curRackPlasmaSlices[i+1];
                for (int col = 0; col < Global.Instance.plasmaSlice; col++)
                {
                    bool exists = barcodes.IsTubeExists(i, col);
                    if (!exists)
                    {
                        dataGridView.Rows[i].Cells[col].Style.BackColor = Color.LightGray;
                    }
                }
            }
        }

       

        private void SetInfo(string s, Color color)
        {
            txtInfo.Text = s;
            txtInfo.ForeColor = color;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txtBarcode_TextChanged(object sender, EventArgs e)
        {
            if( !barcodes.IsTubeExists(selectedCell.RowIndex,selectedCell.ColumnIndex))
            {
                ShowEmptyTubeWarning();
                return;
            }

            if (selectedCell.RowIndex == -1 || selectedCell.ColumnIndex == -1)
            {
                SetInfo("当前选择单元格非法！", Color.Red);
                return;
            }
            if (txtBarcode.Text.Length != barcodeLen)
                return;
            string sBarcode = txtBarcode.Text;
            if (HasExist(sBarcode))
                return;
            UpdatePackageInfo(sBarcode,selectedCell.ColumnIndex,selectedCell.RowIndex);
            Move2NextCell(selectedCell.RowIndex, selectedCell.ColumnIndex);
            ClearInfo();
            txtBarcode.Text = "";
        }
       
        private void UpdatePackageInfo(string sBarcode,int col , int row)
        {
            Point curPoint = new Point(col + 1, row + 1);
            if (barcodes.thisRackScannedPackageInfos.ContainsKey(curPoint))
            {
                barcodes.thisRackScannedPackageInfos[curPoint] = sBarcode;
            }
            else
                barcodes.thisRackScannedPackageInfos.Add(curPoint, sBarcode);
            //selectedCell.Value = sBarcode;
            UpdateBkColor(col, row,sBarcode);
        }

        private void UpdateBkColor(int col, int row,string scannedBarcode)
        {
            if (scannedBarcode == "")
                return;
            Point curPoint = new Point(col + 1, row + 1);
            var tmpCell = dataGridView.Rows[row].Cells[col];
            tmpCell.Value = scannedBarcode;
            string expectedBarcode = barcodes.thisRackPackageExpectedInfos[curPoint];
            bool consistBarcode = expectedBarcode == scannedBarcode;
            tmpCell.Style.BackColor = consistBarcode ? Color.LightGreen : Color.HotPink;
            if (!consistBarcode)
            {
                //WarnInconsistBarcode(expectedBarcode, scannedBarcode);
                SetInfo(barcodes.GetInconsistWarning(expectedBarcode, scannedBarcode), Color.Red);
            }
        }

        private bool HasExist(string sBarcode)
        {
            bool bContains = barcodes.thisRackScannedPackageInfos.ContainsValue(sBarcode);
            if (!bContains)
                return false;
            else
            {
                var pos = barcodes.thisRackScannedPackageInfos.Where(x => x.Value == sBarcode).Select(x => x.Key).First();
                string typeName = GetSampleTypeName(pos.X);
                SetInfo(string.Format("条码{0}已经存在于：管{1}-{2}{3}", sBarcode, pos.Y, typeName,pos.X), Color.Red);
                return true;
            }
        }

        private void ClearInfo()
        {
            SetInfo("", Color.Black);
        }

        private void Move2NextCell(int curRowIndex, int curColIndex)
        {
            int maxRowThisRack = barcodes.curRackPlasmaSlices.Count;
            int curSampleID = curColIndex * maxRowThisRack + curRowIndex + 1;
            int colNum = Global.Instance.plasmaSlice + Global.Instance.buffySlice;
            int totalBarocodeCnt = dataGridView.Rows.Count*colNum;
            if (curSampleID == totalBarocodeCnt )
                return;
            int nextRowIndex = curRowIndex+1;
            int nextColIndex = curColIndex;
            
            if (curRowIndex == Math.Min(15,maxRowThisRack-1))
            {
                nextRowIndex = 0;
                nextColIndex = selectedCell.ColumnIndex + 1;
            }
            if (barcodes.IsTubeExists(nextRowIndex, nextColIndex))
            {
                selectedCell = dataGridView.Rows[nextRowIndex].Cells[nextColIndex];
                selCellColor = selectedCell.Style.BackColor;
                selectedCell.Style.BackColor = Color.LightBlue;
            }
            else
            {
                Move2NextCell(nextRowIndex, nextColIndex);
            }
        }

        private void lstRackIDs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstRackIDs.SelectedIndex == -1)
                return;
            blockMessage = true;
            barcodes.ChangeRackIndex(lstRackIDs.SelectedIndex);
            Debug.WriteLine(string.Format("Change selected index to {0}.",lstRackIDs.SelectedIndex));
            InitDataGridView();
            FillDataGridView(barcodes.thisRackPackageExpectedInfos,
                barcodes.thisRackScannedPackageInfos);
            blockMessage = false;
            selectedCell = dataGridView.Rows[0].Cells[0];
        }

        private void FillDataGridView(Dictionary<Point, string> expectedBarcodes, 
                                      Dictionary<Point, string> scannedBarcodes)
        {
            Dictionary<Point, string> curBarcodes = expectedBarcodes.ToDictionary(x => x.Key, x => x.Value);//new Dictionary<Point, string>(expectedBarcodes);
            foreach (var pair in scannedBarcodes)
            {
                curBarcodes[pair.Key] = pair.Value;
            }
            foreach (var pair in curBarcodes)
            {
                int colIndex = pair.Key.X - 1;
                int rowIndex = pair.Key.Y - 1;
                var curCell = dataGridView.Rows[rowIndex].Cells[colIndex];
                curCell.Value = pair.Value;
            }
            foreach (var pair in scannedBarcodes)
            {
                int colIndex = pair.Key.X - 1;
                int rowIndex = pair.Key.Y - 1;
                UpdateBkColor(colIndex, rowIndex, pair.Value);
            }

        }
        

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            string errMsg = "";
            bool isOk = barcodes.AllbarcodesAreExpected(ref errMsg);
            if (!isOk)
            {
                SetInfo(errMsg, Color.Red);
                if (!chkIgnore.Checked)
                    return;
            }

            log.Info("try to upload");
            List<TubeInfo> tubeInfos = barcodes.GetTubeInfos();
            try
            {
                UpdateResult updateResult = Global.Instance.server.UpdatePackageInfo(tubeInfos.ToArray());
                if ((bool)updateResult.bok)
                    SetInfo("上传成功！", Color.Green);
                else
                {
                    string description = string.Format("上传失败，原因是：{0}", updateResult.errDescription);
                    SetInfo(description, Color.Red);
                    log.Error(description);
                }
            }
            catch (Exception ex)
            {
                string description = string.Format("上传失败，原因是：{0}", ex.Message);
                SetInfo(description, Color.Red);
                log.Error(description);
            }
        }


        #region complete
        private void btnComplete_Click(object sender, EventArgs e)
        {
            int num2Complete;
            bool bInputOk = int.TryParse(txtCells2CompleteCount.Text, out num2Complete);
            if (!bInputOk)
            {
                SetInfo("补齐数量必须为数字", Color.Red);
                return;
            }
            if (num2Complete <= 0)
            {
                SetInfo("补齐数量必须大于0", Color.Red);
                return;
            }

            if (dataGridView.SelectedCells.Count != 2)
            {
                SetInfo("需选择两个单元格作为后续单元格的补齐模板。", Color.Red);
                return;
            }

            List<DataGridViewCell> selCells = new List<DataGridViewCell>();
            foreach (DataGridViewCell cell in dataGridView.SelectedCells)
            {
                selCells.Add(cell);
            }

            //sort the selected cells by column then row index
            selCells = selCells.OrderBy(x => x.ColumnIndex * 16 + x.RowIndex).ToList();
            //DataGridViewCell firstCell = selCells.First();

            string errMessage = "";
            bool bRightFormat = CheckSelCellsFormat(selCells[0], selCells[1], out errMessage);
            if (!bRightFormat)
            {
                SetInfo(errMessage,Color.Red);
                return;
            }

            FillTheCells(selCells, num2Complete);    
        }

        private void FillTheCells(List<DataGridViewCell> selCells, int num2Complete)
        {
            Permutation permutaion = new Permutation();
            bool finished = false;
            List<string> strs = permutaion.GetPossibleBarcodes(selCells[0].Value.ToString(), selCells[1].Value.ToString(), num2Complete);
            int colStart = selCells[1].ColumnIndex;
            int rowStart = selCells[1].RowIndex;
            int startCellID = colStart * 16 + rowStart;
            int curAutoCellIndex = 0;
            foreach (string s in strs)
            {
                if (HasExist(s))
                    return;
            }
            for (int col = colStart; col < dataGridView.Columns.Count; col++)
            {
                for (int row = 0; row < dataGridView.Rows.Count; row++)
                {
                    if (row + col * 16 <= startCellID)
                        continue;
                    if (!barcodes.IsTubeExists(row, col))
                        continue;
                    string sTmpBarcode = strs[curAutoCellIndex++];
                    dataGridView.Rows[row].Cells[col].Value = sTmpBarcode;
                    UpdatePackageInfo(sTmpBarcode,col,row);
                    //ValidateCell(dataGridView1.Rows[row].Cells[col]);
                    if (curAutoCellIndex == num2Complete)
                    {
                        finished = true;
                        break;
                    }
                }

                if (finished)
                    break;
            }
        }

        private bool CheckSelCellsFormat(DataGridViewCell firstCell, DataGridViewCell secondCell, out string errMsg)
        {
            //check the selected cell's format
            int row = firstCell.RowIndex;
            int col = firstCell.ColumnIndex;
            if (!IsValidBarcode(firstCell.Value.ToString(), out errMsg, row, col))
            {
                errMsg = "首单元格的条形码格式不正确： " + errMsg;
                //SetInfo(Color.Red, "首单元格的条形码格式不正确： " + sErrMsg);
                return false;
            }

            row = secondCell.RowIndex;
            col = secondCell.ColumnIndex;
            if (!IsValidBarcode(secondCell.Value.ToString(), out errMsg, row, col))
            {
                errMsg = "次单元格的条形码格式不正确： " + errMsg;
                //SetInfo(Color.Red, "首单元格的条形码格式不正确： " + sErrMsg);
                return false;
            }

            if (firstCell.Value.ToString().Length != secondCell.Value.ToString().Length)
            {
                errMsg = "首单元格与次单元格的条码长度不一致。";
                return false;
            }

            int nDiffCharNum = 0;
            string sFirstCell = firstCell.Value.ToString();
            string sSecondCell = secondCell.Value.ToString();
            for (int i = 0; i < sFirstCell.Length; i++)
            {
                char chr1 = sFirstCell[i];
                char chr2 = sSecondCell[i];
                if (chr1 != chr2)
                {
                    nDiffCharNum++;

                    if (!IsSameType(chr1, chr2))
                    {
                        errMsg = string.Format("首单元格与次单元格的字符在第{0}处不匹配，必须同为数字、小写字母或大写字母。", i + 1);
                        return false;
                    }
                    if (chr2 < chr1)
                    {
                        errMsg = "次单元格的条码不得小于首单元格的条码！";
                        return false;
                    }

                    if (chr2 - chr1 != 1)
                    {
                        errMsg = "次单元格的条码与首单元格的条码必须邻接！";
                        return false;
                    }
                }
            }

            if (nDiffCharNum > 1)
            {
                errMsg = "首单元格与次单元格不同的字符数大于1，无法推断其余单元格的内容。";
                return false;
            }
            return true;
        }
        private bool IsSameType(char chr1, char chr2)
        {
            if (Char.IsDigit(chr1))
                return Char.IsDigit(chr2);

            if (Char.IsLower(chr1))
                return Char.IsLower(chr2);

            if (Char.IsUpper(chr1))
                return Char.IsUpper(chr2);
            return chr1 == chr2;
        }
        private bool IsValidBarcode(string barcode, out string errMsg, int row, int col)
        {
            errMsg = "";
            return true;
        }
        #endregion
    }

    

}
