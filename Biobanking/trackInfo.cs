﻿using Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biobanking
{
    
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

        private bool IsValidBarcode(string s)
        {
            foreach (char ch in s)
            {
                if (char.IsDigit(ch))
                    return true;
            }
            return false;
        }

        internal void Track(List<double> plasmaVols, int sliceIndex)
        {
            //trackInfos.Add( new TrackInfo(srcBarcodes[sam))
            int indexInList = 0;
            foreach (var vol in plasmaVols)
            {
                string correspondingbarcode = correspondingbarcodes[sampleIndex + indexInList][sliceIndex];
                if(!IsValidBarcode(correspondingbarcode))
                {
                    throw new Exception(string.Format("第{0}个样品对应的第{1}份目标条码:{2}非法！", sampleIndex + indexInList + 1, sliceIndex + 1,correspondingbarcode));
                }
                var adjustVol = Math.Min(pipettingSettings.maxVolumePerSlice, vol);
                TrackInfo info = new TrackInfo(srcBarcodes[sampleIndex+ indexInList],
                    correspondingbarcode,
                    plasmaName,
                    Math.Round(adjustVol, 2).ToString());
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
            List<string> strs = FormatInfos();
            File.WriteAllLines(sCSVFile, strs);
            ExcelReader.SaveAsExcel(sCSVFile, sExcelFile);
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
