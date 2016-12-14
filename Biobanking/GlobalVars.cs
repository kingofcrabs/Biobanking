using Settings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biobanking
{
    class GlobalVars
    {
        static GlobalVars instance = null;
        public static GlobalVars Instance
        {
            get
            {
                if (instance == null)
                    instance = new GlobalVars();
                return instance;
            }
        }
        private GlobalVars()
        {
            DstBarcodeFolder = ConfigurationManager.AppSettings["DstBarcodeFolder"];
            SrcBarcodeFile = ConfigurationManager.AppSettings["SrcBarcodeFile"];
            ResultFile = ConfigurationManager.AppSettings[stringRes.reportPath];
            string sFileStruct = Settings.Utility.GetExeFolder() + "fileStruct.xml";

            string exePath = Utility.GetExeFolder() + "SampleInfo.exe";
            Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
            BloodDescription = config.AppSettings.Settings["BloodType"].Value;

            TrackBarcode = DstBarcodeFolder != "";
            if (File.Exists(sFileStruct))
            {
                var sContent = File.ReadAllText(sFileStruct);
                FileStruct = Settings.Utility.Deserialize<FileStruct>(sContent);
            }
            else
            {
                FileStruct = new FileStruct();
                Settings.Utility.SaveSettings(FileStruct, sFileStruct);
            }
        }
        public bool TrackBarcode { get; set; }

        public string ResultFile { get; set; }

        public string DstBarcodeFolder { get; set; }

        public string SrcBarcodeFile { get; set; }

        public FileStruct FileStruct { get; set; }

        public string BloodDescription { get; set; }

        public bool IsRedCell
        {
            get
            {
                return BloodDescription == "RedCell";
            }
        }
    }
}
