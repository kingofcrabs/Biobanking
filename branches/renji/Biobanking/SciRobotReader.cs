using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Biobanking
{
	internal class SciRobotReader : BaseReader
	{
		private bool HasBuffyCoat(string sFile)
		{
			string source = "False";
			using (StreamReader streamReader = new StreamReader(sFile))
			{
				source = streamReader.ReadLine();
				source = streamReader.ReadLine();
			}
			return int.Parse(source.Last().ToString()) == 1;
		}

		public bool HasBuffyCoat()
		{
			string sFile = "C:\\BuffyEx\\data\\Options.csv";
			return HasBuffyCoat(sFile);
		}

		public override List<DetectedInfo> Read()
		{
			SciRobotHelper sciRobotHelper = new SciRobotHelper();
			List<DetectedInfo> heights = new List<DetectedInfo>();
			sciRobotHelper.ReadZValues(ref heights);
			return heights;
		}
	}
}
