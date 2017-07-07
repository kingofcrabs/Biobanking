﻿using Microsoft.Office.Interop.Excel;
using Settings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace Biobanking
{
    class ExcelReader
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        int startIndex = 0;
        LabwareSettings labwareSettings;
        PipettingSettings pipettingSettings;
        public List<List<Tuple<string,string>>> ReadBarcodes(LabwareSettings labwareSettings,
            PipettingSettings pipettingSettings,
            Dictionary<string, string> barcode_plateBarcode, 
            Dictionary<string,string> barcode_Position)
        {
            startIndex = 0;
            this.labwareSettings = labwareSettings;
            this.pipettingSettings = pipettingSettings;
            string sFolder = GlobalVars.Instance.DstBarcodeFolder;
            var di = new DirectoryInfo(sFolder);
            var files = di.EnumerateFiles("*.csv").ToList();
            files = files.OrderBy(x => x.CreationTime).ToList();
            List<List<Tuple<string,string>>> correspondingbarcodes = new List<List<Tuple<string,string>>>();
            List<string> fileFullNames = files.Select(x => x.FullName).ToList();
            string buffyPlateName = "";
            
            if(pipettingSettings.buffyStandalone && pipettingSettings.dstbuffySlice != 0)
            {
                int cnt = files.Count(x => x.FullName.ToLower().Contains("buffy"));
                if( cnt == 0)
                    throw new Exception("No barcode file for buffy plate found!");
                if( cnt != 1)
                    throw new Exception("Only one buffy plate supported!");
                buffyPlateName = files.Where(x => x.FullName.ToLower().Contains("buffy")).First().FullName;
                fileFullNames = fileFullNames.Except(new List<string>() { buffyPlateName }).ToList();
            }
          
            fileFullNames.ForEach(x => ReadBarcode(correspondingbarcodes, barcode_plateBarcode, barcode_Position, x));
            if(pipettingSettings.dstbuffySlice > 0 && pipettingSettings.buffyStandalone)
            {
                ReadBarcode(correspondingbarcodes, barcode_plateBarcode, barcode_Position, buffyPlateName);
            }

            return correspondingbarcodes;
        }

        private int GetNum(DirectoryInfo d)
        {
            return int.Parse(d.Name.Substring(5));
        }

        private void ReadBarcode(List<List<Tuple<string,string>>> srcTubeCorrespondingBarcodes,
            Dictionary<string, string>barcode_plateBarcode,
            Dictionary<string,string> barcode_Position,
            string sFile)
        {
            var strs = File.ReadAllLines(sFile).ToList();
            string plateBarcode = "dummy";
            string vendorName =ConfigurationManager.AppSettings["2DBarcodeVendor"] ;
            if(vendorName == "HR") //no plateID
            {
                plateBarcode = sFile.Substring(sFile.LastIndexOf("\\")+1);
                plateBarcode = plateBarcode.Replace(".csv", "");
            }
            else if(vendorName == "WG")
            {
                plateBarcode = GetPlateBarcode4WG(strs);
                strs = strs.Skip(1).ToList();
            }
            else
            {
                throw new Exception("unsupported 2D vendor!");
            }
            //else
            //{
            //    string firstLine = strs[0];
            //    firstLine = firstLine.ToLower();
            //    var indexOfID = firstLine.IndexOf("id:");
            //    if (indexOfID == -1)
            //        throw new Exception("cannot find Plate ID！");
            //    plateBarcode = strs[0].Substring(indexOfID + 3);
            //    if (plateBarcode == "")
            //        throw new Exception(string.Format("Plate ID is empty in file：{0}", sFile));
            //    strs = strs.Skip(1).ToList();
            //}

            int dstBarcodeColumnIndex = GetBarcodeColumnIndex();
            //plateBarcodes.Add(plateBarcode);
            startIndex += labwareSettings.dstLabwareRows * labwareSettings.dstLabwareColumns;
            Dictionary<string, string> barcodesThisPlate = new Dictionary<string, string>();
            int sampleID = 1;
            foreach (var s in strs)
            {
                if (s == "")
                    continue;
                var subStrs = s.Split(',');
                
                string position = Utility.GetDescription(sampleID);
                var barcode = subStrs[dstBarcodeColumnIndex];
                barcode = barcode.Replace("\"", "");
                barcodesThisPlate.Add(position, barcode);
                if(barcode == "")
                {
                    continue; //ignore empty barcodes.
                    //throw new Exception(string.Format("Line :{0} has empty barcode!", barcode));
                }
                if (barcodesThisPlate.Where(x => x.Value == barcode).Count() > 1)
                {
                    var wells = barcodesThisPlate.Where(x => x.Value == barcode).Select(x => x.Key).ToList();
                    throw new Exception(string.Format("Position at {0} and {1}'s barcodes:{2} are duplicated.",
                         wells[0], wells[1], barcode));
                }
                barcode_Position.Add(barcode, position);
                barcode_plateBarcode.Add(barcode, plateBarcode);
                sampleID++;
            }

            int samplesPerRow;
            int buffySlice = pipettingSettings.dstbuffySlice;
            if (buffySlice !=0 && pipettingSettings.buffyStandalone && sFile.ToLower().Contains("buffy"))
            {
                samplesPerRow = Utility.GetSamplesPerRow4Buffy(labwareSettings, pipettingSettings);
                int sampleIndex = 0;
                for (int subRegionIndex = 0; subRegionIndex < samplesPerRow; subRegionIndex++)
                {
                    int startColumn = subRegionIndex * buffySlice;
                    for (int rowIndex = 0; rowIndex < labwareSettings.dstLabwareRows; rowIndex++)
                    {
                        List<Tuple<string, string>> subRegionPosition_Barcodes = new List<Tuple<string, string>>();
                        for (int slice = 0; slice < buffySlice; slice++)
                        {
                            string well = string.Format("{0}{1:D2}", (char)('A' + rowIndex), startColumn + slice + 1);
                       
                            string tmpBarcode = "";
                            if (barcodesThisPlate.ContainsKey(well))
                            {
                                tmpBarcode = barcodesThisPlate[well];
                            }
                            var tuple = Tuple.Create(well, tmpBarcode);
                            if (sampleIndex >= srcTubeCorrespondingBarcodes.Count)
                                break;
                            srcTubeCorrespondingBarcodes[sampleIndex++].Add(tuple);
                        }
                    }
                }

                return;
            }

            int dstBuffySlice = pipettingSettings.buffyStandalone ? 0 : pipettingSettings.dstbuffySlice;
            int totalSliceCnt = dstBuffySlice + pipettingSettings.dstPlasmaSlice + pipettingSettings.dstRedCellSlice;
            samplesPerRow = Utility.GetSamplesPerRow4Plasma(labwareSettings, pipettingSettings,pipettingSettings.buffyStandalone);
            for (int subRegionIndex = 0; subRegionIndex < samplesPerRow; subRegionIndex++)
            {
                int startColumn = subRegionIndex * totalSliceCnt;
                for (int rowIndex = 0; rowIndex < labwareSettings.dstLabwareRows; rowIndex++)
                {
                    List<Tuple<string,string>> subRegionPosition_Barcodes = new List<Tuple<string,string>>();
                    for (int slice = 0; slice < totalSliceCnt; slice++)
                    {
                        string well = string.Format("{0}{1:D2}", (char)('A' + rowIndex), startColumn + slice + 1);
                        //if (!IsValidBarcode(barcodesThisPlate[well]))
                        //    throw new Exception(string.Format("{0}处的条码:{1}非法！", well, barcodesThisPlate[well]));
                        string tmpBarcode = "";
                        if (barcodesThisPlate.ContainsKey(well))
                        {
                            tmpBarcode = barcodesThisPlate[well];
                        }
                        var tuple = Tuple.Create(well, tmpBarcode);
                        subRegionPosition_Barcodes.Add(tuple);
                    }
                    srcTubeCorrespondingBarcodes.Add(subRegionPosition_Barcodes);
                }
            }
        }

        private int GetBarcodeColumnIndex()
        {
            string vendorName = ConfigurationManager.AppSettings["2DBarcodeVendor"];
            Dictionary<string, int> vendor_Index = new Dictionary<string, int>();
            vendor_Index.Add("HR", 1);
            vendor_Index.Add("WG", 2);
            return vendor_Index[vendorName];
        }

        private string GetPlateBarcode4WG(List<string> strs)
        {
            if (strs.Count < 2)
                throw new Exception("Cannot find plate barcode, lines too few!");
            string s = strs[1];
            string[] subStrs = s.Split(',');
            if (subStrs.Length < 4)
                throw new Exception("Cannot find plate barcode, columns too few!");
            return subStrs[3];

        }



        private void SaveAsCSV(List<string> sheetPaths)
        {
            Application app = new Application();
            app.Visible = false;
            app.DisplayAlerts = false;
            foreach (string sheetPath in sheetPaths)
            {

                string sWithoutSuffix = "";
                int pos = sheetPath.IndexOf(".xls");
                if (pos == -1)
                    throw new Exception("Cannot find xls in file name!");
                sWithoutSuffix = sheetPath.Substring(0, pos);
                string sCSVFile = sWithoutSuffix + ".csv";
                if (File.Exists(sCSVFile))
                    continue;
                sCSVFile = sCSVFile.Replace("\\\\", "\\");
                Workbook wbWorkbook = app.Workbooks.Open(sheetPath);
                wbWorkbook.SaveAs(sCSVFile, XlFileFormat.xlCSV);
                wbWorkbook.Close();
                Console.WriteLine(sCSVFile);
            }
            app.Quit();
        }

        public static void SaveAsExcel(string sCSVFile, string sExcelFile)
        {

            Workbooks excelWorkBooks = null;
            Workbook excelWorkBook = null;

            Application app = new Application();
            app.Visible = false;
            app.DisplayAlerts = false;
            excelWorkBooks = app.Workbooks;
            excelWorkBook = ((Workbook)excelWorkBooks.Open(sCSVFile, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value));

            log.InfoFormat("excel path is :{0}", sExcelFile);
            excelWorkBook.SaveAs(sExcelFile, XlFileFormat.xlAddIn8, Missing.Value, Missing.Value, Missing.Value, Missing.Value, XlSaveAsAccessMode.xlNoChange, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);
            excelWorkBook.Close();
            app.Quit();
        }
    }
  
}
