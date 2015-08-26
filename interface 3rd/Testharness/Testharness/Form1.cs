using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Testharness.BiobankService;
using System.Diagnostics;

namespace Testharness
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
            
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            BiobankExternalServicePortTypeClient client = new BiobankExternalServicePortTypeClient();
            TubeInfo[] tubeInfos = new TubeInfo[2];
            tubeInfos[0] = CreateTubeInfo("111","plasma","1","100");
            tubeInfos[1] = CreateTubeInfo("222","buffy","2","200");
            var result = client.updatePackageInfo(tubeInfos);
            Debug.Write(string.Format("isOk : {0}, errMessage:{1}, batchID{2}", 
                result.isOk, result.errMessage, result.batchID));
        }

        private TubeInfo CreateTubeInfo(string barcode,string smpType, string sliceID, string vol)
        {
            TubeInfo tmpTube = new TubeInfo();
            tmpTube.barcode = barcode;
            tmpTube.sampleType = smpType;
            tmpTube.sliceID = sliceID;
            tmpTube.volume = vol;
            return tmpTube;
        }
    }
}
