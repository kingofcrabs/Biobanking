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

namespace Biobanking
{
    
    public class BarcodeTracker
    {
        List<TrackInfo> trackInfos = new List<TrackInfo>();
        PipettingSettings pipettingSettings = null;
        List<List<string>> correspondingbarcodes;
        Dictionary<string, string> barcode_plateBarcodes = new Dictionary<string, string>();
        Dictionary<string, string> barcode_Position = new Dictionary<string, string>();
        List<string> srcBarcodes;
        int sampleIndex = 0;
        const string buffyName = "Blood-Buffy";
        const string plasmaName = "Blood-Plasma";
        public BarcodeTracker(PipettingSettings pipettingSettings,LabwareSettings labwareSettings,List<string> srcBarcodes)
        {
            this.srcBarcodes = srcBarcodes;
            this.pipettingSettings = pipettingSettings;
            ExcelReader excelReader = new ExcelReader();
            correspondingbarcodes = excelReader.ReadBarcodes(labwareSettings,
                pipettingSettings,
                barcode_plateBarcodes,
                barcode_Position);
            if(srcBarcodes.Count > correspondingbarcodes.Count)
            {
                throw new Exception("source barcodes' count > dest barcodes' count");
            }
        }

        

        internal void Track(List<double> plasmaVols, int sliceIndex)
        {
            //trackInfos.Add( new TrackInfo(srcBarcodes[sam))
            int indexInList = 0;
            foreach (var vol in plasmaVols)
            {
                string dstBarcode = correspondingbarcodes[sampleIndex + indexInList][sliceIndex];
                if(!Utility.IsValidBarcode(dstBarcode))
                {
                    int sampleID =sampleIndex + indexInList + 1;
                    int rackID = (sampleID - 1) / 16 + 1;
                    int IDInRack = sampleID - (rackID - 1) * 16;
                    string sDesc = string.Format("rack:{0} position:{1}", rackID, IDInRack);
                    throw new Exception(string.Format("The {0} slice of sample at {1}'s barocde:{2} is illegal.", sliceIndex + 1, sDesc, dstBarcode));
                }
                var adjustVol = Math.Min(pipettingSettings.maxVolumePerSlice, vol);
                TrackInfo info = new TrackInfo(srcBarcodes[sampleIndex+ indexInList],
                    dstBarcode,
                    plasmaName,
                    Math.Round(adjustVol, 2).ToString(),
                    barcode_plateBarcodes[dstBarcode],
                    barcode_Position[dstBarcode]);
                trackInfos.Add(info);
                indexInList++;
            }

            if(sliceIndex+1 == pipettingSettings.dstPlasmaSlice )
            {
                //add buffy info
                if ( pipettingSettings.dstbuffySlice > 0)
                {
                    double vol = pipettingSettings.buffyVolume / pipettingSettings.dstbuffySlice;
                    for (indexInList = 0; indexInList < plasmaVols.Count; indexInList++)
                    {
                        for (int i = 0; i < pipettingSettings.dstbuffySlice; i++)
                        {
                            if(sampleIndex + indexInList >= correspondingbarcodes.Count )
                            {
                                throw new Exception(string.Format("cannot find the corresponding barcode for sample:{0}", 
                                    sampleIndex + indexInList));
                            }
                            if(pipettingSettings.dstPlasmaSlice + i >= correspondingbarcodes[sampleIndex + indexInList].Count)
                            {
                                throw new Exception(string.Format("cannot find the corresponding barcode for sample:{0}, slice:{1}",
                                    sampleIndex + indexInList, 
                                    pipettingSettings.dstPlasmaSlice + i));
                            }
                            var dstBarcode = correspondingbarcodes[sampleIndex+indexInList][pipettingSettings.dstPlasmaSlice + i];
                            TrackInfo info = new TrackInfo(srcBarcodes[sampleIndex + indexInList],
                            dstBarcode,
                            buffyName,
                            Math.Round(vol, 2).ToString(), 
                            barcode_plateBarcodes[dstBarcode],
                            barcode_Position[dstBarcode]);
                            trackInfos.Add(info);
                        }
                    }
                }
                sampleIndex += plasmaVols.Count;
            }
            
        }

        internal void WriteResult()
        {
            Utility.SaveSettings(trackInfos, Utility.GetOutputFolder() + "trackinfo.xml");
            string sFolder = Utility.GetOutputFolder() + DateTime.Now.ToString("yyyyMMdd")+"\\";
            if (!Directory.Exists(sFolder))
                Directory.CreateDirectory(sFolder);
            string csvFolder = sFolder + "csv\\";
            string excelFolder = sFolder + "excel\\";
            CreateIfNotExist(csvFolder);
            CreateIfNotExist(excelFolder);
            //sFolder += DateTime.Now.ToString("HHmmss") + "\\";
            string sTime = DateTime.Now.ToString("HHmmss");
            if (!Directory.Exists(sFolder))
                Directory.CreateDirectory(sFolder);
            var sCSVFile = csvFolder + sTime + ".csv";
            var sExcelFile = excelFolder + sTime + ".xls";
            WriteResult2SqlServer();
            List<string> strs = FormatInfos();
            File.WriteAllLines(sCSVFile, strs);
            ExcelReader.SaveAsExcel(sCSVFile, sExcelFile);
        }

        private void WriteResult2SqlServer()
        {
#if　DEBUG
            return;
#endif
            string conStr = ConfigurationManager.AppSettings["sqlConnectionString"];
         
            if (conStr == "")
            {
                Console.WriteLine("No sql connection string.");
                return;
            }
            Console.WriteLine("Writing result into sql, it takes a long time, please wait...");
            SqlConnection con = new SqlConnection();
            con.ConnectionString = conStr; //"server=192.168.10.128;database=BioBank_tecan;uid=sa;pwd=wonders,1";
            
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

        private List<string> FormatInfos()
        {
            List<string> strs = new List<string>();
            strs.Add("Barcode,Sample Source,Sample Type,Volume");
            trackInfos.ForEach(x => Format(x, strs));
            return strs;
        }

        private void Format(TrackInfo info, List<string> strs)
        {
            string s = string.Format("{0},{1},{2},{3}", info.dstBarcode, info.sourceBarcode, info.description, info.volume);
            strs.Add(s);
        }
    }
}
