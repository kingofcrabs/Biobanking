using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Biobanking
{
	internal class PatientInfoReader
	{
		public List<PatientInfo> Read()
		{
			if (GlobalVars.Instance.DstBarcodeFolder == "")
			{
				return null;
			}
			new List<string>();
			List<string> list = File.ReadAllLines(GlobalVars.Instance.SrcBarcodeFile).ToList();
			list.RemoveAll((string x) => x.Trim() == "");
			List<PatientInfo> patientInfos = new List<PatientInfo>();
			list.ForEach(delegate(string x)
			{
				patientInfos.Add(Parse(x));
			});
			return patientInfos;
		}

		private PatientInfo Parse(string content)
		{
			char c = ',';
			if (content.Contains('\t'))
			{
				c = '\t';
			}
			if (content.Contains(c))
			{
				string[] array = content.Split(c);
				if (array.Length == 3)
				{
					return new PatientInfo(array[0], array[1], array[2]);
				}
				if (array.Length == 1)
				{
					return new PatientInfo(array[0]);
				}
				throw new Exception("Invalid patient information format!");
			}
			return new PatientInfo(content, "", "");
		}
	}
}
