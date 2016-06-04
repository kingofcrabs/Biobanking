using Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biobanking
{
    public class TrackInfo
    {
        public string sourceBarcode;
        public string dstBarcode;
        public string description;
        public string volume;
        public TrackInfo(string src, string dst, string desc, string v)
        {
            sourceBarcode = src;
            dstBarcode = dst;
            description = desc;
            volume = v;
        }
    }

    public class BarcodeTracker
    {
        List<TrackInfo> trackInfos = new List<TrackInfo>();
        PipettingSettings pipettingSettings = null;
        List<List<string>> correspondingbarcodes;
        List<string> srcBarcodes;
        int sampleIndex = 0;
        const string buffyName = "Blood-Buffy";
        const string plasmaName = "Blood-Plasma";
        public BarcodeTracker(PipettingSettings pipettingSettings,LabwareSettings labwareSettings,List<string> srcBarcodes)
        {
            this.srcBarcodes = srcBarcodes;
            this.pipettingSettings = pipettingSettings;
            ExcelReader excelReader = new ExcelReader();
            correspondingbarcodes = excelReader.ReadBarcodes(labwareSettings,pipettingSettings);
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
                TrackInfo info = new TrackInfo(srcBarcodes[sampleIndex+ indexInList], 
                    correspondingbarcodes[sampleIndex+ indexInList][sliceIndex],
                    plasmaName,
                    Math.Round(vol, 2).ToString());
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
                            var dstBarcode = correspondingbarcodes[sampleIndex+indexInList][pipettingSettings.dstPlasmaSlice + i];
                            TrackInfo info = new TrackInfo(srcBarcodes[sampleIndex + indexInList],
                            dstBarcode,
                            buffyName,
                            Math.Round(vol, 2).ToString());
                            trackInfos.Add(info);
                        }
                    }
                }
                sampleIndex += plasmaVols.Count;
            }
            
        }

        internal void WriteResult()
        {
            string sFolder = Utility.GetOutputFolder() + DateTime.Now.ToString("yyyyMMdd")+"\\";
            if (!Directory.Exists(sFolder))
                Directory.CreateDirectory(sFolder);
            sFolder += DateTime.Now.ToString("HHmmss") + "\\";
            if (!Directory.Exists(sFolder))
                Directory.CreateDirectory(sFolder);
            var sCSVFile = sFolder + "track.csv";
            var sExcelFile = sFolder + "track.xls";
            List<string> strs = FormatInfos();
            
            File.WriteAllLines(sCSVFile, strs);
            ExcelReader.SaveAsExcel(sCSVFile, sExcelFile);
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
