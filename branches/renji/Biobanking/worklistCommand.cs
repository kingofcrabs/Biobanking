using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;
using Settings;

namespace Biobanking
{
	internal class worklistCommand
	{
		protected const string BBPlasmaFast = "BB_Plasma_Fast";

		protected const string BBPlasmaMedium = "BB_Plasma_Medium";

		protected const string BBPlasmaSlow = "BB_Plasma_Slow";

		protected const string BBRedCell = "BB_RedCell";

		protected const string BB_Buffy = "BB_Buffy";

		protected const string BB_Buffy_Mix = "BB_Buffy_Mix";

		protected string breakPrefix = "B;";

		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected PipettingSettings pipettingSettings = new PipettingSettings();

		protected LabwareSettings labwareSettings = new LabwareSettings();

		public worklistCommand()
		{
			SettingsHelper settingsHelper = new SettingsHelper();
			log.Info("load settings");
			settingsHelper.LoadSettings(ref pipettingSettings, ref labwareSettings);
			string errMsg = "";
			if (!settingsHelper.IsValidSetting(labwareSettings, pipettingSettings, ref errMsg))
			{
				throw new Exception("Invalid setting:" + errMsg);
			}
		}

		protected int GetTipSelection(int samplesInTheBatch, int startTip = 0)
		{
			int num = 0;
			for (int i = 0; i < samplesInTheBatch; i++)
			{
				num += (int)Math.Pow(2.0, i + startTip);
			}
			return num;
		}

		protected int GetTipSelection(List<double> volumes)
		{
			int num = 0;
			for (int i = 0; i < volumes.Count; i++)
			{
				if (volumes[i] != 0.0)
				{
					num += (int)Math.Pow(2.0, i);
				}
			}
			return num;
		}

		protected void WriteComand(string sSEP, StreamWriter sw)
		{
			string value = string.Format(breakPrefix + "Command(\"{0}\",1,1,,,2,2,0);", sSEP);
			sw.WriteLine(value);
		}

		protected string GetComment(string sComment)
		{
			return string.Format(breakPrefix + "Comment(\"{0}\");", sComment);
		}

		protected void WriteComment(string sComment, StreamWriter sw)
		{
			string comment = GetComment(sComment);
			sw.WriteLine(comment);
		}

		protected void WriteVariable(string sName, string sVal, StreamWriter sw)
		{
			string value = string.Format(breakPrefix + "Variable({0}, \"{1}\", 0, \"\", 0, 1.000000, 10.000000, 0, 2, 0, 0)", sName, sVal);
			sw.WriteLine(value);
		}

		protected void WriteVariable(string sName, int nVal, StreamWriter sw)
		{
			string sVal = nVal.ToString();
			WriteVariable(sName, sVal, sw);
		}

		protected string GenerateAspirateCommand(List<POINT> wells, List<double> volumes, string liquidClass, int gridPos, int site, int height)
		{
			return GenerateAspirateOrDispenseCommand(wells, volumes, liquidClass, gridPos, site, 1, height, aspirate: true);
		}

		protected string GenerateDispenseCommand(List<POINT> wells, List<double> volumes, string liquidClass, int gridPos, int site, int width, int height)
		{
			return GenerateAspirateOrDispenseCommand(wells, volumes, liquidClass, gridPos, site, width, height, aspirate: false);
		}

		protected string GenerateAspirateOrDispenseCommand(List<POINT> wells, List<double> volumes, string liquidClass, int gridPos, int site, int width, int height, bool aspirate)
		{
			int tipSelection = GetTipSelection(volumes);
			List<POINT> list = new List<POINT>();
			for (int i = 0; i < wells.Count; i++)
			{
				if (volumes[i] != 0.0)
				{
					list.Add(wells[i]);
				}
			}
			string text = "";
			for (int j = 0; j < 12; j++)
			{
				string text2 = "";
				text2 = ((j >= volumes.Count) ? "0," : $"\"{volumes[j]}\",");
				text += text2;
			}
			string wellSelection = GetWellSelection(width, height, list);
			string text3 = (aspirate ? "Aspirate" : "Dispense");
			return string.Format(breakPrefix + "{0}({1},\"{2}\",{3}{4},{5},1,\"{6}\", 0, 0);", text3, tipSelection, liquidClass, text, gridPos, site, wellSelection);
		}

		protected int GetGlobalSampleIndex(int rackIndex, int startSample)
		{
			return rackIndex * labwareSettings.sourceWells + startSample;
		}

