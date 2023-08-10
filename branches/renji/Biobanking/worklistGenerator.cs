using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;
using Settings;

namespace Biobanking
{
	internal class worklistGenerator : worklistCommand
	{
		private List<int> destPlasmaPos = new List<int>();

		private PositionGenerator positionGenerator;

		private MappingCalculator mappingCalculator;

		private BarcodeTracker barcodeTracker;

		private const double extraBuffy = 24.0;

		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const int maxSourceCountOneRack = 10;

		private List<DetectedInfo> detectInfos;

		private List<PatientInfo> patientInfos;

		public bool DoJob()
		{
			detectInfos = ResultReader.Instance.Read();
			patientInfos = ResultReader.Instance.ReadPatientInfos();
			if (GlobalVars.Instance.TrackBarcode)
			{
				patientInfos = patientInfos.Take(detectInfos.Count).ToList();
				barcodeTracker = new BarcodeTracker(pipettingSettings, labwareSettings, patientInfos);
			}
			log.Info("read heights");
			mappingCalculator = new MappingCalculator(Utility.GetExeFolder() + Settings.stringRes.calibFileName);
			positionGenerator = new PositionGenerator(pipettingSettings, labwareSettings, detectInfos.Count);
			int num = positionGenerator.AllowedSamples();
			if (num < detectInfos.Count)
			{
				throw new Exception($"max allowed sample is: {num}");
			}
			string outputFolder = Utility.GetOutputFolder();
			int num2 = (int)Math.Ceiling((double)detectInfos.Count / (double)labwareSettings.sourceWells);
			WriteRacksCount(num2);
			string text = outputFolder;
			RunResult runResult = new RunResult();
			AdjustZ1According2PlasmaRatio(detectInfos);
			for (int i = 0; i < num2; i++)
			{
				outputFolder = text + "\\srcRack" + (i + 1) + "\\";
				if (!Directory.Exists(outputFolder))
				{
					Directory.CreateDirectory(outputFolder);
				}
				int num3 = ((i == num2 - 1) ? (detectInfos.Count - i * labwareSettings.sourceWells) : labwareSettings.sourceWells);
				Utility.Write2File(outputFolder + "totalSample.txt", num3.ToString());
				Utility.Write2File(s: ((int)Math.Ceiling((double)num3 / (double)labwareSettings.tipCount)).ToString(), fileName: outputFolder + "batchCount.txt");
				for (int j = 0; j < num3; j += labwareSettings.tipCount)
				{
					List<DetectedInfo> list = new List<DetectedInfo>();
					for (int k = 0; k < labwareSettings.tipCount && k + j < num3; k++)
					{
						list.Add(detectInfos[i * labwareSettings.sourceWells + j + k]);
					}
					GenerateForBatch(outputFolder, i, j, list);
					AddEachSampleInfo2RunResult(i, j, list, runResult);
				}
			}
			if (GlobalVars.Instance.TrackBarcode)
			{
				barcodeTracker.WriteResult();
			}
			AddCommonInfo2RunResult(runResult);
			SaveRunResult(runResult);
			return true;
		}

		private void AdjustZ1According2PlasmaRatio(List<DetectedInfo> detectInfos)
		{
			for (int i = 0; i < detectInfos.Count; i++)
			{
				double z = detectInfos[i].Z2;
				double z2 = detectInfos[i].Z1;
				double num = z2 - z;
				z2 = z + num * pipettingSettings.plasmaRatio;
				detectInfos[i].Z1 = z2;
			}
		}

