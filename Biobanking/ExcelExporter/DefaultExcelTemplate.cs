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
            //string header = "样本条码,目标孔条码,目标板条码,坐标,血液类型,分血时间,体积";
            string header = "Rack Id\tCavity Id\tPosition\tSample Id\tCONCENTRATION\tCONCENTRATIONUNIT\tVOLUME"
                + "USERDEFINED1\tUSERDEFINED2\tUSERDEFINED3\tUSERDEFINED4\tUSERDEFINED5\tPlateErrors\tSampleErrors\tSAMPLEINSTANCEID\tSAMPLEID";
            strs.Add(header);
            trackInfos.ForEach(x => strs.Add(formater(x)));
            File.WriteAllLines(sFile, strs, Encoding.Unicode);
            string sFolder = Utility.GetOutputFolder() + "Latest\\";
            if (!Directory.Exists(sFolder))
                Directory.CreateDirectory(sFolder);
            File.Copy(sFile, sFolder + "latestTrackInfo.csv",true);
            //ExcelReader.SaveAsExcel(sCSV, sExcel);
        }
        private static string GetWellDescription(int wellID)
        {
            int colIndex = (wellID - 1) / 8;
            int rowIndex = wellID - colIndex * 8 - 1;
            return $"{(char)('A' + rowIndex)}{(colIndex + 1).ToString("00")}";
        }
        private static string Format4Management(TrackInfo x)
        {
            //Rack Id	Cavity Id	Position	Sample Id	CONCENTRATION	CONCENTRATIONUNIT	VOLUME
            //	USERDEFINED1	USERDEFINED2	USERDEFINED3	USERDEFINED4	USERDEFINED5	PlateErrors	SampleErrors	SAMPLEINSTANCEID	SAMPLEID
            int CONCENTRATION = 1;
            string CONCENTRATIONUNIT = "";
            string sWell = x.position;
            int wellID = -1;
            bool isDigital = int.TryParse(sWell, out wellID);
            if (isDigital)
                sWell = GetWellDescription(wellID);
            return $"{x.plateBarcode}\t{x.dstBarcode}\t{sWell}\t{x.sourceBarcode}\t{CONCENTRATION}\t{CONCENTRATIONUNIT}\t" +
                $"{x.volume}\t{x.description}\t\t\t\t\t\t{x.seqNo}\t{x.seqNo}";
            //return string.Format("{0},{1},{2},{3},{4},{5},{6}", x.sourceBarcode, x.dstBarcode,x.plateBarcode, x.position, x.description, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),x.volume);
        }

       
    }
}
