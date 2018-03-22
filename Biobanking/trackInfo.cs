using Settings;
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
        Dictionary<string,List<Tuple<int,string>>> srcbarcode_EachSliceDstPositionAndBarcode;
        Dictionary<string, string> barcode_plateBarcodes = new Dictionary<string, string>();
        Dictionary<string, string> barcode_Position = new Dictionary<string, string>();
        List<string> srcBarcodes;
        int sampleIndex = 0;
        
        //const string plasmaName = "Plasma";
        //const string redCellName = "RedCell";
        public BarcodeTracker(PipettingSettings pipettingSettings, LabwareSettings labwareSettings, List<string> srcBarcodes,int startWellID)
        {
            this.srcBarcodes = srcBarcodes;
            this.pipettingSettings = pipettingSettings;
            ExcelReader excelReader = new ExcelReader();
            srcbarcode_EachSliceDstPositionAndBarcode = excelReader.ReadBarcodes(labwareSettings, pipettingSettings, barcode_plateBarcodes, barcode_Position, srcBarcodes, startWellID);
        }

        private bool IsValidBarcode(string s)
        {
            foreach (char ch in s)
            {
                if (char.IsDigit(ch))
                    return true;
            }
            return false;
        }

        internal void Track(List<double> plasmaVols, int sliceIndex,List<DetectedInfo> detectInfos)
        {
            //trackInfos.Add( new TrackInfo(srcBarcodes[sam))
            int indexInList = 0;
            foreach (var vol in plasmaVols)
            {
                string srcBarcode = detectInfos[indexInList].sBarcode;
                var tuple = srcbarcode_EachSliceDstPositionAndBarcode[srcBarcode][sliceIndex];
                string dstBarcode = tuple.Item2;
                int sampleID = sampleIndex + indexInList + 1;
                if (dstBarcode == "" || dstBarcode == "NOTUBE" ||dstBarcode == "NOREAD")
                    throw new Exception(string.Format("Cannot find dest barcode at position: {0} for sample: {1}!", tuple.Item1, sampleID));
                
                var adjustVol = Math.Min(pipettingSettings.maxVolumePerSlice, vol);
                
                string description = GlobalVars.Instance.BloodDescription;
                TrackInfo info = new TrackInfo(
                    srcBarcode,
                    dstBarcode,
                    description,
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
                        string srcBarcode = detectInfos[indexInList].sBarcode;
                        for (int i = 0; i < pipettingSettings.dstbuffySlice; i++)
                        {
                            if(sampleIndex + indexInList >= srcbarcode_EachSliceDstPositionAndBarcode.Count )
                            {
                                throw new Exception(string.Format("Cannot find the corresponding barcode for sample:{0}", 
                                    sampleIndex + indexInList));
                            }
                            //if(pipettingSettings.dstPlasmaSlice + i >= srcbarcode_EachSliceDstPositionAndBarcode[sampleIndex + indexInList].Count)
                            //{
                            //    throw new Exception(string.Format("Cannot find the corresponding barcode for sample:{0}, slice:{1}",
                            //        sampleIndex + indexInList, 
                            //        pipettingSettings.dstPlasmaSlice + i));
                            //}
                            var tuple = srcbarcode_EachSliceDstPositionAndBarcode[srcBarcode][pipettingSettings.dstPlasmaSlice + i];
                            var dstBarcode = tuple.Item2;
                            var position = tuple.Item1;
                            if(dstBarcode == "" || dstBarcode== "NOREAD" || dstBarcode == "NOTUBE")
                            {
                                throw new Exception(string.Format("Cannot find dest barcode at position: {0} for sample: {1}!", position, sampleIndex + indexInList+1));
                            }

                            double tempVol = vol;
                            if (detectInfos[indexInList].Z2 == 100)
                            {
                                tempVol = 0;
                            }

                            TrackInfo info = new TrackInfo(
                            srcBarcode, 
                            dstBarcode,
                            GlobalVars.Instance.BuffyName,
                            Math.Round(tempVol, 2).ToString(), 
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
        
            WriteResult2SqlServer();
            Save2Excel(sFolder);

        }

        private void Save2Excel(string sFolder)
        {
            string csvFolder = sFolder + "csv\\";
            string excelFolder = sFolder + "excel\\";
            CreateIfNotExist(csvFolder);
            CreateIfNotExist(excelFolder);
          
            if (!Directory.Exists(sFolder))
                Directory.CreateDirectory(sFolder);
         
            DefaultExcelTemplate.Save2Excel(trackInfos, csvFolder);
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

    }

}