		private void GenerateForBatch(string sOutput, int rackIndex, int sampleIndexInRack, List<DetectedInfo> heightsThisTime)
		{
			bool flag = NeedUseLastFour(sampleIndexInRack);
			log.InfoFormat("rack index : {0}, start sample : {1}", rackIndex, sampleIndexInRack);
			int num = 1 + sampleIndexInRack / labwareSettings.tipCount;
			int tipOffSet = GetTipOffSet(flag);
			int num2 = 0;
			for (int i = tipOffSet; i < tipOffSet + heightsThisTime.Count; i++)
			{
				num2 += (int)Math.Pow(2.0, i);
			}
			string arg = ".gwl";
			string path = sOutput + $"\\worklist{num}{arg}";
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			StreamWriter streamWriter = new StreamWriter(new FileStream(path, FileMode.CreateNew), Encoding.Default);
			streamWriter.WriteLine("W;");
			string exeFolder = Utility.GetExeFolder();
			WriteComment($"batch id is: {num}", streamWriter);
			streamWriter.WriteLine(string.Format(breakPrefix + "GetDiti2({0},\"DiTi 1000ul LiHa\",0,0,10,70);", num2));
			mappingCalculator.GetArea();
			List<POINT> srcWells = positionGenerator.GetSrcWells(sampleIndexInRack, heightsThisTime.Count);
			int dstPlasmaSlice = pipettingSettings.dstPlasmaSlice;
			List<double> list = new List<double>();
			for (int j = 0; j < dstPlasmaSlice; j++)
			{
				string arg2 = exeFolder + $"Notifier.exe Pipetting;{rackIndex};{num - 1};{j}";
				if (exeFolder != "")
				{
					streamWriter.WriteLine(string.Format(breakPrefix + "Execute(\"{0}\",2,\"\",2);", arg2));
				}
				WriteComment($"Processing slice: {j + 1}, plasma part", streamWriter);
				list = GenerateForSlice(j, dstPlasmaSlice, srcWells, rackIndex, sampleIndexInRack, heightsThisTime, streamWriter);
				if (GlobalVars.Instance.TrackBarcode)
				{
					barcodeTracker.Track(list, j, GlobalVars.Instance.BloodDescription);
				}
			}
			bool num3 = pipettingSettings.dstbuffySlice > 0;
			bool flag2 = IsDstWellsInSameColumn(rackIndex, sampleIndexInRack, srcWells.Count);
			GetGlobalSampleIndex(rackIndex, sampleIndexInRack);
			if (num3)
			{
				string arg3 = exeFolder + $"Notifier.exe Pipetting;{rackIndex};{num - 1};{pipettingSettings.dstPlasmaSlice}";
				if (exeFolder != "")
				{
					streamWriter.WriteLine(string.Format(breakPrefix + "Execute(\"{0}\",2,\"\",2);", arg3));
				}
				List<double> list2 = new List<double>();
				if (flag)
				{
					list2.AddRange(new List<double> { 0.0, 0.0, 0.0, 0.0 });
					POINT item = new POINT(0.0, 0.0);
					srcWells.InsertRange(0, new List<POINT> { item, item, item, item });
				}
				for (int k = 0; k < heightsThisTime.Count; k++)
				{
					list2.Add(24.0);
				}
				int srcGrid = GetSrcGrid(rackIndex);
				string value = GenerateAspirateCommand(srcWells, list2, "BB_Buffy", srcGrid, 0, labwareSettings.sourceWells);
				streamWriter.WriteLine(value);
				WriteMSDCommands(streamWriter, heightsThisTime, tipOffSet);
				if (flag2)
				{
					WriteDispenseBuffy(rackIndex, sampleIndexInRack, heightsThisTime.Count, flag, streamWriter);
				}
				else
				{
					int endIndexForFirstColumn = GetEndIndexForFirstColumn(rackIndex, sampleIndexInRack);
					int num4 = endIndexForFirstColumn - rackIndex * labwareSettings.sourceWells - sampleIndexInRack + 1;
					WriteDispenseBuffy(rackIndex, sampleIndexInRack, num4, flag, streamWriter);
					int sampleIndexThisRack = endIndexForFirstColumn + 1;
					WriteDispenseBuffy(rackIndex, sampleIndexThisRack, heightsThisTime.Count - num4, flag, streamWriter);
				}
			}
			if (pipettingSettings.dstRedCellSlice > 0)
			{
				if (pipettingSettings.dstbuffySlice > 0)
				{
					streamWriter.WriteLine(string.Format(breakPrefix + "DropDiti({0},{1},2,10,70,0);", num2, labwareSettings.wasteGrid));
					streamWriter.WriteLine(string.Format(breakPrefix + "GetDiti2({0},\"DiTi 1000ul LiHa\",0,0,10,70);", num2));
				}
				WriteComment("Discard extra plasma.", streamWriter);
				DiscardExtraPlasma(srcWells, heightsThisTime, rackIndex, sampleIndexInRack, streamWriter);
			}
			int dstRedCellSlice = pipettingSettings.dstRedCellSlice;
			List<double> redCellVols = new List<double>();
			heightsThisTime.ForEach(delegate(DetectedInfo x)
			{
				redCellVols.Add(Math.Max(0, (int)(mappingCalculator.GetVolumeFromHeight(x.Z2) - 50.0)));
			});
			int num5 = ((!pipettingSettings.buffyStandalone) ? pipettingSettings.dstbuffySlice : 0);
			int num6 = pipettingSettings.dstPlasmaSlice + num5;
			for (int l = 0; l < dstRedCellSlice; l++)
			{
				WriteComment($"Processing slice: {l + 1}, red cell part", streamWriter);
				List<double> redCellVol = GetRedCellVol(redCellVols, l);
				ProcessSliceOnce(srcWells, redCellVol, "BB_RedCell", rackIndex, l + num6, sampleIndexInRack, streamWriter, redCellVols);
				if (GlobalVars.Instance.TrackBarcode)
				{
					barcodeTracker.Track(redCellVol, l + num6, "redCell");
				}
			}
			streamWriter.WriteLine(string.Format(breakPrefix + "DropDiti({0},{1},2,10,70,0);", num2, labwareSettings.wasteGrid));
			if (rackIndex * 16 + sampleIndexInRack + heightsThisTime.Count >= detectInfos.Count)
			{
				string arg4 = exeFolder + $"Notifier.exe Pipetting;true";
				streamWriter.WriteLine(string.Format(breakPrefix + "Execute(\"{0}\",2,\"\",2);", arg4));
			}
			streamWriter.Close();
		}

