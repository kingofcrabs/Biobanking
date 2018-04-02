using Microsoft.Office.Interop.Excel;
using Settings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
        public Dictionary<IDBarcodePair, List<Tuple<int, string>>> ReadBarcodes(LabwareSettings labwareSettings,
            PipettingSettings pipettingSettings,
            Dictionary<string, string> barcode_plateBarcode,
            Dictionary<string, string> barcode_Position, List<string> srcBarcodes,int startWellID)
        {
            startIndex = 0;
            this.labwareSettings = labwareSettings;
            this.pipettingSettings = pipettingSettings;
            string dstBarcodeFolder = GlobalVars.Instance.DstBarcodeFolder;
            DirectoryInfo directoryInfo = new DirectoryInfo(dstBarcodeFolder);
            var allDayDirectorys = from x in directoryInfo.EnumerateDirectories()
                                   where (x.CreationTime.DayOfYear == DateTime.Now.DayOfYear && x.CreationTime.Year == DateTime.Now.Year)
                                   select x;
            if (allDayDirectorys == null || allDayDirectorys.Count() == 0)
            {
                throw new Exception("Cannot find directory of today!");
            }
            DirectoryInfo directoryToday = allDayDirectorys.First<DirectoryInfo>();
            if (directoryToday == null)
            {
                throw new Exception("Cannot find directory of today!");
            }


            var batchesDirectorys = directoryToday.EnumerateDirectories().ToList();
            batchesDirectorys.ForEach(x => Console.WriteLine(x.FullName));

            DirectoryInfo latestDirectory = batchesDirectorys.OrderBy(x => GetNum(x)).Last();
            Console.WriteLine("Latest folder name: {0}", latestDirectory.Name);
            string path = Utility.GetOutputFolder() + "barcodeTime.txt";
            string text = File.ReadAllText(path);
            string text2 = latestDirectory.LastWriteTime.ToString("HHmm");
            if (int.Parse(text2) < int.Parse(text))
            {
                ExcelReader.log.InfoFormat("File time is:{0}, experiment time is:{1}", text2, text);
                throw new Exception("Cannot find file of this experiment!");
            }
            List<FileInfo> excelFiles = latestDirectory.EnumerateFiles("*.xls").ToList();
            excelFiles = excelFiles.OrderBy(x => GetSequence(x)).ToList();
            List<string> csvFiles = new List<string>();
            foreach (FileInfo current in excelFiles)
            {
                string fullName = current.FullName;
                Console.WriteLine(fullName);
                string csvFileName = fullName.Replace("xls", "csv");
                csvFiles.Add(csvFileName);
                if (!File.Exists(csvFileName))
                {
                    this.SaveAsCSV(new List<string>
					{
						fullName
					});
                }
            }
            if (csvFiles.Count < pipettingSettings.dstPlasmaSlice + pipettingSettings.dstbuffySlice)
                throw new Exception(string.Format("Need {0} plates, only {1} can be found!",pipettingSettings.dstPlasmaSlice, pipettingSettings.dstbuffySlice));
            Dictionary<IDBarcodePair, List<Tuple<int, string>>> srcBarcode_eachSlicePosition_barcode = new Dictionary<IDBarcodePair, List<Tuple<int, string>>>();
            csvFiles.ForEach(delegate(string fileName)
            {
                ReadBarcode(srcBarcodes, srcBarcode_eachSlicePosition_barcode, startWellID, barcode_plateBarcode, barcode_Position, fileName);
                //ReadBarcode(eachTubeCorrespondingBarcodes, barcode_plateBarcode, barcode_Position, fileName);
            });
            return srcBarcode_eachSlicePosition_barcode;
        }

        

        private int GetSequence(FileInfo fileInfo)
        {
            string name = fileInfo.Name.Replace(".xls", "");
            string subName = name.Substring(name.Length - 2);
            return int.Parse(subName);
        }

        private int GetNum(DirectoryInfo d)
        {
            return int.Parse(d.Name.Substring(5));
        }

        private void ReadBarcode(List<string> srcBarcodes,
            Dictionary<IDBarcodePair, List<Tuple<int, string>>> srcBarcode_eachSlicePosition_barcode,
            int startWellID,
            Dictionary<string, string> barcode_plateBarcode,
            Dictionary<string, string> barcode_Position,
            string sFile)
        {
            var strs = File.ReadAllLines(sFile).ToList();
            string plateBarcode = "dummy";
            string vendorName = ConfigurationManager.AppSettings["2DBarcodeVendor"] ;
            plateBarcode = GetPlateBarcode(sFile);
            int barcodeColumnIndex = 1;
            startIndex += labwareSettings.dstLabwareRows * labwareSettings.dstLabwareColumns;
            Dictionary<string, string> position_barcode = new Dictionary<string, string>();
            ReadBarcodes(strs, barcode_Position, barcode_plateBarcode,position_barcode, barcodeColumnIndex, plateBarcode);
            int remainWellCnt = 96 - startWellID + 1;
            if (remainWellCnt < srcBarcodes.Count)
                throw new Exception(string.Format("Need {0} wells, only {1} wells can be found!", srcBarcodes.Count, remainWellCnt));

            for (int i = 0; i < srcBarcodes.Count; i++)
            {
                int wellID = startWellID + i;
                string wellDesc = PositionGenerator.GetDesc(wellID);
                string srcBarcode = srcBarcodes[i];
                if (!position_barcode.ContainsKey(wellDesc))
                    throw new Exception(string.Format("Cannot find barcode for well:{0} in plate:{1}", wellDesc, plateBarcode));
                string dstBarcode = position_barcode[wellDesc];
                if (dstBarcode == "" || dstBarcode == "NOREAD" || dstBarcode == "NOTUBE" || dstBarcode.Contains("DECODE"))
                    throw new Exception(string.Format("No valid tube found at well {0} at file :{1}!",wellDesc,sFile));
                var newTuple = new Tuple<int, string>(wellID, dstBarcode);
                IDBarcodePair pair = new IDBarcodePair(i + 1, srcBarcode);
                if (srcBarcode_eachSlicePosition_barcode.ContainsKey(pair))
                    srcBarcode_eachSlicePosition_barcode[pair].Add(newTuple);
                else
                    srcBarcode_eachSlicePosition_barcode.Add(pair, new List<Tuple<int, string>>() { newTuple });
            }
        }

        private string GetPlateBarcode(string sFile)
        {
            int lastIndex = sFile.LastIndexOf("\\");
            sFile = sFile.Substring(lastIndex+1);
            return sFile.Replace(".csv","");
        }

        private void ReadBarcodes(List<string> strs, 
            Dictionary<string, string> barcode_Position, 
            Dictionary<string, string> barcode_plateBarcode,
            Dictionary<string, string> position_barcode,
            int barcodeColumnIndex,
            string plateBarcode)
        {
            int sampleID = 1;
            strs = strs.Skip(1).ToList();
            foreach (var s in strs)
            {
                
                if (s == "")
                    continue;
                var subStrs = s.Split(',');
                string position = subStrs[1];
                var barcode = subStrs[2];
                barcode = barcode.Replace("\"", "");
                position_barcode.Add(position, barcode);
                if (barcode == "" || barcode == "NOREAD" || barcode == "NOTUBE" || barcode.Contains("DECODE") || barcode.Contains("EMPTY"))
                {
                    continue; //ignore empty barcodes.
                }

                if (position_barcode.Where(x => x.Value == barcode).Count() > 1)
                {
                    var wells = position_barcode.Where(x => x.Value == barcode).Select(x => x.Key).ToList();
                    throw new Exception(string.Format("Position at {0} and {1}'s barcodes:{2} are duplicated.",
                         wells[0], wells[1], barcode));
                }
                barcode_Position.Add(barcode, position);
                barcode_plateBarcode.Add(barcode, plateBarcode);
                sampleID++;
            }

           
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
            Marshal.ReleaseComObject(app);
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
