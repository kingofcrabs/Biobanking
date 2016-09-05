using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;
using System.IO;
namespace Biobanking
{
    public interface IResultReader
    {
        //bool HasBuffyCoat();
        List<DetectedInfo> Read();

    }

    class SciRobotReader : IResultReader
    {
        private bool HasBuffyCoat(string sFile)
        {
            string sRes = "False";
            using (StreamReader sr = new StreamReader(sFile))
            {
                sRes = sr.ReadLine();
                sRes = sr.ReadLine();
            }
            int val = int.Parse(sRes.Last().ToString());
            return val == 1;
        }

        public bool HasBuffyCoat()
        {
            string buffyCoatFile = @"C:\BuffyEx\data\Options.csv";
            return HasBuffyCoat(buffyCoatFile);
        }

        public List<DetectedInfo> Read()
        {
            SciRobotHelper sciRobotHelper = new SciRobotHelper();
            List<DetectedInfo> heights = new List<DetectedInfo>();
            //check capacity
            sciRobotHelper.ReadZValues(ref heights);
            return heights;
        }
    }

    class TIUReader : IResultReader
    {
        //public bool HasBuffyCoat()
        //{
        //    string reportPath = ConfigurationManager.AppSettings[stringRes.reportPath];
        //    int pos = reportPath.LastIndexOf('\\');
        //    string sDir = reportPath.Substring(0, pos);
        //    string sPath = sDir + "\\hasBuffy.txt";
        //    bool bHasBuffy = false;
        //    if (!File.Exists(sPath))
        //        return false;
        //    using (StreamReader sr = new StreamReader(sPath))
        //    {
        //        string s = sr.ReadLine();
        //        bHasBuffy = bool.Parse(s);
        //    }
        //    return bHasBuffy;
        //}
      
        public List<DetectedInfo> Read()
        {
            List<DetectedInfo> heights = new List<DetectedInfo>();
            List<string> barcodes = null;
            List<string> trimedBarcodes = new List<string>();
            if (GlobalVars.Instance.DstBarcodeFolder != "")
            {
                barcodes = File.ReadAllLines(GlobalVars.Instance.SrcBarcodeFile).ToList();
              
                barcodes.ForEach(x => trimedBarcodes.Add(x.Trim()));
                HashSet<string> uniqueBarcodes = new HashSet<string>(trimedBarcodes);
                if(uniqueBarcodes.Count != trimedBarcodes.Count)
                {
                    var duplicates = trimedBarcodes.GroupBy(s => s).Where(grp => grp.Count() > 1);
                    string duplicated = duplicates.First().Key;
                    throw new Exception(string.Format("条码:{0}重复",duplicated));
                }
            }
            string reportPath = GlobalVars.Instance.ResultFile;//ConfigurationManager.AppSettings[stringRes.reportPath];
            int line = 1;
            using (StreamReader sr = new StreamReader(reportPath))
            {
                string sContent = "";
                bool bFirstRow = true;
                int curRow = 0;
                while (true)
                {
                    sContent = sr.ReadLine();
                    if (sContent == null)
                        break;
                    if (sContent == "")
                        continue;
                    if (bFirstRow)
                    {
                        bFirstRow = false;
                        continue;
                    }
                    
                    DetectedInfo detectedInfo = new DetectedInfo();
                    int infoIndex = (curRow - 1);
                    string[] vals = sContent.Split(',');
                    detectedInfo.Z1 = double.Parse(vals[1])/10;
                    detectedInfo.Z2 = double.Parse(vals[2])/10;
                    if(barcodes != null)
                        detectedInfo.sBarcode = trimedBarcodes[line++];//vals[0];
                    heights.Add(detectedInfo);

                    if (detectedInfo.Z1 < 0 || detectedInfo.Z2 < 0)
                        throw new Exception("Z1,Z2 cannot be smaller than 0 at line: " + heights.Count.ToString());
                    curRow++;
                }
            }
            return heights;
        }
    }

    class RelaxReader : IResultReader
    {
        public List<DetectedInfo> Read()
        {
            string sReportXml = ConfigurationManager.AppSettings[stringRes.reportPath];
            DataSet ds = new DataSet();
            ds.ReadXml(sReportXml);
            DataTable dt = ds.Tables[0];
            List<DetectedInfo> heights = new List<DetectedInfo>();
            Dictionary<int, string> nameValDict = new Dictionary<int, string>();
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                nameValDict.Add(i, dt.Columns[i].Caption);
            }

            foreach (DataRow dr in dt.Rows)
            {

                DetectedInfo detectResult = new DetectedInfo();
                for (int i = 0; i < dr.ItemArray.Count(); i++)
                {
                    if (nameValDict[i] == "Z1")
                        detectResult.Z1 = 10 * double.Parse(dr.ItemArray[i].ToString());
                    if (nameValDict[i] == "Z2")
                        detectResult.Z2 = 10 * double.Parse(dr.ItemArray[i].ToString());
                }
                if (detectResult.Z1 < detectResult.Z2)
                    throw new Exception("Z1 must be greater than Z2");
                heights.Add(detectResult);
            }
            return heights;
        }
    }


    class ResultReader
    {
        static IResultReader impliment = null;
        public static IResultReader Instance
        {
            get
            {
                if (impliment != null)
                    return impliment;
                string sMeasureName = ConfigurationManager.AppSettings[stringRes.MeasureName];
                switch (sMeasureName)
                {
                    case "TIU":
                        impliment = new TIUReader();
                        break;
                    default:
                        impliment = new RelaxReader();
                        break;
                }
                return impliment;
            }
        }
    }
}