		private List<double> GetRedCellVol(List<double> redCellVols, int sliceIndex)
		{
			int num = sliceIndex + 1;
			double num2 = (double)num * pipettingSettings.redCellGreedyVolume;
			double num3 = (double)(num - 1) * pipettingSettings.redCellGreedyVolume;
			List<double> list = new List<double>();
			foreach (double redCellVol in redCellVols)
			{
				if (redCellVol > num2)
				{
					list.Add(pipettingSettings.redCellGreedyVolume);
				}
				else if (redCellVol > num3)
				{
					list.Add(redCellVol - num3);
				}
				else
				{
					list.Add(0.0);
				}
			}
			return list;
		}

		private void DiscardExtraPlasma(List<POINT> ptsAsp, List<DetectedInfo> heightsThisTime, int rackIndex, int sampleIndexInRack, StreamWriter sw)
		{
			if (pipettingSettings.plasmaGreedyVolume == 0.0)
			{
				return;
			}
			List<int> vols = new List<int>();
			double pipettedVolume = pipettingSettings.plasmaGreedyVolume * (double)pipettingSettings.dstPlasmaSlice;
			heightsThisTime.ForEach(delegate(DetectedInfo x)
			{
				vols.Add(Math.Max(0, (int)(mappingCalculator.GetVolumeFromHeight(x.Z1) - mappingCalculator.GetVolumeFromHeight(x.Z2) - pipettedVolume)));
			});
			int slices = (int)Math.Ceiling((double)vols.Max() / 850.0);
			if (slices != 0)
			{
				List<double> volsEachBatch = new List<double>();
				vols.ForEach(delegate(int x)
				{
					volsEachBatch.Add(x / slices);
				});
				int srcGrid = GetSrcGrid(rackIndex);
				List<POINT> list = new List<POINT>();
				for (int i = 0; i < vols.Count; i++)
				{
					list.Add(new POINT(1.0, i + 1));
				}
				for (int j = 0; j < slices; j++)
				{
					string text = "";
					text = GenerateAspirateCommand(ptsAsp, volsEachBatch, "BB_Plasma_Medium", srcGrid, 0, labwareSettings.sourceWells);
					sw.WriteLine(text);
					text = GenerateDispenseCommand(list, volsEachBatch, "BB_Plasma_Medium", labwareSettings.wasteTroughGrid, 0, 1, 8);
					sw.WriteLine(text);
				}
			}
		}

		private int GetTipOffSet(bool bNeedUseLastFour)
		{
			if (!bNeedUseLastFour)
			{
				return 0;
			}
			return 4;
		}

		private bool NeedUseLastFour(int startSample)
		{
			return false;
		}

		private List<double> GenerateForSlice(int slice, int totalSlice, List<POINT> ptsAsp, int srcRackIndex, int sampleIndexInRack, List<DetectedInfo> heightsThisTime, StreamWriter sw, bool isRedCell = false)
		{
			List<double> list = new List<double>();
			List<List<double>> list2 = new List<List<double>>();
			List<List<List<string>>> list3 = new List<List<List<string>>>();
			List<List<double>> list4 = new List<List<double>>();
			for (int i = 0; i < 10; i++)
			{
				list2.Add(new List<double>());
			}
			for (int j = 0; j < 10; j++)
			{
				list3.Add(new List<List<string>>());
				list4.Add(new List<double>());
			}
			int num = 850;
			int maxVolumePerSlice = pipettingSettings.maxVolumePerSlice;
			bool bNeedUseLastFour = NeedUseLastFour(sampleIndexInRack);
			int tipOffSet = GetTipOffSet(bNeedUseLastFour);
			for (int k = 0; k < heightsThisTime.Count; k++)
			{
				double z = heightsThisTime[k].Z1;
				double z2 = heightsThisTime[k].Z2;
				double v = CalcuAspiratePositionVolume(slice, totalSlice, z, z2);
				double num2 = CalculateAspirateVolume(slice, totalSlice, z, z2);
				list.Add(num2);
				num2 = Math.Min(maxVolumePerSlice, num2);
				num2 = (int)num2;
				for (int l = 0; l < 10; l++)
				{
					if (num2 < (double)num)
					{
						double item = num2;
						list2[l].Add(item);
						num2 = 0.0;
					}
					else
					{
						num2 -= (double)num;
						if (num2 < 15.0)
						{
							list2[l].Add((double)num + num2);
							num2 = 0.0;
						}
						else
						{
							list2[l].Add(num);
						}
					}
					double volume2Set = mappingCalculator.GetTipVolumeFromVolume(v);
					list3[l].Add(GetSetVolString(k + tipOffSet, volume2Set));
					list4[l].Add(mappingCalculator.GetHeightFromVolume(v));
				}
			}
			for (int m = 0; m < list2.Count; m++)
			{
				string liquidClass = "BB_Plasma_Fast";
				List<double> list5 = list2[m];
				if (list5.Sum() == 0.0)
				{
					continue;
				}
				List<double> heights = list4[m];
				WriteComment($"aspirate times : {m + 1}", sw);
				double smallestDiff = GetSmallestDiff(list5, heights, heightsThisTime);
				if (smallestDiff < 10.0)
				{
					liquidClass = "BB_Plasma_Medium";
					if (smallestDiff < 5.0)
					{
						liquidClass = "BB_Plasma_Slow";
					}
				}
				ProcessSliceOnce(ptsAsp, list5, liquidClass, srcRackIndex, slice, sampleIndexInRack, sw);
			}
			return list;
		}

