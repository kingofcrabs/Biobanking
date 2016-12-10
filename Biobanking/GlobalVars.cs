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
            IsRedCell = bool.Parse(ConfigurationManager.AppSettings["IsRedCell"]);
            ResultFile = ConfigurationManager.AppSettings[stringRes.reportPath];
            string sFileStruct = Settings.Utility.GetExeFolder() + "fileStruct.xml";
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

        public bool IsRedCell { get; set; }
        public bool TrackBarcode { get; set; }
        public string ResultFile { get; set; }
        public string DstBarcodeFolder { get; set; }
        public string SrcBarcodeFile { get; set; }
        public FileStruct FileStruct { get; set; }

    }
}
