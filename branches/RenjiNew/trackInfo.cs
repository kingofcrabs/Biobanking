﻿using Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.Data;
using System.Configuration;
using Biobanking.ExcelExporter;

namespace Biobanking
{
    
    public class BarcodeTracker
    {
        List<TrackInfo> trackInfos = new List<TrackInfo>();
        PipettingSettings pipettingSettings = null;
        List<List<Tuple<string,string>>> correspondingbarcodes;
        Dictionary<string, string> barcode_plateBarcodes = new Dictionary<string, string>();
        Dictionary<string, string> barcode_Position = new Dictionary<string, string>();
        List<PatientInfo> patientInfos;
        int sampleIndex = 0;
        
        //const string plasmaName = "Plasma";
        //const string redCellName = "RedCell";
        public BarcodeTracker(PipettingSettings pipettingSettings,LabwareSettings labwareSettings,List<PatientInfo> patientInfos)
        {
            this.patientInfos = patientInfos;
            this.pipettingSettings = pipettingSettings;
            ExcelReader excelReader = new ExcelReader();
            correspondingbarcodes = excelReader.ReadBarcodes(labwareSettings,
                pipettingSettings,
                barcode_plateBarcodes,
                barcode_Position);
            if(patientInfos.Count > correspondingbarcodes.Count)
            {
                throw new Exception(string.Format("source barcodes' count:{0} > dest barcodes' count :{1}", patientInfos.Count, correspondingbarcodes.Count));
            }
        }

        private bool IsValidBarcode(string s) //if no digital, invalid
        {
            foreach (char ch in s)
            {
                if (char.IsDigit(ch))
                    return true;
            }
            return false;
        }

        internal void Track(List<double> vols, int sliceIndex, string typeDescription)
        {
            //trackInfos.Add( new TrackInfo(srcBarcodes[sam))
            int indexInList = 0;
            foreach (var vol in vols)
            {
                var tuple= correspondingbarcodes[sampleIndex + indexInList][sliceIndex];


                string dstBarcode = tuple.Item2;
                int sampleID = sampleIndex + indexInList + 1;
              
                if (dstBarcode == "")
                    throw new Exception(string.Format("Cannot find dest barcode at position: {0} for sample: {1}!", tuple.Item1, sampleID));
                
                if(!IsValidBarcode(dstBarcode))
                {
                    throw new Exception(string.Format("Sample:{0} slice:{1}'s corresponding barcode:{2} is invalid!", sampleID, sliceIndex + 1, dstBarcode));
                }

                var adjustVol = Math.Min(pipettingSettings.maxVolumePerSlice, vol);
                if(patientInfos.Count <= sampleIndex+ indexInList)
                    throw new Exception(string.Format("Cannot find {0}th sample's source barcode!", sampleID));
                var patient = patientInfos[sampleIndex+ indexInList];
                TrackInfo info = new TrackInfo(
                    patient.id,
                    dstBarcode,
                    typeDescription,
                    Math.Round(adjustVol, 2).ToString(),
                    barcode_plateBarcodes[dstBarcode],
                    barcode_Position[dstBarcode],
                    sliceIndex+1,  
                    patient.name, patient.seqNo
                    );
                trackInfos.Add(info);
                indexInList++;
            }

            //if(sliceIndex+1 == pipettingSettings.dstPlasmaSlice )
            //{
            //    //add buffy info
            //    if ( pipettingSettings.dstbuffySlice > 0)
            //    {
            //        double vol = pipettingSettings.buffyVolume / pipettingSettings.dstbuffySlice;
            //        for (indexInList = 0; indexInList < vols.Count; indexInList++)
            //        {
            //            var patient = patientInfos[sampleIndex + indexInList];
            //            for (int i = 0; i < pipettingSettings.dstbuffySlice; i++)
            //            {
            //                if(sampleIndex + indexInList >= correspondingbarcodes.Count )
            //                {
            //                    throw new Exception(string.Format("Cannot find the corresponding barcode for sample:{0}", 
            //                        sampleIndex + indexInList));
            //                }
            //                if(pipettingSettings.dstPlasmaSlice + i >= correspondingbarcodes[sampleIndex + indexInList].Count)
            //                {
            //                    throw new Exception(string.Format("Cannot find the corresponding barcode for sample:{0}, slice:{1}",
            //                        sampleIndex + indexInList, 
            //                        pipettingSettings.dstPlasmaSlice + i));
            //                }
            //                var tuple = correspondingbarcodes[sampleIndex + indexInList][pipettingSettings.dstPlasmaSlice + i];
            //                var position = tuple.Item1;
            //                var dstBarcode = tuple.Item2;
            //                if (dstBarcode == "")
            //                    throw new Exception($"{position}条码为空");
            //                TrackInfo info = new TrackInfo(
            //                patient.id, 
            //                dstBarcode,
            //                GlobalVars.Instance.BuffyName,
            //                Math.Round(vol, 2).ToString(), 
            //                barcode_plateBarcodes[dstBarcode],
            //                barcode_Position[dstBarcode], 
            //                1,
            //                patient.name, patient.seqNo);
            //                trackInfos.Add(info);
            //            }
            //        }
            //    }
            //    sampleIndex += vols.Count;
            //}
            
        }