		private double GetSmallestDiff(List<double> volumes, List<double> heights, List<DetectedInfo> heightsThisTime)
		{
			double num = 999.0;
			for (int i = 0; i < heightsThisTime.Count; i++)
			{
				if (volumes[i] != 0.0)
				{
					double num2 = heights[i] - heightsThisTime[i].Z2;
					if (num2 < num)
					{
						num = num2;
					}
				}
			}
			return num;
		}

		private double CalculateAspirateVolume(int curSlice, int totalSlice, double z1, double z2)
		{
			double area = mappingCalculator.GetArea();
			double num = 0.0;
			bool num2 = pipettingSettings.plasmaGreedyVolume != 0.0;
			double num3 = mappingCalculator.GetVolumeFromHeight(z1) - mappingCalculator.GetVolumeFromHeight(z2) - (double)pipettingSettings.safeDelta * area;
			if (num2)
			{
				double plasmaGreedyVolume = pipettingSettings.plasmaGreedyVolume;
				double num4 = (double)(curSlice + 1) * plasmaGreedyVolume;
				double num5 = (double)curSlice * plasmaGreedyVolume;
				if (num5 >= num3)
				{
					num = 0.0;
				}
				else
				{
					num = ((num4 > num3) ? (num3 - num5) : plasmaGreedyVolume);
					if (pipettingSettings.giveUpNotEnough && num < plasmaGreedyVolume)
					{
						num = 0.0;
					}
					if (num < pipettingSettings.minVolumeAsp)
					{
						num = 0.0;
					}
				}
				if (num4 < num3 && curSlice == totalSlice - 1)
				{
					log.Debug("cannot aspirate all the plasma within specified slices & greedy approach.");
				}
			}
			else
			{
				num = num3 / (double)totalSlice;
			}
			if (num < 0.0)
			{
				num = 0.0;
			}
			return num;
		}

		private double CalcuAspiratePositionVolume(int curSlice, int totalSlice, double z1, double z2)
		{
			double area = mappingCalculator.GetArea();
			double num = mappingCalculator.GetVolumeFromHeight(z1) - mappingCalculator.GetVolumeFromHeight(z2) - (double)pipettingSettings.safeDelta * area;
			double result = z2 + (double)pipettingSettings.safeDelta;
			double num2;
			if (pipettingSettings.plasmaGreedyVolume != 0.0)
			{
				num2 = (double)(curSlice + 1) * pipettingSettings.plasmaGreedyVolume;
				if (num2 > num)
				{
					return result;
				}
			}
			else
			{
				num2 = (double)(curSlice + 1) * num / (double)totalSlice;
			}
			return mappingCalculator.GetVolumeFromHeight(z1) - num2;
		}

		private int GetSrcGrid(int rackIndex)
		{
			return rackIndex + labwareSettings.sourceLabwareStartGrid;
		}

