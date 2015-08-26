namespace AutomateBarcode
{
    partial class InputBarcodeForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.lstRackIDs = new System.Windows.Forms.ListBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtBarcode = new System.Windows.Forms.TextBox();
            this.txtInfo = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnExit = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.grpAutoComplete = new System.Windows.Forms.GroupBox();
            this.btnComplete = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.txtCells2CompleteCount = new System.Windows.Forms.TextBox();
            this.chkIgnore = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.grpAutoComplete.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "封装EP管信息：";
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Location = new System.Drawing.Point(15, 30);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.ReadOnly = true;
            this.dataGridView.Size = new System.Drawing.Size(810, 380);
            this.dataGridView.TabIndex = 2;
            // 
            // lstRackIDs
            // 
            this.lstRackIDs.FormattingEnabled = true;
            this.lstRackIDs.Location = new System.Drawing.Point(861, 32);
            this.lstRackIDs.Name = "lstRackIDs";
            this.lstRackIDs.Size = new System.Drawing.Size(126, 121);
            this.lstRackIDs.TabIndex = 13;
            this.lstRackIDs.SelectedIndexChanged += new System.EventHandler(this.lstRackIDs_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(861, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(91, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "原始样品载架：";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(861, 187);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 13);
            this.label3.TabIndex = 15;
            this.label3.Text = "条码";
            // 
            // txtBarcode
            // 
            this.txtBarcode.Location = new System.Drawing.Point(861, 206);
            this.txtBarcode.Name = "txtBarcode";
            this.txtBarcode.Size = new System.Drawing.Size(126, 20);
            this.txtBarcode.TabIndex = 14;
            this.txtBarcode.TextChanged += new System.EventHandler(this.txtBarcode_TextChanged);
            // 
            // txtInfo
            // 
            this.txtInfo.Location = new System.Drawing.Point(15, 429);
            this.txtInfo.Multiline = true;
            this.txtInfo.Name = "txtInfo";
            this.txtInfo.Size = new System.Drawing.Size(810, 116);
            this.txtInfo.TabIndex = 17;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 413);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(43, 13);
            this.label2.TabIndex = 16;
            this.label2.Text = "提示：";
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(912, 519);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 23);
            this.btnExit.TabIndex = 19;
            this.btnExit.Text = "退出";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(912, 490);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(75, 23);
            this.btnUpdate.TabIndex = 18;
            this.btnUpdate.Text = "上传";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // grpAutoComplete
            // 
            this.grpAutoComplete.Controls.Add(this.btnComplete);
            this.grpAutoComplete.Controls.Add(this.label5);
            this.grpAutoComplete.Controls.Add(this.txtCells2CompleteCount);
            this.grpAutoComplete.Location = new System.Drawing.Point(861, 251);
            this.grpAutoComplete.Name = "grpAutoComplete";
            this.grpAutoComplete.Size = new System.Drawing.Size(126, 88);
            this.grpAutoComplete.TabIndex = 21;
            this.grpAutoComplete.TabStop = false;
            this.grpAutoComplete.Text = "自动补齐";
            this.grpAutoComplete.Visible = false;
            // 
            // btnComplete
            // 
            this.btnComplete.Location = new System.Drawing.Point(45, 54);
            this.btnComplete.Name = "btnComplete";
            this.btnComplete.Size = new System.Drawing.Size(75, 22);
            this.btnComplete.TabIndex = 2;
            this.btnComplete.Text = "补齐";
            this.btnComplete.UseVisualStyleBackColor = true;
            this.btnComplete.Click += new System.EventHandler(this.btnComplete_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 31);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(34, 13);
            this.label5.TabIndex = 1;
            this.label5.Text = "数量:";
            // 
            // txtCells2CompleteCount
            // 
            this.txtCells2CompleteCount.Location = new System.Drawing.Point(39, 28);
            this.txtCells2CompleteCount.Name = "txtCells2CompleteCount";
            this.txtCells2CompleteCount.Size = new System.Drawing.Size(81, 20);
            this.txtCells2CompleteCount.TabIndex = 0;
            this.txtCells2CompleteCount.Text = "1";
            // 
            // chkIgnore
            // 
            this.chkIgnore.AutoSize = true;
            this.chkIgnore.Location = new System.Drawing.Point(912, 467);
            this.chkIgnore.Name = "chkIgnore";
            this.chkIgnore.Size = new System.Drawing.Size(74, 17);
            this.chkIgnore.TabIndex = 22;
            this.chkIgnore.Text = "忽略错误";
            this.chkIgnore.UseVisualStyleBackColor = true;
            // 
            // InputBarcodeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1027, 684);
            this.Controls.Add(this.chkIgnore);
            this.Controls.Add(this.grpAutoComplete);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnUpdate);
            this.Controls.Add(this.txtInfo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtBarcode);
            this.Controls.Add(this.lstRackIDs);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dataGridView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "InputBarcodeForm";
            this.Text = "InputBarcode";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.grpAutoComplete.ResumeLayout(false);
            this.grpAutoComplete.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.ListBox lstRackIDs;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtBarcode;
        private System.Windows.Forms.TextBox txtInfo;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.GroupBox grpAutoComplete;
        private System.Windows.Forms.Button btnComplete;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtCells2CompleteCount;
        private System.Windows.Forms.CheckBox chkIgnore;
    }
}