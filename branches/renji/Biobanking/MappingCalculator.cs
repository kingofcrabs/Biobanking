using System.Collections.Generic;
using System.IO;
using System.Linq;
using Settings;

namespace Biobanking
{
	public class MappingCalculator
	{
		private List<CalibrationItem> calibItems;

		public MappingCalculator(string sFile)
		{
			string xml = File.ReadAllText(sFile);
			calibItems = Utility.Deserialize<CalibrationItems>(xml).calibItems;
			calibItems = calibItems.OrderBy((CalibrationItem x) => x.volumeUL).ToList();
		}

		public double GetArea()
		{
			List<CalibrationItem> source = calibItems.Skip(1).ToList();
			CalibrationItem calibrationItem = source.Last();
			CalibrationItem calibrationItem2 = source.First();
			return (double)(calibrationItem.volumeUL - calibrationItem2.volumeUL) / (calibrationItem.height - calibrationItem2.height);
		}

		private void GetVolumeAndTipVolume(double height, ref int volume, ref int tipVolume)
		{
			double num = calibItems.Max((CalibrationItem x) => x.height);
			calibItems.Min((CalibrationItem x) => x.height);
			CalibrationItem calibrationItem = calibItems.Last();
			CalibrationItem calibrationItem2 = calibItems.First();
			if (height < num)
			{
				for (int i = 1; i < calibItems.Count; i++)
				{
					if (height < calibItems[i].height)
					{
						calibrationItem = calibItems[i];
						calibrationItem2 = calibItems[i - 1];
						break;
					}
				}
			}
			else
			{
				calibrationItem = calibItems.Last();
				calibrationItem2 = calibItems[calibItems.Count - 2];
			}
			double num2 = calibrationItem.volumeUL - calibrationItem2.volumeUL;
			double num3 = calibrationItem.height - calibrationItem2.height;
			double num4 = num2 / num3;
			double num5 = (height - calibrationItem2.height) * num4;
			volume = calibrationItem2.volumeUL + (int)num5;
		}

		private void GetTipVolumeAndHegiht(double v, ref int tipVol, ref double height)
		{
			double num = calibItems.Max((CalibrationItem x) => x.volumeUL);
			calibItems.Min((CalibrationItem x) => x.volumeUL);
			CalibrationItem calibrationItem = calibItems.Last();
			CalibrationItem calibrationItem2 = calibItems.First();
			if (v < num)
			{
				for (int i = 1; i < calibItems.Count; i++)
				{
					if (v < (double)calibItems[i].volumeUL)
					{
						calibrationItem = calibItems[i];
						calibrationItem2 = calibItems[i - 1];
						break;
					}
				}
			}
			else
			{
				calibrationItem = calibItems.Last();
				calibrationItem2 = calibItems[calibItems.Count - 2];
			}
			double num2 = calibrationItem.volumeUL - calibrationItem2.volumeUL;
			double num3 = (calibrationItem.height - calibrationItem2.height) / num2;
			double num4 = (v - (double)calibrationItem2.volumeUL) * num3;
			height = num4 + calibrationItem2.height;
		}

		public double GetVolumeFromHeight(double height)
		{
			int volume = 0;
			int tipVolume = 0;
			GetVolumeAndTipVolume(height, ref volume, ref tipVolume);
			return volume;
		}

		public int GetTipVolumeFromHeight(double height)
		{
			int volume = 0;
			int tipVolume = 0;
			GetVolumeAndTipVolume(height, ref volume, ref tipVolume);
			return tipVolume;
		}

		public double GetHeightFromVolume(double v)
		{
			int tipVol = 0;
			double height = 0.0;
			GetTipVolumeAndHegiht(v, ref tipVol, ref height);
			return height;
		}

		public int GetTipVolumeFromVolume(double v)
		{
			int tipVol = 0;
			double height = 0.0;
			GetTipVolumeAndHegiht(v, ref tipVol, ref height);
			return tipVol;
		}
	}
}