		private void ProcessSliceOnce(List<POINT> ptsAspOrg, List<double> volumes, string liquidClass, int srcRackIndex, int sliceIndex, int sampleIndexInRack, StreamWriter sw, List<double> redCellVols = null)
		{
			bool num = IsDstWellsInSameColumn(srcRackIndex, sampleIndexInRack, ptsAspOrg.Count);
			int srcGrid = GetSrcGrid(srcRackIndex);
			int globalSampleIndex = GetGlobalSampleIndex(srcRackIndex, sampleIndexInRack);
			bool flag = NeedUseLastFour(sampleIndexInRack);
			List<POINT> list = new List<POINT>(ptsAspOrg);
			string value = GenerateAspirateCommand(list, volumes, liquidClass, srcGrid, 0, labwareSettings.sourceWells);
			sw.WriteLine(value);
			if (num)
			{
				List<POINT> destWells = positionGenerator.GetDestWells(srcRackIndex, sliceIndex, sampleIndexInRack, ptsAspOrg.Count);
				int grid = 0;
				int site = 0;
				CalculateDestGridAndSite(globalSampleIndex, sliceIndex, ref grid, ref site);
				if (flag)
				{
					POINT item = new POINT(0.0, 0.0);
					destWells.InsertRange(0, new List<POINT> { item, item, item, item });
				}
				string value2 = GenerateDispenseCommand(destWells, volumes, liquidClass, grid, site, labwareSettings.dstLabwareColumns, labwareSettings.dstLabwareRows);
				sw.WriteLine(value2);
			}
			else
			{
				int num2 = GetEndIndexForFirstColumn(srcRackIndex, sampleIndexInRack) - srcRackIndex * labwareSettings.sourceWells - sampleIndexInRack + 1;
				List<POINT> destWells2 = positionGenerator.GetDestWells(srcRackIndex, sliceIndex, sampleIndexInRack, num2);
				int grid2 = 0;
				int site2 = 0;
				CalculateDestGridAndSite4OneSlicePerLabware(sliceIndex, ref grid2, ref site2);
				SplitVolumes2Region(volumes, out var volumes2, out var volumes3, num2);
				string value3 = GenerateDispenseCommand(destWells2, volumes2, liquidClass, grid2, site2, labwareSettings.dstLabwareColumns, labwareSettings.dstLabwareRows);
				sw.WriteLine(value3);
				destWells2 = positionGenerator.GetDestWells(srcRackIndex, sliceIndex, sampleIndexInRack + num2, list.Count - num2);
				value3 = GenerateDispenseCommand(destWells2, volumes3, liquidClass, grid2, site2, labwareSettings.dstLabwareColumns, labwareSettings.dstLabwareRows);
				sw.WriteLine(value3);
			}
		}

		private void SplitVolumes2Region(List<double> volumes, out List<double> volumes1, out List<double> volumes2, int firstRegionSampleCount)
		{
			volumes1 = new List<double>();
			volumes2 = new List<double>();
			for (int i = 0; i < volumes.Count; i++)
			{
				if (i < firstRegionSampleCount)
				{
					volumes1.Add(volumes[i]);
					volumes2.Add(0.0);
				}
				else
				{
					volumes2.Add(volumes[i]);
				}
			}
		}

		private int GetEndIndexForFirstColumn(int srcRackIndex, int startSample)
		{
			return ((srcRackIndex * labwareSettings.sourceWells + startSample) / labwareSettings.dstLabwareRows + 1) * labwareSettings.dstLabwareRows - 1;
		}

		public int GetMaxGrid()
		{
			int num = int.Parse(ConfigurationManager.AppSettings[stringRes.f]);
			if (num != 75 && num != 100 && num != 150 && num != 200)
			{
				string text = "EVOModel must be one of 75,100,150 & 200!";
				throw new Exception("Invalid EVOModel: " + text);
			}
			int num2 = 27;
			return num switch
			{
				100 => 30, 
				150 => 45, 
				200 => 60, 
				_ => 27, 
			};
		}

		private void CalculateDestGridAndSite(int sampleIndex, int slice, ref int grid, ref int site)
		{
			CalculateDestPlasmaGridAndSite(sampleIndex, slice, ref grid, ref site);
		}

		private void CalculateDestGridAndSite4OneSlicePerLabware(int sliceIndex, ref int grid, ref int site)
		{
			int dstLabwareStartGrid = labwareSettings.dstLabwareStartGrid;
			int sitesPerCarrier = labwareSettings.sitesPerCarrier;
			int num = labwareSettings.gridsPerCarrier * (sliceIndex / sitesPerCarrier);
			grid = dstLabwareStartGrid + num;
			site = sliceIndex % labwareSettings.sitesPerCarrier;
		}

