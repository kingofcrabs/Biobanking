using Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biobanking.ExcelExporter
{
    class DefaultExcelTemplate
    {
        delegate string Formater(TrackInfo trackInfo);
        internal static void Save2Excel(List<TrackInfo> trackInfos, string sCSV )
        {
            Formater formater = Format4Management;
            string path = sCSV.Replace(".csv", "");
            path = path + "tracking.csv";
            Save2Excel(trackInfos, path,  formater);
        }

        private static void Save2Excel(List<TrackInfo> trackInfos, string sFile, Formater formater)
        {
            List<string> strs = new List<string>();
            string header = "病人ID,检验流水号,样本二维码,目标板条码,坐标,血液类型,分血时间,体积";
            strs.Add(header);
            trackInfos.ForEach(x => strs.Add(formater(x)));
            File.WriteAllLines(sFile, strs, Encoding.Unicode);
            //ExcelReader.SaveAsExcel(sCSV, sExcel);
        }
        private static string Format4Management(TrackInfo x)
        {
            //病人ID,检验流水号,样本二维码,坐标,血液类型,分血时间,体积
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7}", x.sourceBarcode, x.seqNo, x.dstBarcode,x.plateBarcode, x.position, x.description, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),x.volume);
        }

       
    }
}
