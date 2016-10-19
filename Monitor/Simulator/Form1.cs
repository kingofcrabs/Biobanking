using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Pipes;
using System.IO;
using System.Threading;

namespace Simulator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form1_Load);
        }

        void Form1_Load(object sender, EventArgs e)
        {
            Thread.Sleep(1000);
            SendCommand("Measure;True");
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            
            if (chkFinished.Checked)
            {
                SendCommand("Pipetting;True");
                return;
            }
            SendCommand(string.Format("Pipetting;{0};{1};{2}", txtRackIndex.Text, txtBatchIndex.Text, txtSliceIndex.Text));
        }


        private static void SendCommand(string sContent)
        {

            string sProgramName = "BiobankingMonitor";
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", sProgramName,
                                                                           PipeDirection.Out,
                                                                        PipeOptions.None))
            {

                Console.WriteLine("Attempting to connect to pipe...");
                try
                {
                    pipeClient.Connect(1000);
                }
                catch
                {
                    Console.WriteLine("The Pipe server must be started in order to send data to it.");
                    return;
                }
                Console.WriteLine("Connected to pipe.");

                using (StreamWriter sw = new StreamWriter(pipeClient))
                {
                    sw.Write(sContent);
                }
            }
        }
    }
}
