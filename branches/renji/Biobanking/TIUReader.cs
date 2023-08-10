using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace Biobanking
{
	internal class TIUReader : BaseReader
	{
		public override List<DetectedInfo> Read()
		{
			List<DetectedInfo> list = new List<DetectedInfo>();
			string resultFile = GlobalVars.Instance.ResultFile;
			if (!File.Exists(resultFile))
			{
				throw new Exception("Cannot find measure result file!");
			}
			int num = 1;
			string text = ConfigurationManager.AppSettings["UnitIsMM"];
			double num2 = 1.0;
			if (text != null && !bool.Parse(text))
			{
				num2 = 0.1;
			}
			using StreamReader streamReader = new StreamReader(resultFile);
			string text2 = "";
			bool flag = true;
			int num3 = 0;
			while (true)
			{
				text2 = streamReader.ReadLine();
				if (text2 == null)
				{
					break;
				}
				if (text2 == "")
				{
					continue;
				}
				if (flag)
				{
					flag = false;
					continue;
				}
				DetectedInfo detectedInfo = new DetectedInfo();
				string[] array = text2.Split(',');
				detectedInfo.Z1 = double.Parse(array[1]) * num2;
				detectedInfo.Z2 = double.Parse(array[2]) * num2;
				num++;
				list.Add(detectedInfo);
				if (detectedInfo.Z1 < 0.0 || detectedInfo.Z2 < 0.0)
				{
					throw new Exception("Z1,Z2 cannot be smaller than 0 at line: " + num);
				}
				num3++;
			}
			return list;
		}
	}
}
