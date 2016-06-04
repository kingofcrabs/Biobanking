using Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Calibration
{
    public partial class Main : Form
    {
        List<CalibrationItem> calibItems;
        public Main()
        {
            InitializeComponent();
            var sFile = Utility.GetExeFolder() + stringRes.calibFileName;
            if(File.Exists(sFile))
            {
                string sContent = File.ReadAllText(sFile);
                calibItems = Utility.Deserialize<CalibrationItems>(sContent).calibItems;
                UpdateCalibItems();
            }
           
        }

        private void UpdateCalibItems()
        {
            foreach(var calibItem in calibItems)
            {
                var strs = new string[] { calibItem.volumeUL.ToString(), Convert(calibItem.height),
                calibItem.tipVolume.ToString()};
                ListViewItem itm = new ListViewItem(strs);
                lvCalibration.Items.Add(itm);
            }
        }

        private string Convert(double val)
        {
            return Math.Round(val, 2).ToString();
        }
        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                AddItem(txtHeight.Text, txtVolume.Text,txtTipVolume.Text);
            }
            catch(Exception ex)
            {
                SetInfo(ex.Message, true);
                return;
            }
            SetInfo("添加成功！",false);
        }

        void SetInfo(string info, bool error)
        {
            txtInfo.Text = info;
            if (error)
                txtInfo.ForeColor = Color.Red;
        }

        private void AddItem(string sHeight, string sVolume, string sTipVolume)
        {
            if (sHeight == "" || sVolume == "" || sTipVolume == "")
                throw new Exception("高度，体积，TipVolume不能为空！");
            string[] strs = new string[] { sVolume, sHeight, sTipVolume };
            ListViewItem itm = new ListViewItem(strs);
            txtHeight.Text = "";
            txtVolume.Text = "";
            txtTipVolume.Text = "";
            lvCalibration.Items.Add(itm);
        }

        private void DeleteItem()
        {
            if (lvCalibration.SelectedIndices.Count == 0)
                throw new Exception("请选中要删除的行！");
            lvCalibration.Items.RemoveAt(lvCalibration.SelectedIndices[0]);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                DeleteItem();
            }
            catch (Exception ex)
            {
                SetInfo(ex.Message, true);
            }

        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var sFolder = Utility.GetExeFolder();
            var sFile = sFolder + stringRes.calibFileName;
            calibItems = new List<CalibrationItem>();
            foreach (ListViewItem itm in lvCalibration.Items)
            {
                int volume = int.Parse(itm.SubItems[0].Text);
                double height = Math.Round(double.Parse(itm.SubItems[1].Text), 2);
               
                int tipPerVolume = int.Parse(itm.SubItems[2].Text);
                CalibrationItem calibItem = new CalibrationItem(height, volume, tipPerVolume);
                calibItems.Add(calibItem);
            }
            CalibrationItems items = new CalibrationItems(calibItems);
            Utility.SaveSettings(items, sFile);
            SetInfo("保存成功！", false);
        }

        private void lvCalibration_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvCalibration.SelectedItems.Count == 0)
                return;
            var currentItem = lvCalibration.SelectedItems[0];
         
            txtVolume.Text = currentItem.SubItems[0].Text;
            txtHeight.Text = currentItem.SubItems[1].Text;
            txtTipVolume.Text = currentItem.SubItems[2].Text;
        }
    }
}
