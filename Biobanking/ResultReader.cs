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
        List<DetectedHeight> Read();

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

        public List<DetectedHeight> Read()
        {
            SciRobotHelper sciRobotHelper = new SciRobotHelper();
            List<DetectedHeight> heights = new List<DetectedHeight>();
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
      
        public List<DetectedHeight> Read()
        {
            //SciRobotHelper sciRobotHelper = new SciRobotHelper();
            //List<DetectedHeight> heights = new List<DetectedHeight>();
            ////check capacity
            //sciRobotHelper.ReadZValues(ref heights);
            //return heights;
            List<DetectedHeight> heights = new List<DetectedHeight>();
            string reportPath = ConfigurationManager.AppSettings[stringRes.reportPath];
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
                    
                    DetectedHeight detectedHeight = new DetectedHeight();
                    int infoIndex = (curRow - 1);
                    string[] vals = sContent.Split(',');
                    detectedHeight.Z1 = double.Parse(vals[1])/10; //convert to mm, tiu result is 1/10 mm
                    detectedHeight.Z2 = double.Parse(vals[2])/10;
                    heights.Add(detectedHeight);

                    if (detectedHeight.Z1 < 0 || detectedHeight.Z2 < 0)
                        throw new Exception("Z1,Z2 cannot be smaller than 0 at line: " + heights.Count.ToString());
                    curRow++;
                }
            }
            return heights;
        }
    }

    class RelaxReader : IResultReader
    {
        public List<DetectedHeight> Read()
        {
            string sReportXml = ConfigurationManager.AppSettings[stringRes.reportPath];
            DataSet ds = new DataSet();
            ds.ReadXml(sReportXml);
            DataTable dt = ds.Tables[0];
            List<DetectedHeight> heights = new List<DetectedHeight>();
            Dictionary<int, string> nameValDict = new Dictionary<int, string>();
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                nameValDict.Add(i, dt.Columns[i].Caption);
            }

            foreach (DataRow dr in dt.Rows)
            {

                DetectedHeight detectResult = new DetectedHeight();
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
                string sThirdPartyName = ConfigurationManager.AppSettings[stringRes.ThridPartyName];
                switch (sThirdPartyName)
                {
                    case "SciRobotic":
                        impliment = new SciRobotReader();
                        break;
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
