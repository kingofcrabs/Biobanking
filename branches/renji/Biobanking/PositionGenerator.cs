using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using Settings;

namespace Biobanking
{
	internal class PositionGenerator
	{
		private PipettingSettings pipettingSettings;

		private LabwareSettings labwareSettings;

		private int totalSample;

		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public PositionGenerator(PipettingSettings pSettings, LabwareSettings lSettings, int nSample)
		{
			pipettingSettings = pSettings;
			labwareSettings = lSettings;
			totalSample = nSample;
		}

		public static List<POINT> GetWells(int startWellIndex, int wellsCount, int x, int y)
		{
			List<POINT> list = new List<POINT>();
			int num = startWellIndex + wellsCount - 1;
			for (int i = startWellIndex; i <= num; i++)
			{
				int num2 = i / y;
				int num3 = i - num2 * y;
				list.Add(new POINT(1 + num2, num3 + 1));
			}
			return list;
		}

		public static int GetWellID(int colIndex, int rowIndex)
		{
			int num = 8;
			return colIndex * num + rowIndex + 1;
		}

		public static int ParseWellID(string sWellID)
		{
			sWellID = sWellID.Trim();
			if (sWellID.Length > 3)
			{
				throw new Exception("WellID length must <=3!");
			}
			if (sWellID.All((char x) => char.IsDigit(x)))
			{
				return int.Parse(sWellID);
			}
			sWellID = sWellID.ToUpper();
			char c = sWellID.First();
			if (char.IsLetter(c))
			{
				string s = sWellID.Substring(1);
				int rowIndex = c - 65;
				return GetWellID(int.Parse(s) - 1, rowIndex);
			}
			throw new Exception("Invalid WellID, must be digital or string likes A01!");
		}

		internal int AllowedSamples()
		{
			int dstLabwareColumns = labwareSettings.dstLabwareColumns;
			int num = pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice;
			int num2 = dstLabwareColumns / num;
			log.InfoFormat("samples per row is: {0}", num2);
			int num3 = num2 * labwareSettings.dstLabwareRows * labwareSettings.dstCarrierCnt * labwareSettings.sitesPerCarrier;
			if (dstLabwareColumns == 1)
			{
				num3 = labwareSettings.dstLabwareRows * (labwareSettings.dstCarrierCnt / num) * labwareSettings.sitesPerCarrier;
			}
			log.InfoFormat("dispenseAllowed is: {0}", num3);
			int num4 = labwareSettings.sourceLabwareGrids * labwareSettings.sourceWells;
			log.InfoFormat("srcSampleAllowed is: {0}", num4);
			return Math.Min(num3, num4);
		}

		internal List<POINT> GetSrcWells(int startSample, int wellsCount)
		{
			List<POINT> list = new List<POINT>();
			for (int i = startSample + 1; i < startSample + wellsCount + 1; i++)
			{
				list.Add(new POINT(1.0, i));
			}
			return list;
		}

		internal List<POINT> GetDestWellsOneSlicePerRegion(int srcRackIndex, int startSample, int sampleCount)
		{
			int num = srcRackIndex * labwareSettings.sourceWells + startSample;
			List<POINT> list = new List<POINT>();
			for (int i = 0; i < sampleCount; i++)
			{
				int num2 = num + i;
				int num3 = num2 / labwareSettings.dstLabwareRows + 1;
				int num4 = num2 - (num3 - 1) * labwareSettings.dstLabwareRows + 1;
				list.Add(new POINT(num3, num4));
			}
			return list;
		}

		internal List<POINT> GetDestWellsBuffyOnly(int srcRackIndex, int sliceIndex, int startSample, int sampleCount)
		{
			return GetDestWells(srcRackIndex, sliceIndex - pipettingSettings.dstPlasmaSlice, startSample, sampleCount, buffyOnly: true);
		}

		internal List<POINT> GetDestWells(int srcRackIndex, int sliceIndex, int startSample, int sampleCount, bool buffyOnly = false)
		{
			if (pipettingSettings.onlyOneSlicePerLabware)
			{
				return GetDestWellsOneSlicePerRegion(srcRackIndex, startSample, sampleCount);
			}
			int num = srcRackIndex * labwareSettings.sourceWells + startSample;
			int num2 = num + sampleCount - 1;
			int dstLabwareRows = labwareSettings.dstLabwareRows;
			int num3 = ((!buffyOnly) ? pipettingSettings.dstPlasmaSlice : 0);
			int num4 = ((!pipettingSettings.buffyStandalone || buffyOnly) ? pipettingSettings.dstbuffySlice : 0) + num3 + pipettingSettings.dstRedCellSlice;
			int num5 = (buffyOnly ? Utility.GetSamplesPerRow4Buffy(labwareSettings, pipettingSettings) : Utility.GetSamplesPerRow4Plasma(labwareSettings, pipettingSettings, pipettingSettings.buffyStandalone)) * labwareSettings.dstLabwareRows;
			int num6;
			for (num6 = num; num6 >= num5; num6 -= num5)
			{
			}
			int num7 = num6 / labwareSettings.dstLabwareRows;
			int num8 = num4 * num7;
			while (num >= dstLabwareRows)
			{
				num -= dstLabwareRows;
			}
			while (num2 >= dstLabwareRows)
			{
				num2 -= dstLabwareRows;
			}
			int num9 = num + 1;
			int num10 = num2 + 1;
			List<POINT> list = new List<POINT>();
			if (labwareSettings.dstLabwareColumns == 1)
			{
				sliceIndex = 0;
			}
			int num11 = sliceIndex + num8 + 1;
			for (int i = num9; i <= num10; i++)
			{
				list.Add(new POINT(num11, i));
			}
			return list;
		}
	}
}