        internal void WriteResult()
        {
            Utility.SaveSettings(trackInfos, Utility.GetOutputFolder() + "trackinfo.xml");
            string sFolder = Utility.GetOutputFolder() + DateTime.Now.ToString("yyyyMMdd")+"\\";
            if (!Directory.Exists(sFolder))
                Directory.CreateDirectory(sFolder);
        
            WriteResult2SqlServer();
            Save2Excel(sFolder);

        }

        private void Save2Excel(string sFolder)
        {
            string csvFolder = sFolder + "csv\\";
            string excelFolder = sFolder + "excel\\";
            CreateIfNotExist(csvFolder);
            CreateIfNotExist(excelFolder);
            string sTime = DateTime.Now.ToString("HHmmss");
            if (!Directory.Exists(sFolder))
                Directory.CreateDirectory(sFolder);
            var sCSVFile = csvFolder + sTime + ".csv";
            var sExcelFile = excelFolder + sTime + ".xls";
            string excelTemplate = ConfigurationManager.AppSettings["ExcelTemplate"];
            switch (excelTemplate.ToLower())
            {
                case "beijinguniv": //for BeiJing university
                    Save2ExcelForBeiJingUniv(sCSVFile,sExcelFile);
                    break;
                default:
                    DefaultExcelTemplate.Save2Excel(trackInfos, sCSVFile);
                    break;
            }
        }

        private void Save2ExcelForBeiJingUniv(string sCSV, string sExcel)
        {
            BeiJingUnivExcelTemplate.Save2Excel(trackInfos,sCSV,sExcel);
        }

        private void WriteResult2SqlServer()
        {

            string connectionStr = ConfigurationManager.AppSettings["ConnectionString"];
            if (connectionStr == "")
            {
                Console.WriteLine("No sql connection string.");
                return;
            }

            Console.WriteLine("Writing result into sql, it takes a long time, please wait...");
            SqlConnection con = new SqlConnection();
            con.ConnectionString = connectionStr;
            con.Open();
            foreach(var info in trackInfos)
            {
                string str = string.Format(@"insert into interface_tecan_info
(SourceBarcode,
DestBarcode,Volume,TypeDescription,DestPlateBarcode,PositionInPlate) values 
('{0}','{1}','{2}','{3}','{4}','{5}')", 
            info.sourceBarcode,
            info.dstBarcode,
            info.volume,
            info.description,
            info.plateBarcode,
            info.position);
                SqlCommand command = new SqlCommand(str, con);
                command.ExecuteNonQuery();
            }
            con.Close();//关闭数据库
        }

        private void CreateIfNotExist(string csvFolder)
        {
            if (!Directory.Exists(csvFolder))
                Directory.CreateDirectory(csvFolder);
        }

        //private List<string> FormatInfos()
        //{
           
        //    List<string> strs = new List<string>();
        //    strs.Add("Barcode,Sample Source,Sample Type,Volume");
        //    trackInfos.ForEach(x => Format(x, strs));
        //    return strs;
        //}

        //private List<string> FormatInfosBeiJingUniv()
        //{
        //    throw new NotImplementedException();
        //}

        //private void Format(TrackInfo info, List<string> strs)
        //{
        //    string s = string.Format("{0},{1},{2},{3}", info.dstBarcode, info.sourceBarcode, info.description, info.volume);
        //    strs.Add(s);
        //}

    
            
    }

}
