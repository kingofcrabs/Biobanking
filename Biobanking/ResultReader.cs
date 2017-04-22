﻿using System;
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
        List<PatientInfo> ReadPatientInfos();
    }
    class BaseReader:IResultReader
    {
        public List<PatientInfo> ReadPatientInfos()
        {
            PatientInfoReader reader = new PatientInfoReader();
            return reader.Read();
        }

        virtual public List<DetectedInfo> Read()
        {
            throw new NotImplementedException();
        }
    }
    class SciRobotReader : BaseReader
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

        public override List<DetectedInfo> Read()
        {
            SciRobotHelper sciRobotHelper = new SciRobotHelper();
            List<DetectedInfo> heights = new List<DetectedInfo>();
            //check capacity
            sciRobotHelper.ReadZValues(ref heights);
            return heights;
        }


        
    }
  
    class PatientInfoReader
    {
        public  List<PatientInfo> Read()
        {
            List<string> contents = null;
            if (GlobalVars.Instance.DstBarcodeFolder == "")
                return null;
            List<string> trimedBarcodes = new List<string>();
            contents = File.ReadAllLines(GlobalVars.Instance.SrcBarcodeFile).ToList();
            contents.RemoveAll(x => x.Trim() == "");
            List<PatientInfo> patientInfos = new List<PatientInfo>();
            if(GlobalVars.Instance.Barcode2DVendor == "HR")
            {
                contents = contents.Skip(1).ToList();
                contents.ForEach(x => patientInfos.Add(Parse(x)));
            }
            
            return patientInfos;
        }

        private PatientInfo Parse(string content)
        {
            char splitter = ',';
            if (content.Contains('\t'))
                splitter = '\t';

            if (content.Contains(splitter))
            {
                string[] strs = content.Split(splitter);
                if(strs.Length == 3)
                    return new PatientInfo(strs[0], strs[1], strs[2]);
                else if (strs.Length >= 5)
                    return new PatientInfo(strs[4], strs[1],strs[0],strs[2]);
                else
                {
                    throw new Exception("Invalid patient information format!");
                }
            }
            else
                return new PatientInfo(content, "","","");
        }


        
    }
      
    class TIUReader : BaseReader
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

        public override List<DetectedInfo> Read()
        {
            List<DetectedInfo> heights = new List<DetectedInfo>();
           
            
        
            string reportPath = GlobalVars.Instance.ResultFile;//ConfigurationManager.AppSettings[stringRes.reportPath];
            int line = 1;

            var unitIsMM = ConfigurationManager.AppSettings["UnitIsMM"];
            double ratio = 1;
            if (unitIsMM != null && !bool.Parse(unitIsMM))
                ratio = 0.1;
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
                    detectedInfo.Z1 = double.Parse(vals[1]) * ratio;
                    detectedInfo.Z2 = double.Parse(vals[2]) * ratio;
                    //if(barcodes != null)
                    //    detectedInfo.sBarcode = trimedBarcodes[line-1];//vals[0];
                    line++;
                    heights.Add(detectedInfo);
                    if(GlobalVars.Instance.IsRedCell)
                    {
                        if (detectedInfo.Z1 < 5 )
                            throw new Exception("Z1 cannot be smaller than 5mm at line: " + line);
                        detectedInfo.Z2 = 5;
                    }
                    else if (detectedInfo.Z1 < 0 || detectedInfo.Z2 < 0)
                        throw new Exception("Z1,Z2 cannot be smaller than 0 at line: " + line);
                    curRow++;
                }
            }
            return heights;
        }
    }

    class RelaxReader : BaseReader
    {
        public override List<DetectedInfo> Read()
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
                    case "SCIRobotic":
                        impliment = new SciRobotReader();
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
