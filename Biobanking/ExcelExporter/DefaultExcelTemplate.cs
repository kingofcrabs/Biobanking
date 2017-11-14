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
        internal static void Save2Excel(List<TrackInfo> trackInfos, string sCSVFolder )
        {
            Formater formater = Format4Management;
            Save2Excel(trackInfos, sCSVFolder, formater);
        }

        private static void Save2Excel(List<TrackInfo> trackInfos, string folder, Formater formater)
        {
            var plateBarcodes = trackInfos.Select(x => x.plateBarcode).ToList();
            HashSet<string> uniquePlateBarcodes = new HashSet<string>();
            plateBarcodes.ForEach(x => uniquePlateBarcodes.Add(x));
            List<string> plateFiles = new List<string>();
            foreach(var plateBarcode in uniquePlateBarcodes)
            {
                List<string> strs = new List<string>();
                string header = "病人ID,样本二维码,目标板条码,坐标,血液类型,分血时间,体积";
                strs.Add(header);
                var thisPlateTrackInfo = trackInfos.Where(x => x.plateBarcode == plateBarcode).ToList();
                thisPlateTrackInfo.ForEach(x => strs.Add(formater(x)));
                string sTime = DateTime.Now.ToString("yyyyMMddHHmmss")+"_";
                string file = folder + sTime + plateBarcode + ".csv";
                plateFiles.Add(file);
                File.WriteAllLines(file, strs, Encoding.Unicode);
            }
            string plateNames = Utility.GetOutputFolder() + "plateFiles.txt";
            File.WriteAllLines(plateNames, plateFiles);

        }
        private static string Format4Management(TrackInfo x)
        {
            //病人ID,样本二维码,坐标,血液类型,分血时间,体积
            return string.Format("{0},{1},{2},{3},{4},{5},{6}", x.sourceBarcode,x.dstBarcode,x.plateBarcode, x.position, x.description, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),x.volume);
        }

       
    }
}
