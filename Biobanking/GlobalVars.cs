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
            BloodDescription = File.ReadAllText(Utility.GetBloodTypeFile());
            IsRedCell = BloodDescription == "RedCell";
            BuffyStandalone = bool.Parse(ConfigurationManager.AppSettings["BuffyStandalone"]);
            BloodDescription = TranslateDescription(BloodDescription);
            Barcode2DVendor = ConfigurationManager.AppSettings["2DBarcodeVendor"];
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

        public  static string TranslateDescription(string BloodDescription)
        {
            string barcodeVendor = ConfigurationManager.AppSettings["2DBarcodeVendor"];
            if (barcodeVendor != "HR")
                return BloodDescription;
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("Plasma", "血浆");
            dict.Add("Serum", "血清");
            dict.Add("Buffy", "白膜");
            dict.Add("RedCell", "红细胞");
            return dict[BloodDescription];
        }

        public bool TrackBarcode { get; set; }

        public bool BuffyStandalone { get; set; }
        public string ResultFile { get; set; }

        public string DstBarcodeFolder { get; set; }

        public string SrcBarcodeFile { get; set; }

        public FileStruct FileStruct { get; set; }

        public string BloodDescription { get; set; }

        public bool IsRedCell { get; set; }

        public string BuffyName
        {
            get
            {
                return TranslateDescription("Buffy");
            }
        }

        public string Barcode2DVendor { get; set; }
    }
}