		private void CalculateDestPlasmaGridAndSite(int sampleIndex, int slice, ref int grid, ref int site)
		{
			int num = Utility.GetSamplesPerRow4Plasma(labwareSettings, pipettingSettings, pipettingSettings.buffyStandalone) * labwareSettings.dstLabwareRows;
			int num2 = labwareSettings.dstCarrierCnt * labwareSettings.sitesPerCarrier;
			int num3 = num * labwareSettings.sitesPerCarrier;
			if (pipettingSettings.buffyStandalone)
			{
				num2--;
			}
			int num4 = num2 * num;
			int num5 = ((!pipettingSettings.buffyStandalone) ? pipettingSettings.dstbuffySlice : 0);
			int num6 = pipettingSettings.dstPlasmaSlice + pipettingSettings.dstRedCellSlice + num5;
			if (labwareSettings.dstLabwareColumns == 1)
			{
				num4 = 16 * labwareSettings.dstCarrierCnt / num6;
			}
			if (sampleIndex + 1 > num4)
			{
				throw new Exception("Max  samples allowed is: " + $"{num4}!");
			}
			int dstLabwareStartGrid = labwareSettings.dstLabwareStartGrid;
			int maxGrid = GetMaxGrid();
			if (dstLabwareStartGrid > maxGrid)
			{
				throw new Exception($"the destination grid: {dstLabwareStartGrid} exceeds the maximum grid： {maxGrid}");
			}
			int num7 = labwareSettings.gridsPerCarrier;
			if (num7 == 1)
			{
				num7 = num6;
			}
			int num8 = sampleIndex / num3 * num7;
			int num9 = ((labwareSettings.gridsPerCarrier == 1) ? slice : 0);
			int num10 = sampleIndex % num3;
			site = num10 / num;
			grid = num9 + num8 + dstLabwareStartGrid;
		}

		private void CalculateDestRedCellGridAndSite(int sampleIndex, int slice, ref int grid, ref int site)
		{
			int num = ((labwareSettings.gridsPerCarrier == 1) ? slice : 0);
			CalculateDestPlasmaGridAndSite(sampleIndex, 0, ref grid, ref site);
			grid += pipettingSettings.dstPlasmaSlice + pipettingSettings.dstbuffySlice + num;
		}

		private void CalculateDestBuffyGridAndSite(int sampleIndex, ref int grid, ref int site)
		{
			if (pipettingSettings.buffyStandalone)
			{
				grid = labwareSettings.dstLabwareStartGrid + (labwareSettings.dstCarrierCnt - 1) * labwareSettings.gridsPerCarrier;
				site = labwareSettings.sitesPerCarrier - 1;
				return;
			}
			CalculateDestPlasmaGridAndSite(sampleIndex, 0, ref grid, ref site);
			if (labwareSettings.gridsPerCarrier == 1)
			{
				grid += pipettingSettings.dstPlasmaSlice;
			}
		}

		private bool IsDstWellsInSameColumn(int srcRackIndex, int startSample, int sampleCount)
		{
			int num = srcRackIndex * labwareSettings.sourceWells + startSample;
			int num2 = num + sampleCount - 1;
			int dstLabwareRows = labwareSettings.dstLabwareRows;
			int num3 = num / dstLabwareRows;
			int num4 = num2 / dstLabwareRows;
			return num3 == num4;
		}

		private void WriteDispenseBuffyNoCheck(List<POINT> pts, int grid, int site, StreamWriter sw, int tipOffset)
		{
			log.Info("WriteDispenseBuffy for certain region");
			int count = pts.Count;
			int tipSelection = GetTipSelection(count, tipOffset);
			List<double> list = new List<double>();
			double item = Math.Floor(24.0 / (double)pipettingSettings.dstbuffySlice);
			for (int i = 0; i < 8; i++)
			{
				list.Add(item);
			}
			for (int j = 0; j < pipettingSettings.dstbuffySlice; j++)
			{
				List<POINT> list2 = new List<POINT>(pts);
				list2 = ChangePositions(pts, j);
				WriteComment($"Dispensing buffy slice: {j + 1}", sw);
				WriteDispenseBuffyWithMovingPluger(list2, tipSelection, list, grid, site, tipOffset, j, sw);
			}
			WriteComment($"Aspirate air gap steps: {pipettingSettings.airGap}", sw);
			string pPAString = GetPPAString(pts.Count, pipettingSettings.airGap, tipOffset);
			WriteComand(pPAString, sw);
		}

		private void WriteDispenseBuffyWithMovingPluger(List<POINT> pts, int ditiMask, List<double> vols, int grid, int site, int tipOffset, int sliceIndex, StreamWriter sw)
		{
			double num = (double)pipettingSettings.buffyVolume * (1.0 - (double)(sliceIndex + 1) / (double)pipettingSettings.dstbuffySlice);
			int num2 = (int)(24.0 * (1.0 - (double)(sliceIndex + 1) / (double)pipettingSettings.dstbuffySlice));
			int num3 = (int)(num / 1000.0 * 3000.0) + num2;
			WriteMovingPluger(pts, vols, ditiMask, tipOffset, grid, site, num3, sw);
			int count = pts.Count;
			WriteComment($"Move LiHa up to {pipettingSettings.retractHeightcm}cm", sw);
			string moveLihaAbsoluteZSlow = GetMoveLihaAbsoluteZSlow(count, pipettingSettings.retractHeightcm, tipOffset);
			WriteComand(moveLihaAbsoluteZSlow, sw);
			string pPAString = GetPPAString(count, num3 + 90, tipOffset);
			WriteComand(pPAString, sw);
			WriteComment("Set end speed for plungers", sw);
			string sEPString = GetSEPString(count, 2400, tipOffset);
			WriteComand(sEPString, sw);
			WriteComment("Set stop speed for plungers", sw);
			string sPPString = GetSPPString(count, 1500, tipOffset);
			WriteComand(sPPString, sw);
		}

