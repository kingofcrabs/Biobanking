using Microsoft.Office.Interop.Excel;
using Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Biobanking.ExcelExporter
{
    class DefaultExcelTemplate
    {
        delegate string Formater(TrackInfo trackInfo);
        internal static void Save2Files(List<TrackInfo> trackInfos, string sCSVFolder,string excelFolder )
        {
            Formater formater = Format4Management;
            var plateBarcodes = trackInfos.Select(x => x.plateBarcode).ToList();
            HashSet<string> uniquePlateBarcodes = new HashSet<string>();
            plateBarcodes.ForEach(x => uniquePlateBarcodes.Add(x));
            List<string> plateFiles = new List<string>();
            List<string> strs = new List<string>();
            List<string> strsWithTab = new List<string>();
            string header = "Barcode,Sample Source,Sample Type,Volume,Position";
            string tabHeader = "Barcode\tSample Source\tSample Type\tVolume\tPosition\tPlateBarcode";
            strs.Add(header);
            strsWithTab.Add(tabHeader);
            string sTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string file = sCSVFolder + sTime  + ".csv";
            string txtFile = excelFolder + sTime  + ".txt";
            string excelFile = excelFolder + sTime  + ".xls";
            foreach (var plateBarcode in uniquePlateBarcodes)
            {
                var thisPlateTrackInfo = trackInfos.Where(x => x.plateBarcode == plateBarcode).ToList();
                thisPlateTrackInfo.ForEach(x => strs.Add(formater(x)));
                thisPlateTrackInfo.ForEach(x => strsWithTab.Add(Format4ManagementTab(x)));
            }
            File.WriteAllLines(file, strs, Encoding.Unicode);
            File.WriteAllLines(file, strs, Encoding.Unicode);
            File.WriteAllLines(txtFile, strsWithTab, Encoding.Unicode);
            SaveAsExcel(txtFile, excelFile);
        }

        

        public static void SaveAsExcel(string sCSVFile, string sExcelFile)
		{
			Application application = (Application)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("00024500-0000-0000-C000-000000000046")));
			application.Visible = false;
			application.DisplayAlerts = false;
			Workbooks workbooks = application.Workbooks;
			Workbook workbook = workbooks.Open(sCSVFile, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);
			workbook.SaveAs(sExcelFile, XlFileFormat.xlAddIn, Missing.Value, Missing.Value, Missing.Value, Missing.Value, XlSaveAsAccessMode.xlNoChange, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);
			workbook.Close(Missing.Value, Missing.Value, Missing.Value);
			application.Quit();
		}
        private static string Format4Management(TrackInfo x)
        {
            return string.Format("{0},{1},{2},{3},{4}", x.dstBarcode, x.sourceBarcode, x.description,x.volume,  x.position);
        }

        private static string Format4ManagementTab(TrackInfo x)
        {
            return string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", x.dstBarcode, x.sourceBarcode, x.description, x.volume, x.position,x.plateBarcode);
        }

       
    }
}