		public static string Chr(int asciiCode)
		{
			if (asciiCode >= 0 && asciiCode <= 255)
			{
				byte[] bytes = new byte[1] { (byte)asciiCode };
				return Encoding.Default.GetString(bytes);
			}
			throw new Exception("ASCII Code is not valid.");
		}

		protected string GetWellSelection(int width, int height, List<POINT> wells)
		{
			string text = $"{width:X2}{height:X2}";
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					if (wells.IndexOf(new POINT(i + 1, j + 1)) != -1)
					{
						num2 |= 1 << num;
					}
					if (++num > 6)
					{
						string text2 = Chr(48 + num2);
						text += text2;
						num = 0;
						num2 = 0;
					}
				}
			}
			if (num > 0)
			{
				text += (char)(48 + num2);
			}
			return text;
		}

		protected List<string> GetSetVolString(int tipIndex, double volume2Set)
		{
			string arg = $"Tip_Volume_{tipIndex + 1}";
			string arg2 = ((int)volume2Set).ToString();
			string item = string.Format(breakPrefix + "Variable({0}, {1}, 0, \"\", 0, 1.000000, 10.000000, 0, 2, 0, 0);", arg, arg2);
			return new List<string> { item };
		}

		protected void WriteSetVolString(int tipIndex, double volume2Set, StreamWriter sw)
		{
			foreach (string item in GetSetVolString(tipIndex, volume2Set))
			{
				sw.WriteLine(item);
			}
		}

		protected string GetCommandForAllTips(string sCommandPrefix, int samplesInTheBatch, int val, int tipOffset = 0)
		{
			string text = sCommandPrefix;
			for (int i = 0; i < tipOffset; i++)
			{
				text += ",";
			}
			for (int j = 0; j < samplesInTheBatch; j++)
			{
				text = text + val + ",";
			}
			for (int k = samplesInTheBatch + tipOffset; k < 8; k++)
			{
				text += ",";
			}
			return text;
		}

		protected void MoveTipsToAbsolutePosition(StreamWriter sw, List<double> heights, int tipOffset)
		{
			string text = "C5PAZ";
			for (int i = 0; i < tipOffset; i++)
			{
				text += ",";
			}
			for (int j = 0; j < heights.Count; j++)
			{
				double num = (double)pipettingSettings.bottomOffset + (heights[j] + pipettingSettings.msdStartPositionAboveBuffy) * 10.0;
				text = text + (int)num + ",";
			}
			for (int k = heights.Count + tipOffset; k < 8; k++)
			{
				text += ",";
			}
			WriteComand(text, sw);
		}

		protected string GetPPAString(int samplesThisBatch, int pos, int tipOffset)
		{
			string sCommandPrefix = "C5PPA";
			return GetCommandForAllTips(sCommandPrefix, samplesThisBatch, pos, tipOffset);
		}

		protected string GetMoveLihaAbsoluteZ(int samplesInTheBatch, int zPosition, int tipOffset)
		{
			string sCommandPrefix = "C5PAZ";
			return GetCommandForAllTips(sCommandPrefix, samplesInTheBatch, zPosition, tipOffset);
		}

		protected string GetMoveLihaAbsoluteZSlow(int samplesInTheBatch, int zPosition, int tipOffset)
		{
			string sCommandPrefix = "C5MAZ";
			return GetCommandForAllTips(sCommandPrefix, samplesInTheBatch, zPosition * 100, tipOffset) + "300";
		}

		protected string GetMoveLihaDown(int samplesInTheBatch, int deltaZ, int tipOffset)
		{
			string sCommandPrefix = "C5PRZ";
			return GetCommandForAllTips(sCommandPrefix, samplesInTheBatch, deltaZ, tipOffset);
		}

		protected string GetSEPString(int samplesInTheBatch, int aspSpeedSteps, int tipOffset)
		{
			string sCommandPrefix = "C5SEP";
			return GetCommandForAllTips(sCommandPrefix, samplesInTheBatch, aspSpeedSteps, tipOffset);
		}

		protected string GetSPPString(int samplesThisBatch, int speed, int tipOffset)
		{
			string sCommandPrefix = "C5SPP";
			return GetCommandForAllTips(sCommandPrefix, samplesThisBatch, speed, tipOffset);
		}

		protected string GetMSDCommand(int deltaXY, int numSegments, int tipSel, int dialutorSteps, int speedXY, int accXY)
		{
			string text = (pipettingSettings.msdXYTogether ? deltaXY.ToString() : string.Format("{0},{0}", deltaXY));
			return $"C5MSD{text},{numSegments},{tipSel},{dialutorSteps},0,{speedXY},{accXY}";
		}
	}
}
