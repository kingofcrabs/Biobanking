using Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biobanking.ExcelExporter
{
    class BeiJingUnivExcelTemplate
    {
        delegate string Formater(TrackInfo trackInfo);
        internal static void Save2Excel(List<TrackInfo> trackInfos, string sCSV, string sExcel)
        {
            Formater formater = Format4Hospital;
            string path = sCSV.Replace(".csv", "");
            string hospital = path + "hospital.csv";
            string management = path + "management.csv";
            string excelPath = sExcel.Replace(".xls", "");
            string excelHospital = excelPath + "hospital.xls";
            string excelManagement = excelPath + "management.xls";
            Save2Excel(trackInfos, hospital, excelHospital, formater);
            formater = Format4Management;
            Save2Excel(trackInfos, management, excelManagement, formater);
        }

        private static void Save2Excel(List<TrackInfo> trackInfos, string sCSV, string sExcel, Formater formater)
        {
            List<string> strs = new List<string>();
            string hospitalHeader = "位置信息,患者序号（胎盘库编号）,病人ID（病案号）,样本编号（二维码）,姓名,年龄,孕周,诊断,采集时间,采集人";
            string managementHeader = "病人ID,检验流水号,样本二维码,坐标,血液类型,分血时间";
            string header = formater == Format4Hospital ? hospitalHeader : managementHeader;
            strs.Add(header);
            trackInfos.ForEach(x => strs.Add(formater(x)));
            File.WriteAllLines(sCSV, strs,Encoding.UTF8);
            //ExcelReader.SaveAsExcel(sCSV, sExcel);
        }
        private static string Format4Management(TrackInfo x)
        {
            //病人ID,检验流水号,样本二维码,坐标,血液类型,分血时间
            return string.Format("{0},{1},,{2},{3},{4},{5}",x.sourceBarcode,x.se,x.dstBarcode,x.position,x.description,DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
        }

        private static string Format4Hospital(TrackInfo x)
        {
            return string.Format("{0},,{1},{2},{3},,,,,", x.position, x.sourceBarcode, x.dstBarcode, x.name);
        }
    }
}
