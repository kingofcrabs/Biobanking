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
        List<List<Tuple<string,string>>> correspondingbarcodes;
        Dictionary<string, string> barcode_plateBarcodes = new Dictionary<string, string>();
        Dictionary<string, string> barcode_Position = new Dictionary<string, string>();
        List<string> srcBarcodes;
        int sampleIndex = 0;
        
        //const string plasmaName = "Plasma";
        //const string redCellName = "RedCell";
        public BarcodeTracker(PipettingSettings pipettingSettings, LabwareSettings labwareSettings, List<string> srcBarcodes)
        {
            this.srcBarcodes = srcBarcodes;
            this.pipettingSettings = pipettingSettings;
            ExcelReader excelReader = new ExcelReader();
            correspondingbarcodes = excelReader.ReadBarcodes(labwareSettings,
                pipettingSettings,
                barcode_plateBarcodes,
                barcode_Position);
            if (srcBarcodes.Count > correspondingbarcodes.Count)
            {
                throw new Exception(string.Format("source barcodes' count:{0} > dest barcodes' count :{1}", srcBarcodes.Count, correspondingbarcodes.Count));
            }
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
                var tuple= correspondingbarcodes[sampleIndex + indexInList][sliceIndex];
                string dstBarcode = tuple.Item2;
                int sampleID = sampleIndex + indexInList + 1;
                if (dstBarcode == "" || dstBarcode == "NOTUBE" ||dstBarcode == "NOREAD")
                    throw new Exception(string.Format("Cannot find dest barcode at position: {0} for sample: {1}!", tuple.Item1, sampleID));
                
                //if(!IsValidBarcode(dstBarcode))
                //{
                //    throw new Exception(string.Format("Sample:{0} slice:{1}'s corresponding barcode:{2} at {3} is invalid!",
                //        sampleID, sliceIndex + 1, dstBarcode, tuple.Item1));
                //}
                var adjustVol = Math.Min(pipettingSettings.maxVolumePerSlice, vol);
                if (srcBarcodes.Count <= sampleIndex + indexInList)
                    throw new Exception(string.Format("Cannot find {0}th sample's source barcode!", sampleID));
                var srcBarcode = srcBarcodes[sampleIndex + indexInList];
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
                        var srcBarcode = srcBarcodes[sampleIndex + indexInList];
                        
                        for (int i = 0; i < pipettingSettings.dstbuffySlice; i++)
                        {
                            if(sampleIndex + indexInList >= correspondingbarcodes.Count )
                            {
                                throw new Exception(string.Format("Cannot find the corresponding barcode for sample:{0}", 
                                    sampleIndex + indexInList));
                            }
                            if(pipettingSettings.dstPlasmaSlice + i >= correspondingbarcodes[sampleIndex + indexInList].Count)
                            {
                                throw new Exception(string.Format("Cannot find the corresponding barcode for sample:{0}, slice:{1}",
                                    sampleIndex + indexInList, 
                                    pipettingSettings.dstPlasmaSlice + i));
                            }
                            var tuple = correspondingbarcodes[sampleIndex+indexInList][pipettingSettings.dstPlasmaSlice + i];
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
