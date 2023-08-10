using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Settings;

namespace Biobanking.ExcelExporter
{
	internal class BeiJingUnivExcelTemplate
	{
		private delegate string Formater(TrackInfo trackInfo);

		internal static void Save2Excel(List<TrackInfo> trackInfos, string sCSV, string sExcel)
		{
			Formater formater = Format4Hospital;
			string text = sCSV.Replace(".csv", "");
			string sCSV2 = text + "hospital.csv";
			string sCSV3 = text + "management.csv";
			string text2 = sExcel.Replace(".xls", "");
			string sExcel2 = text2 + "hospital.xls";
			string sExcel3 = text2 + "management.xls";
			Save2Excel(trackInfos, sCSV2, sExcel2, formater);
			formater = Format4Management;
			Save2Excel(trackInfos, sCSV3, sExcel3, formater);
		}

		private static void Save2Excel(List<TrackInfo> trackInfos, string sCSV, string sExcel, Formater formater)
		{
			List<string> strs = new List<string>();
			string text = "位置信息,患者序号（胎盘库编号）,病人ID（病案号）,样本编号（二维码）,姓名,年龄,孕周,诊断,采集时间,采集人,板号,类型";
			string text2 = "病人ID,检验流水号,样本二维码,坐标,血液类型,分血时间";
			string item = ((formater == new Formater(Format4Hospital)) ? text : text2);
			strs.Add(item);
			trackInfos.ForEach(delegate(TrackInfo x)
			{
				strs.Add(formater(x));
			});
			File.WriteAllLines(sCSV, strs, Encoding.Unicode);
		}

		private static string Format4Management(TrackInfo x)
		{
			return string.Format("{0},{1},{2},{3},{4},{5}", x.sourceBarcode, x.seqNo, x.dstBarcode, x.position, x.description, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
		}

		private static string Format4Hospital(TrackInfo x)
		{
			return $"{x.position},,{x.sourceBarcode},{x.dstBarcode},{x.name},,,,,,{x.plateBarcode}";
		}
	}
}
