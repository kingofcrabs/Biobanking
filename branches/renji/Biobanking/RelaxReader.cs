using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;

namespace Biobanking
{
	internal class RelaxReader : BaseReader
	{
		public override List<DetectedInfo> Read()
		{
			string fileName = ConfigurationManager.AppSettings[stringRes.reportPath];
			DataSet dataSet = new DataSet();
			dataSet.ReadXml(fileName);
			DataTable dataTable = dataSet.Tables[0];
			List<DetectedInfo> list = new List<DetectedInfo>();
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			for (int i = 0; i < dataTable.Columns.Count; i++)
			{
				dictionary.Add(i, dataTable.Columns[i].Caption);
			}
			foreach (DataRow row in dataTable.Rows)
			{
				DetectedInfo detectedInfo = new DetectedInfo();
				for (int j = 0; j < row.ItemArray.Count(); j++)
				{
					if (dictionary[j] == "Z1")
					{
						detectedInfo.Z1 = 10.0 * double.Parse(row.ItemArray[j].ToString());
					}
					if (dictionary[j] == "Z2")
					{
						detectedInfo.Z2 = 10.0 * double.Parse(row.ItemArray[j].ToString());
					}
				}
				if (detectedInfo.Z1 < detectedInfo.Z2)
				{
					throw new Exception("Z1 must be greater than Z2");
				}
				list.Add(detectedInfo);
			}
			return list;
		}
	}
}
