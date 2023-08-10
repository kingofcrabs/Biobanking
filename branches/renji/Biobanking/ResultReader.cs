using System.Configuration;

namespace Biobanking
{
	internal class ResultReader
	{
		private static IResultReader impliment;

		public static IResultReader Instance
		{
			get
			{
				if (impliment != null)
				{
					return impliment;
				}
				string text = ConfigurationManager.AppSettings[stringRes.MeasureName];
				if (!(text == "TIU"))
				{
					if (text == "SCIRobotic")
					{
						impliment = new SciRobotReader();
					}
					else
					{
						impliment = new RelaxReader();
					}
				}
				else
				{
					impliment = new TIUReader();
				}
				return impliment;
			}
		}
	}
}
