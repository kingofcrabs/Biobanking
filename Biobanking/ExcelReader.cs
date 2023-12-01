using Microsoft.Office.Interop.Excel;
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
        public List<List<Tuple<string, string>>> ReadBarcodes(LabwareSettings labwareSettings,
            PipettingSettings pipettingSettings,
            Dictionary<string, string> barcode_plateBarcode,
            Dictionary<string, string> barcode_Position)
        {
            startIndex = 0;
            this.labwareSettings = labwareSettings;
            this.pipettingSettings = pipettingSettings;
            string sFolder = GlobalVars.Instance.DstBarcodeFolder;
            var di = new DirectoryInfo(sFolder);
            var files = di.EnumerateFiles("*.csv").ToList();
            files = files.OrderBy(x => x.CreationTime).ToList();
            List<List<Tuple<string, string>>> correspondingbarcodes = new List<List<Tuple<string, string>>>();
            List<string> fileFullNames = files.Select(x => x.FullName).ToList();
            string buffyPlateName = "";
            if(pipettingSettings.onlyOneSlicePerLabware)
            {
                if(fileFullNames.Count < pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice)
                {
                    throw new Exception("Not enough barcode files!");
                }
                //plateBarcode = GetPlateBarcode4NJ(strs);
                //foreach(var fileName in fileFullNames)
                //{
                //    var strs = File.ReadAllLines(fileName).ToList();
                //    plateBarcode = GetPlateBarcode4NJ(strs);
                //}
            }



            bool buffy2Standalone = pipettingSettings.buffyStandalone && pipettingSettings.dstbuffySlice != 0;
            if (buffy2Standalone && !pipettingSettings.onlyOneSlicePerLabware)
            {
                //int cnt = files.Count(x => x.FullName.ToLower().Contains("buffy"));
                //if (cnt == 0)
                //    throw new Exception("No barcode file for buffy plate found!");
                //if (cnt != 1)
                //    throw new Exception("Only one buffy plate supported!");
                buffyPlateName = files.Where(x => x.FullName.ToLower().Contains("buffy")).First().FullName;
                fileFullNames = fileFullNames.Except(new List<string>() { buffyPlateName }).ToList();
            }

            fileFullNames.ForEach(x => ReadBarcode(correspondingbarcodes, barcode_plateBarcode, barcode_Position, x));
            if (buffy2Standalone && !pipettingSettings.onlyOneSlicePerLabware)
            {
                ReadBarcode(correspondingbarcodes, barcode_plateBarcode, barcode_Position, buffyPlateName);
            }
            return correspondingbarcodes;
        }

        private int GetNum(DirectoryInfo d)
        {
            return int.Parse(d.Name.Substring(5));
        }

        private void ReadBarcode(List<List<Tuple<string, string>>> srcTubeCorrespondingBarcodes,
            Dictionary<string, string> barcode_plateBarcode,
            Dictionary<string, string> barcode_Position,
            string sFile)
        {
            var strs = File.ReadAllLines(sFile).ToList();
            string plateBarcode = "dummy";
            string vendorName = ConfigurationManager.AppSettings["2DBarcodeVendor"] ;
            if ( vendorName == "HR") //no plateID
            {
                plateBarcode = sFile.Substring(sFile.LastIndexOf("\\") + 1);
                plateBarcode = plateBarcode.Replace(".csv", "");
            }
            else if( vendorName == "WG")
            {
                plateBarcode = GetPlateBarcode4WG();
            }
            else if( vendorName == "INK")
            {
                plateBarcode = GetPlateBarcode4Ink(strs);
            }
            else if(vendorName == "NJ")
            {
                plateBarcode = GetPlateBarcode4NJ(strs);
            }
            //plateBarcodes.Add(plateBarcode);
            int barcodeColumnIndex = GetBarcodeColumnIndex();
            startIndex += labwareSettings.dstLabwareRows * labwareSettings.dstLabwareColumns;
            Dictionary<string, string> barcodesThisPlate = new Dictionary<string, string>();
            ReadBarcodes(strs, barcode_Position, barcode_plateBarcode,barcodesThisPlate, barcodeColumnIndex, plateBarcode, vendorName);
            int samplesPerRow;
            int buffySlice = pipettingSettings.dstbuffySlice;
            int sampleIndex = 0;
            if (buffySlice != 0 && pipettingSettings.buffyStandalone && sFile.ToLower().Contains("buffy"))
            {
                samplesPerRow = Utility.GetSamplesPerRow4Buffy(labwareSettings, pipettingSettings);
               
                for (int subRegionIndex = 0; subRegionIndex < samplesPerRow; subRegionIndex++)
                {
                    int startColumn = subRegionIndex * buffySlice;
                    for (int rowIndex = 0; rowIndex < labwareSettings.dstLabwareRows; rowIndex++)
                    {
                        List<Tuple<string, string>> subRegionPosition_Barcodes = new List<Tuple<string, string>>();
                        for (int slice = 0; slice < buffySlice; slice++)
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
                            if (sampleIndex >= srcTubeCorrespondingBarcodes.Count)
                                break;
                            //subRegionPosition_Barcodes.Add(tuple);
                            srcTubeCorrespondingBarcodes[sampleIndex++].Add(tuple);
                        }
                        //barcodesAllSrcTube.Add(subRegionPosition_Barcodes);

                    }
                }

                return;
            }

            int dstBuffySlice = pipettingSettings.buffyStandalone ? 0 : pipettingSettings.dstbuffySlice;
            int totalSliceCnt = dstBuffySlice + pipettingSettings.dstPlasmaSlice;
            if (pipettingSettings.onlyOneSlicePerLabware)
                totalSliceCnt = 1;
            samplesPerRow = Utility.GetSamplesPerRow(labwareSettings, pipettingSettings, pipettingSettings.buffyStandalone);
            sampleIndex = 0;
            for (int subRegionIndex = 0; subRegionIndex < samplesPerRow; subRegionIndex++)
            {
                int startColumn = subRegionIndex * totalSliceCnt;
                for (int rowIndex = 0; rowIndex < labwareSettings.dstLabwareRows; rowIndex++)
                {
                    List<Tuple<string, string>> subRegionPosition_Barcodes = new List<Tuple<string, string>>();
                    for (int slice = 0; slice < totalSliceCnt; slice++)
                    {
                        int wellID = Utility.GetWellID(rowIndex, startColumn + slice, labwareSettings);
                        wellID += pipettingSettings.startWell - 1;
                        string well = Utility.GetWellDescription(wellID,labwareSettings);
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
                    if(pipettingSettings.onlyOneSlicePerLabware)
                    {
                        if (srcTubeCorrespondingBarcodes.Count < 96)
                            srcTubeCorrespondingBarcodes.Add(subRegionPosition_Barcodes);
                        else
                            srcTubeCorrespondingBarcodes[sampleIndex++].Add(subRegionPosition_Barcodes[0]);
                    }
                   else
                    {
                        srcTubeCorrespondingBarcodes.Add(subRegionPosition_Barcodes);
                    }
                }
            }
        }

        private string GetPlateBarcode4NJ(List<string> strs)
        {
            return GetPlateBarcode4Ink(strs);
        }

        private string GetPlateBarcode4Ink(List<string> strs)
        {
            return strs[1].Replace("rackID=", "");
        }

        private void ReadBarcodes(List<string> strs, 
            Dictionary<string, string> barcode_Position, 
            Dictionary<string, string> barcode_plateBarcode,
            Dictionary<string, string> barcodesThisPlate,
            int barcodeColumnIndex,
            string plateBarcode,
            string vendorName)
        {
            switch(vendorName)
            {
                case "HR":
                    ReadBarcode4HR(strs, barcode_Position, barcode_plateBarcode, barcodesThisPlate, plateBarcode, barcodeColumnIndex);
                    break;
                case "NJ":
                case "WG":
                    ReadBarcode4WG(strs, barcode_Position, barcode_plateBarcode, barcodesThisPlate, plateBarcode, barcodeColumnIndex);
                    break;
                case "INK":
                    ReadBarcode4INK(strs, barcode_Position, barcode_plateBarcode, barcodesThisPlate, plateBarcode, barcodeColumnIndex);
                    break;
            }
            
        }

        private void ReadBarcode4INK(List<string> strs,
            Dictionary<string, string> barcode_Position,
            Dictionary<string, string> barcode_plateBarcode, 
            Dictionary<string, string> barcodesThisPlate, 
            string plateBarcode,
            int barcodeColumnIndex)
        {
            int sampleID = 1;
            strs = strs.Skip(2).ToList();
            foreach (var s in strs)
            {
                if (s == "")
                    continue;
                var subStrs = s.Split(',');
                int val = 0;
                //A1 A2
                val = ParseWell(subStrs[0]);
                //if (!int.TryParse(subStrs[0], out val))
                //    continue;

                string position = Utility.GetDescription(sampleID);
                var barcode = subStrs[1];
                barcode = barcode.Replace("\"", "");
                barcodesThisPlate.Add(position, barcode);
                if (barcode == "NoData" || barcode == "error")
                {
                    continue; //ignore empty barcodes.
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
        }

        private void ReadBarcode4WG(List<string> strs,
            Dictionary<string, string> barcode_Position, 
            Dictionary<string, string> barcode_plateBarcode, 
            Dictionary<string, string> barcodesThisPlate, 
            string plateBarcode, 
            int barcodeColumnIndex)
        {
            foreach (var s in strs)
            {
                if (s == "")
                    continue;
                var subStrs = s.Split(',');
         
                if (subStrs.Count() <= 1)
                    continue;
               

                string position = subStrs[1];
                if (position.ToLower().Contains("tube"))
                    continue;

                int val = 0;
                try
                {
                    bool bok = int.TryParse(position, out val);
                    if(!bok)
                        val = ParseWell(position);
                    if (val > 96 || val < 1)
                        continue;
                }
                catch(Exception ex)
                {
                    continue;
                }


                var barcode = subStrs[barcodeColumnIndex];
                barcode = barcode.Replace("\"", "");
                
                if (barcode == "" || barcode == "NoData")
                {
                    continue; //ignore empty barcodes.
                }
                string formatPosition = Utility.GetWellDescription(val,labwareSettings);
                barcodesThisPlate.Add(formatPosition, barcode);
                if (barcodesThisPlate.Where(x => x.Value == barcode).Count() > 1)
                {
                    var wells = barcodesThisPlate.Where(x => x.Value == barcode).Select(x => x.Key).ToList();
                    throw new Exception(string.Format("Position at {0} and {1}'s barcodes:{2} are duplicated.",
                         wells[0], wells[1], barcode));
                }
                if(!barcode.Contains("NoData"))
                {
                    barcode_Position.Add(barcode, position);
                    barcode_plateBarcode.Add(barcode, plateBarcode);
                }
                   
            }
        }

     

        private void ReadBarcode4HR(List<string> strs,
            Dictionary<string, string> barcode_Position, 
            Dictionary<string, string> barcode_plateBarcode,
            Dictionary<string, string> barcodesThisPlate,
            string plateBarcode,
            int barcodeColumnIndex)
        {
            int sampleID = 1;
            foreach (var s in strs)
            {
                if (s == "")
                    continue;
                var subStrs = s.Split(',');
                int wellID = ParseWell(subStrs[0]);
                string position = Utility.GetDescription(wellID);
                var barcode = subStrs[barcodeColumnIndex];
                barcode = barcode.Replace("\"", "");
                barcodesThisPlate.Add(position, barcode);
                if (barcode == "")
                {
                    sampleID++;
                    continue; //ignore empty barcodes.
                }
                if (barcodesThisPlate.Where(x => x.Value == barcode).Count() > 1)
                {
                    if (barcode == "")
                    {
                        sampleID++;
                        continue; //ignore empty barcodes.
                    }
                    var wells = barcodesThisPlate.Where(x => x.Value == barcode).Select(x => x.Key).ToList();
                    throw new Exception(string.Format("Position at {0} and {1}'s barcodes:{2} are duplicated.",
                         wells[0], wells[1], barcode));
                }
                barcode_Position.Add(barcode, position);
                barcode_plateBarcode.Add(barcode, plateBarcode);
                sampleID++;
            }
        }

        private int ParseWell(string s)
        {
            int row = s.First() - 'A';
            int column = int.Parse(s.Substring(1));
            return (column -1) * 8 + row + 1;
        }

        private int GetBarcodeColumnIndex()
        {
            string vendorName = ConfigurationManager.AppSettings["2DBarcodeVendor"];
            Dictionary<string, int> vendor_Index = new Dictionary<string, int>();
            vendor_Index.Add("HR", 1);
            vendor_Index.Add("NJ", 2);
            vendor_Index.Add("WG", 2);
            vendor_Index.Add("INK", 1);
            return vendor_Index[vendorName];
        }

        private string GetPlateBarcode4WG()
        {
            return "WG" + DateTime.Now.Second.ToString() ;
            //throw new NotImplementedException();
        }

        private string GetPosition(List<string> strs)
        {
            List<string> newStrs = new List<string>();
            strs.ForEach(x => newStrs.Add(x.Replace("\"", "")));
            if (GlobalVars.Instance.Barcode2DVendor.ToLower() == "baiquan")
            {
                int col = int.Parse(newStrs[1]);
                int rowIndex = newStrs[2][0] - 'A';
                int wellID = PositionGenerator.GetWellID(col - 1, rowIndex);
                return Utility.GetDescription(wellID);
            }
            else
            {
                string position = newStrs[GlobalVars.Instance.FileStruct.dstPosition];
                int wellID = PositionGenerator.ParseWellID(position);
                return Utility.GetDescription(wellID);
            }
        }



        private bool IsValidBarcode(string barcode)
        {
            foreach (char ch in barcode)
            {
                if (!char.IsDigit(ch))
                {
                    return false;
                }
            }
            return true;
        }



        //private string GetDescription(int sampleID)
        //{
        //    int sampleIndex = sampleID - 1;
        //    int colIndex = sampleIndex / 8;
        //    int rowIndex = sampleIndex - colIndex * 8;
        //    return string.Format("{0}{1:D2}", (char)('A' + rowIndex), colIndex + 1);
        //}

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
            excelWorkBook.SaveAs(sExcelFile, XlFileFormat.xlAddIn, Missing.Value, Missing.Value, Missing.Value, Missing.Value, XlSaveAsAccessMode.xlNoChange, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);
            excelWorkBook.Close();
            app.Quit();
        }
    }

}
