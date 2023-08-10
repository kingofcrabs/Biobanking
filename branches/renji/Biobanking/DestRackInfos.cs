using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;

namespace Biobanking
{
	public class DestRackInfos
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private List<DestRack> rackInfos;

		public List<DestRack> RackInfos => rackInfos;

		public void ReadConfigureFile(string sConfigPath)
		{
			if (!File.Exists(sConfigPath))
			{
				log.Error("Configure file does not exist!");
				return;
			}
			bool flag = true;
			using StreamReader streamReader = new StreamReader(sConfigPath, Encoding.Default);
			string text = "";
			while (true)
			{
				text = streamReader.ReadLine();
				if (text == null)
				{
					break;
				}
				if (flag)
				{
					flag = false;
					continue;
				}
				DestRack item = ExtractRackInfo(text.Split('\t').ToList());
				rackInfos.Add(item);
			}
		}

		private DestRack ExtractRackInfo(List<string> list)
		{
			if (list.Count < 4)
			{
				throw new Exception("配置文件格式错误，配置项不全");
			}
			return new DestRack
			{
				Name = list[0],
				Alignment = (PipettingApproach)Enum.Parse(typeof(PipettingApproach), list[1]),
				Width = int.Parse(list[2]),
				Height = int.Parse(list[3])
			};
		}
	}
}