		private void WriteMovingPluger(List<POINT> pts, List<double> vols, int ditiMask, int tipOffset, int grid, int site, int plugerStep, StreamWriter sw)
		{
			string volumeString = GetVolumeString(vols);
			string wellSelection = GetWellSelection(labwareSettings.dstLabwareColumns, labwareSettings.dstLabwareRows, pts);
			string value = string.Format(breakPrefix + "{0}({1},\"{2}\",{3}{4},{5},1,\"{6}\", 0, 0);", "Dispense", ditiMask, "BB_Buffy", volumeString, grid, site, wellSelection);
			WriteComment("Write Dispense for sample tracking", sw);
			sw.WriteLine(value);
			int count = pts.Count;
			WriteComment("Set end speed for plungers", sw);
			string sEPString = GetSEPString(count, 2400, tipOffset);
			WriteComand(sEPString, sw);
			WriteComment("Set stop speed for plungers", sw);
			string sPPString = GetSPPString(count, 1500, tipOffset);
			WriteComand(sPPString, sw);
			WriteComment($"Move plunger to absolut step: {plugerStep}", sw);
			string pPAString = GetPPAString(count, plugerStep, tipOffset);
			WriteComand(pPAString, sw);
		}

		private string GetVolumeString(List<double> vols)
		{
			string text = "";
			for (int i = 0; i < 12; i++)
			{
				string text2 = "";
				text2 = ((i >= vols.Count) ? "0," : $"\"{vols[i]}\",");
				text += text2;
			}
			return text;
		}

		private List<POINT> ChangePositions(List<POINT> pts, int distance2Org)
		{
			List<POINT> list = new List<POINT>();
			foreach (POINT pt in pts)
			{
				list.Add(new POINT(pt.x + (double)distance2Org, pt.y));
			}
			return list;
		}

		private void WriteDispenseBuffy(List<POINT> pts, int grid, int site, StreamWriter sw, int tipOffset = 0)
		{
			double num = pts.Select((POINT x) => x.y).Max();
			int num2 = tipOffset + pts.Count;
			if (num < 4.0 || num > 13.0)
			{
				bool flag = true;
				if (num - (double)num2 >= 12.0 || (double)num2 - num >= 5.0)
				{
					flag = false;
				}
				int num3 = int.Parse(ConfigurationManager.AppSettings[stringRes.f]);
				if (!flag && num3 != 75)
				{
					throw new Exception("Cannot match tips at labware edge");
				}
			}
			WriteDispenseBuffyNoCheck(pts, grid, site, sw, tipOffset);
		}

		private void WriteDispenseBuffy(int rackIndex, int sampleIndexThisRack, int samplesCountThisBatch, bool bNeedUseLastFour, StreamWriter sw)
		{
			log.Info("Write DispenseBuffy");
			int dstPlasmaSlice = pipettingSettings.dstPlasmaSlice;
			List<POINT> pts = (pipettingSettings.buffyStandalone ? positionGenerator.GetDestWellsBuffyOnly(rackIndex, dstPlasmaSlice, sampleIndexThisRack, samplesCountThisBatch) : positionGenerator.GetDestWells(rackIndex, dstPlasmaSlice, sampleIndexThisRack, samplesCountThisBatch));
			int grid = 0;
			int site = 0;
			CalculateDestBuffyGridAndSite(GetGlobalSampleIndex(rackIndex, sampleIndexThisRack), ref grid, ref site);
			int tipOffset = (bNeedUseLastFour ? 4 : 0);
			WriteDispenseBuffy(pts, grid, site, sw, tipOffset);
		}

