using Microsoft.Office.Interop.Excel;
using Settings;
using System;
using System.Collections.Generic;
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
        public List<List<string>> ReadBarcodes(LabwareSettings labwareSettings,PipettingSettings pipettingSettings)
        {
            startIndex = 0;
            this.labwareSettings = labwareSettings;
            this.pipettingSettings = pipettingSettings;
            string sFolder = GlobalVars.Instance.DstBarcodeFolder;
            var di = new DirectoryInfo(sFolder);
            var todayDir = di.EnumerateDirectories().Where(x => x.CreationTime.DayOfYear == DateTime.Now.DayOfYear).First();
            if (todayDir == null)
                throw new Exception("Cannot find directory of today!");
            var lastBatch = todayDir.EnumerateDirectories().OrderBy(d =>GetNum(d)).Last();
            log.InfoFormat("File name: {0}", lastBatch.Name);
            string sTimeFile = Utility.GetOutputFolder() + "barcodeTime.txt";
            var experimentTime = File.ReadAllText(sTimeFile);
            var fileTime = lastBatch.LastWriteTime.ToString("HHmm");
            if(int.Parse(fileTime) < int.Parse(experimentTime))
            {
                log.InfoFormat("File time is:{0}, experiment time is:{1}", fileTime, experimentTime);
                throw new Exception("Cannot find file of this experiment!");
            }
            var files = lastBatch.EnumerateFiles("*.xls").OrderBy(x => x.Name.Substring((int)x.Name.Length - 2)).ToList();
            List<string> csvFiles = new List<string>();
            foreach(var file in files)
            {
                string sFilePath = file.FullName;
                string csvFilePath = sFilePath.Replace("xls", "csv");
                csvFiles.Add(csvFilePath);
                if(!File.Exists(csvFilePath))
                    SaveAsCSV(new List<string>() { sFilePath });
            }
            List<List<string>> correspondingbarcodes = new List<List<string>>();
            csvFiles.ForEach(x=>ReadBarcode(correspondingbarcodes, x));
            return correspondingbarcodes;
        }

        private int GetNum(DirectoryInfo d)
        {
            return int.Parse(d.Name.Substring(5));
        }

        private void ReadBarcode(List<List<string>> barcodesAllSrcTube, string sFile)
        {
            var strs =  File.ReadAllLines(sFile).ToList();
            strs = strs.Skip(1).ToList();
            startIndex += labwareSettings.dstLabwareRows * labwareSettings.dstLabwareColumns;
            Dictionary<string, string> barcodesThisPlate = new Dictionary<string, string>();
            foreach(var s in strs)
            {
                var subStrs = s.Split(',');
                var position = subStrs[GlobalVars.Instance.FileStruct.dstSamplePosition];
                var barcode = subStrs[GlobalVars.Instance.FileStruct.dstBarcodeIndex];
                barcodesThisPlate.Add(position,barcode);
            }
            Dictionary<string, List<string>> tubeCorrespondingBarcodes = new Dictionary<string, List<string>>();
            int totalSliceCnt = pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice;
            int samplesPerRow = Utility.GetSamplesPerRow(labwareSettings, pipettingSettings);
            for(int subRegionIndex = 0; subRegionIndex< samplesPerRow; subRegionIndex++)
            {
                int startColumn = subRegionIndex * totalSliceCnt;
                
                for(int rowIndex = 0; rowIndex < labwareSettings.dstLabwareRows; rowIndex++)
                {
                    List<string> subRegionBarcodes = new List<string>();
                    for (int slice = 0; slice < totalSliceCnt; slice++)
                    {
                        string well = string.Format("{0}{1:D2}", (char)('A' + rowIndex), startColumn + slice + 1);
                        subRegionBarcodes.Add(barcodesThisPlate[well]);
                    }
                    barcodesAllSrcTube.Add(subRegionBarcodes);
                }
                
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
