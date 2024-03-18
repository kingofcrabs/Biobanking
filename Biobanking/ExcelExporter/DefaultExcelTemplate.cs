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

            string header = "Rack Id,Cavity Id,Position,Sample Id,CONCENTRATION,CONCENTRATIONUNIT,VOLUME,"
                + "USERDEFINED1,USERDEFINED2,USERDEFINED3,USERDEFINED4,USERDEFINED5,PlateErrors,SampleErrors,SAMPLEINSTANCEID,SAMPLEID";
            strs.Add(header);
            trackInfos.ForEach(x => strs.Add(formater(x)));
            File.WriteAllLines(sFile, strs, Encoding.UTF8);
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
            char splitter = ',';
            string descriptionWithSlice = x.description;
            //注意，昆山要加后缀
            //if(!descriptionWithSlice.Contains("Buffy"))
            //{
            //    descriptionWithSlice += $"{x.sliceID}";
            //}
            return $"{x.plateBarcode},{x.dstBarcode},{sWell},{x.sourceBarcode},{CONCENTRATION},{CONCENTRATIONUNIT}," +
                $"{x.volume},{descriptionWithSlice},{x.sliceID},,,,,{x.seqNo},{x.seqNo}";
        }

       
    }
}
