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
        public List<List<string>> ReadBarcodes(LabwareSettings labwareSettings,
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
            List<List<string>> correspondingbarcodes = new List<List<string>>();
            files.ForEach(x => ReadBarcode(correspondingbarcodes, barcode_plateBarcode,barcode_Position, x.FullName));
            return correspondingbarcodes;
        }

        private int GetNum(DirectoryInfo d)
        {
            return int.Parse(d.Name.Substring(5));
        }

        private void ReadBarcode(List<List<string>> barcodesAllSrcTube,
            Dictionary<string, string>barcode_plateBarcode,
            Dictionary<string,string> barcode_Position,
            string sFile)
        {
            var strs = File.ReadAllLines(sFile).ToList();
            string plateBarcode = "dummy";
            if(ConfigurationManager.AppSettings["2DBarcodeVendor"] == "HR") //no plateID
            {
            }
            else
            {
                string firstLine = strs[0];
                firstLine = firstLine.ToLower();
                var indexOfID = firstLine.IndexOf("id:");
                if (indexOfID == -1)
                    throw new Exception("cannot find Plate ID！");
                plateBarcode = strs[0].Substring(indexOfID + 3);
                if (plateBarcode == "")
                    throw new Exception(string.Format("Plate ID is empty in file：{0}", sFile));
                strs = strs.Skip(1).ToList();
            }
            
            //plateBarcodes.Add(plateBarcode);
            startIndex += labwareSettings.dstLabwareRows * labwareSettings.dstLabwareColumns;
            Dictionary<string, string> barcodesThisPlate = new Dictionary<string, string>();
            int sampleID = 1;
            foreach (var s in strs)
            {
                var subStrs = s.Split(',');
                string position = Utility.GetDescription(sampleID);
                var barcode = subStrs[GlobalVars.Instance.FileStruct.dstBarcodeIndex];
                barcode = barcode.Replace("\"", "");
                barcodesThisPlate.Add(position, barcode);
                //if (!IsValidBarcode(barcode))
                //{
                //    throw new Exception(string.Format("Invalid barcode:{0}", barcode));
                //    sampleID++;
                //    continue;
                //}
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
            

            Dictionary<string, List<string>> tubeCorrespondingBarcodes = new Dictionary<string, List<string>>();
            int totalSliceCnt = pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice;
            int samplesPerRow = Utility.GetSamplesPerRow(labwareSettings, pipettingSettings);
            for (int subRegionIndex = 0; subRegionIndex < samplesPerRow; subRegionIndex++)
            {
                int startColumn = subRegionIndex * totalSliceCnt;
                for (int rowIndex = 0; rowIndex < labwareSettings.dstLabwareRows; rowIndex++)
                {
                    List<string> subRegionBarcodes = new List<string>();
                    for (int slice = 0; slice < totalSliceCnt; slice++)
                    {
                        string well = string.Format("{0}{1:D2}", (char)('A' + rowIndex), startColumn + slice + 1);
                        //if (!IsValidBarcode(barcodesThisPlate[well]))
                        //    throw new Exception(string.Format("{0}处的条码:{1}非法！", well, barcodesThisPlate[well]));
                        subRegionBarcodes.Add(barcodesThisPlate[well]);
                    }
                    barcodesAllSrcTube.Add(subRegionBarcodes);
                }
            }
        }

        private bool IsValidBarcode(string barcode)
        {
            foreach(char ch in barcode)
            {
                if(!char.IsDigit(ch))
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
            excelWorkBook.SaveAs(sExcelFile, XlFileFormat.xlAddIn8, Missing.Value, Missing.Value, Missing.Value, Missing.Value, XlSaveAsAccessMode.xlNoChange, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);
            excelWorkBook.Close();
            app.Quit();
        }
    }
  
}
