namespace Simulator
{
    partial class Form1
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
            this.txtRackIndex = new System.Windows.Forms.TextBox();
            this.txtBatchIndex = new System.Windows.Forms.TextBox();
            this.txtSliceIndex = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.btnSend = new System.Windows.Forms.Button();
            this.chkFinished = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // txtRackIndex
            // 
            this.txtRackIndex.Location = new System.Drawing.Point(216, 111);
            this.txtRackIndex.Name = "txtRackIndex";
            this.txtRackIndex.Size = new System.Drawing.Size(100, 20);
            this.txtRackIndex.TabIndex = 0;
            this.txtRackIndex.Text = "0";
            // 
            // txtBatchIndex
            // 
            this.txtBatchIndex.Location = new System.Drawing.Point(322, 111);
            this.txtBatchIndex.Name = "txtBatchIndex";
            this.txtBatchIndex.Size = new System.Drawing.Size(100, 20);
            this.txtBatchIndex.TabIndex = 1;
            this.txtBatchIndex.Text = "0";
            // 
            // txtSliceIndex
            // 
            this.txtSliceIndex.Location = new System.Drawing.Point(429, 111);
            this.txtSliceIndex.Name = "txtSliceIndex";
            this.txtSliceIndex.Size = new System.Drawing.Size(100, 20);
            this.txtSliceIndex.TabIndex = 2;
            this.txtSliceIndex.Text = "0";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(429, 92);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "SliceIndex";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(213, 95);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "RackIndex";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(319, 92);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "BatchIndex";
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(456, 151);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(75, 23);
            this.btnSend.TabIndex = 6;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // chkFinished
            // 
            this.chkFinished.AutoSize = true;
            this.chkFinished.Location = new System.Drawing.Point(388, 157);
            this.chkFinished.Name = "chkFinished";
            this.chkFinished.Size = new System.Drawing.Size(62, 17);
            this.chkFinished.TabIndex = 7;
            this.chkFinished.Text = "finished";
            this.chkFinished.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(549, 261);
            this.Controls.Add(this.chkFinished);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtSliceIndex);
            this.Controls.Add(this.txtBatchIndex);
            this.Controls.Add(this.txtRackIndex);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtRackIndex;
        private System.Windows.Forms.TextBox txtBatchIndex;
        private System.Windows.Forms.TextBox txtSliceIndex;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.CheckBox chkFinished;
    }
}

