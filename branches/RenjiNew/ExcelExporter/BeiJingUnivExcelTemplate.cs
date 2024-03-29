﻿using Settings;
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
        static int seqNo = 1;
        internal static void Save2Excel(List<TrackInfo> trackInfos, string sCSV, string sExcel)
        {
            Formater formater = Format4Hospital;
            string path = sCSV.Replace(".csv", "");
            string hospital = path + "hospital.csv";
            string management = path + "management.csv";
            string excelPath = sExcel.Replace(".xls", "");
            string excelHospital = excelPath + "hospital.xls";
            string excelManagement = excelPath + "management.xls";
            seqNo = 1;
            Save2Excel(trackInfos, hospital, excelHospital, formater);
            formater = Format4Management;
            Save2Excel(trackInfos, management, excelManagement, formater);
        }

        private static void Save2Excel(List<TrackInfo> trackInfos, string sCSV, string sExcel, Formater formater)
        {
            List<string> strs = new List<string>();
            string hospitalHeader = "位置信息,患者序号（胎盘库编号）,病人ID（病案号）,样本编号（二维码）,姓名,年龄,孕周,诊断,采集时间,采集人,板号,SeqNo,体积";
            string managementHeader = "病人ID,检验流水号,样本二维码,坐标,血液类型,分血时间,体积";
            string header = formater == Format4Hospital ? hospitalHeader : managementHeader;
            strs.Add(header);
            trackInfos = trackInfos.Where(x => x.volume.Trim() != "0").ToList();
            trackInfos.ForEach(x => strs.Add(formater(x)));
            File.WriteAllLines(sCSV, strs,Encoding.Unicode);
            //ExcelReader.SaveAsExcel(sCSV, sExcel);
        }
        private static string Format4Management(TrackInfo x)
        {
            //病人ID,检验流水号,样本二维码,坐标,血液类型,分血时间,体积
            return string.Format("{0},{1},{2},{3},{4},{5},{6}",x.sourceBarcode,x.seqNo,x.dstBarcode,x.position,x.description,DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),x.volume);
            
        }

        private static string Format4Hospital(TrackInfo x)
        {
            return string.Format("{0},,{1},{2},{3},,,,,,{4},{5},{6}", x.position, x.sourceBarcode, x.dstBarcode, x.name,x.plateBarcode,seqNo++,x.volume);
        }
    }
}