		private void WriteMSDCommands(StreamWriter sw, List<DetectedInfo> detectedInfos, int tipOffset)
		{
			log.Info("Write MSD");
			int count = detectedInfos.Count;
			List<double> heights = detectedInfos.Select((DetectedInfo x) => x.Z2).ToList();
			MoveTipsToAbsolutePosition(sw, heights, tipOffset);
			int buffyVolume = pipettingSettings.buffyVolume;
			int num = 9;
			double buffySpeedFactor = pipettingSettings.buffySpeedFactor;
			int speedXY = (int)(60.0 * buffySpeedFactor);
			double area = mappingCalculator.GetArea();
			double num2 = ((double)(pipettingSettings.buffyVolume * 10) / area + pipettingSettings.msdZDistance * 10.0) / (double)(2 * pipettingSettings.buffyAspirateLayers);
			int num3 = pipettingSettings.deltaXYForMSD;
			int accXY = 2000;
			int numSegments = 5;
			double num4 = (double)buffyVolume / ((double)pipettingSettings.buffyAspirateLayers * 2.0);
			int dialutorSteps = (int)(3.1 * num4);
			int aspSpeedSteps = (int)(3.1 * (double)num * buffySpeedFactor * (double)buffyVolume / 300.0);
			int tipSelection = GetTipSelection(count, tipOffset);
			WriteComment("Set Move values", sw);
			string sEPString = GetSEPString(count, aspSpeedSteps, tipOffset);
			WriteComand(sEPString, sw);
			int num5 = 0;
			for (int i = 0; i < pipettingSettings.buffyAspirateLayers; i++)
			{
				WriteComment($"Move LiHa spiral out -times: {i + 1}", sw);
				string mSDCommand = GetMSDCommand(num3, numSegments, tipSelection, dialutorSteps, speedXY, accXY);
				WriteComand(mSDCommand, sw);
				WriteComment($"Move LiHa deltaZ down -times: {i + 1} distance:{num2}", sw);
				int num6 = (int)((double)(i + 1) * num2 - (double)num5 + 0.5);
				num5 += num6;
				string moveLihaDown = GetMoveLihaDown(count, -num6, tipOffset);
				WriteComand(moveLihaDown, sw);
				WriteComment($"Move LiHa spiral in -times: {i + 1}", sw);
				num3 = -num3;
				mSDCommand = GetMSDCommand(num3, numSegments, tipSelection, dialutorSteps, speedXY, accXY);
				WriteComand(mSDCommand, sw);
				num3 = -num3;
				if (i != pipettingSettings.buffyAspirateLayers - 1)
				{
					WriteComment($"Move LiHa deltaZ down -times: {i + 1}", sw);
					WriteComand(moveLihaDown, sw);
					continue;
				}
				break;
			}
		}

		private void WriteRacksCount(int n)
		{
			Utility.Write2File(Utility.GetOutputFolder() + "RacksCount.txt", n.ToString());
		}

		private void SaveRunResult(RunResult runResult)
		{
			string path = Utility.GetOutputFolder() + "runResult.xml";
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			string contents = Utility.Serialize(runResult);
			File.WriteAllText(path, contents);
		}

		private void AddCommonInfo2RunResult(RunResult runResult)
		{
			runResult.buffySlice = pipettingSettings.dstbuffySlice;
			runResult.buffyVolume = pipettingSettings.buffyVolume;
			runResult.plasmaVolume = pipettingSettings.plasmaGreedyVolume;
			runResult.plasmaTotalSlice = pipettingSettings.dstPlasmaSlice;
		}

		private void AddEachSampleInfo2RunResult(int srcRackIndex, int startSampleIndex, List<DetectedInfo> heightsThisTime, RunResult runResult)
		{
			double area = mappingCalculator.GetArea();
			for (int i = 0; i < heightsThisTime.Count; i++)
			{
				double z = heightsThisTime[i].Z1;
				double z2 = heightsThisTime[i].Z2;
				double num = mappingCalculator.GetVolumeFromHeight(z) - mappingCalculator.GetVolumeFromHeight(z2) - (double)pipettingSettings.safeDelta * area;
				int num2 = pipettingSettings.dstPlasmaSlice;
				if (pipettingSettings.plasmaGreedyVolume != 0.0)
				{
					int val = (int)Math.Ceiling(num / pipettingSettings.plasmaGreedyVolume);
					num2 = Math.Min(num2, val);
				}
				_ = labwareSettings.sourceWells;
				runResult.plasmaRealSlices.Add(num2);
			}
		}

		private void WriteRunResult(int srcRackIndex, int startSampleIndex, List<DetectedInfo> heightsThisTime, StreamWriter sw)
		{
			double area = mappingCalculator.GetArea();
			for (int i = 0; i < heightsThisTime.Count; i++)
			{
				double z = heightsThisTime[i].Z1;
				double z2 = heightsThisTime[i].Z2;
				double num = (z - z2 - (double)pipettingSettings.safeDelta) * area;
				int num2 = pipettingSettings.dstPlasmaSlice;
				if (pipettingSettings.plasmaGreedyVolume != 0.0)
				{
					int val = (int)Math.Ceiling(num / pipettingSettings.plasmaGreedyVolume);
					num2 = Math.Min(num2, val);
				}
				int num3 = srcRackIndex * labwareSettings.sourceWells + i + startSampleIndex + 1;
				int dstbuffySlice = pipettingSettings.dstbuffySlice;
				sw.WriteLine($"{num3};{num2};{pipettingSettings.dstPlasmaSlice};{dstbuffySlice}{pipettingSettings}");
			}
		}
	}
}
