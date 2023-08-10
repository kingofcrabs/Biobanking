using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using log4net;
using Settings;

namespace Biobanking
{
	internal class ExcelReader
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private int startIndex;

		private LabwareSettings labwareSettings;

		private PipettingSettings pipettingSettings;

		public List<List<Tuple<string, string>>> ReadBarcodes(LabwareSettings labwareSettings, PipettingSettings pipettingSettings, Dictionary<string, string> barcode_plateBarcode, Dictionary<string, string> barcode_Position)
		{
			startIndex = 0;
			this.labwareSettings = labwareSettings;
			this.pipettingSettings = pipettingSettings;
			List<FileInfo> source = new DirectoryInfo(GlobalVars.Instance.DstBarcodeFolder).EnumerateFiles("*.csv").ToList();
			source = source.OrderBy((FileInfo x) => x.CreationTime).ToList();
			List<List<Tuple<string, string>>> correspondingbarcodes = new List<List<Tuple<string, string>>>();
			List<string> list = source.Select((FileInfo x) => x.FullName).ToList();
			string text = "";
			if (pipettingSettings.buffyStandalone && pipettingSettings.dstbuffySlice != 0)
			{
				switch (source.Count((FileInfo x) => x.FullName.ToLower().Contains("buffy")))
				{
				case 0:
					throw new Exception("No barcode file for buffy plate found!");
				default:
					throw new Exception("Only one buffy plate supported!");
				case 1:
					break;
				}
				text = source.Where((FileInfo x) => x.FullName.ToLower().Contains("buffy")).First().FullName;
				list = list.Except(new List<string> { text }).ToList();
			}
			list.ForEach(delegate(string x)
			{
				ReadBarcode(correspondingbarcodes, barcode_plateBarcode, barcode_Position, x);
			});
			if (pipettingSettings.dstbuffySlice > 0 && pipettingSettings.buffyStandalone)
			{
				ReadBarcode(correspondingbarcodes, barcode_plateBarcode, barcode_Position, text);
			}
			return correspondingbarcodes;
		}

		private int GetNum(DirectoryInfo d)
		{
			return int.Parse(d.Name.Substring(5));
		}

		private void ReadBarcode(List<List<Tuple<string, string>>> srcTubeCorrespondingBarcodes, Dictionary<string, string> barcode_plateBarcode, Dictionary<string, string> barcode_Position, string sFile)
		{
			List<string> list = File.ReadAllLines(sFile).ToList();
			string text = "dummy";
			string text2 = ConfigurationManager.AppSettings["2DBarcodeVendor"];
			switch (text2)
			{
			case "HR":
				text = sFile.Substring(sFile.LastIndexOf("\\") + 1);
				text = text.Replace(".csv", "");
				break;
			case "INK":
				text = GetPlateBarcode4Ink(list);
				break;
			case "WG":
				text = list[1].Split(',').ToList()[3];
				if (text == "")
				{
					throw new Exception($"Plate ID is empty in file：{sFile}");
				}
				list = list.Skip(1).ToList();
				break;
			default:
			{
				int num = list[0].ToLower().IndexOf("id:");
				if (num == -1)
				{
					throw new Exception("cannot find Plate ID！");
				}
				text = list[1].Substring(num + 3);
				if (text == "")
				{
					throw new Exception($"Plate ID is empty in file：{sFile}");
				}
				list = list.Skip(1).ToList();
				break;
			}
			}
			int barcodeColumnIndex = GetBarcodeColumnIndex();
			startIndex += labwareSettings.dstLabwareRows * labwareSettings.dstLabwareColumns;
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			ReadBarcodes(list, barcode_Position, barcode_plateBarcode, dictionary, barcodeColumnIndex, text, text2);
			int dstbuffySlice = pipettingSettings.dstbuffySlice;
			int samplesPerRow4Buffy;
			if (dstbuffySlice != 0 && pipettingSettings.buffyStandalone && sFile.ToLower().Contains("buffy"))
			{
				samplesPerRow4Buffy = Utility.GetSamplesPerRow4Buffy(labwareSettings, pipettingSettings);
				int num2 = 0;
				for (int i = 0; i < samplesPerRow4Buffy; i++)
				{
					int num3 = i * dstbuffySlice;
					for (int j = 0; j < labwareSettings.dstLabwareRows; j++)
					{
						new List<Tuple<string, string>>();
						for (int k = 0; k < dstbuffySlice; k++)
						{
							string text3 = $"{(char)(65 + j)}{num3 + k + 1:D2}";
							string item = "";
							if (dictionary.ContainsKey(text3))
							{
								item = dictionary[text3];
							}
							Tuple<string, string> item2 = Tuple.Create(text3, item);
							if (num2 >= srcTubeCorrespondingBarcodes.Count)
							{
								break;
							}
							srcTubeCorrespondingBarcodes[num2++].Add(item2);
						}
					}
				}
				return;
			}
			int num4 = ((!pipettingSettings.buffyStandalone) ? pipettingSettings.dstbuffySlice : 0) + pipettingSettings.dstPlasmaSlice + pipettingSettings.dstRedCellSlice;
			samplesPerRow4Buffy = Utility.GetSamplesPerRow4Plasma(labwareSettings, pipettingSettings, pipettingSettings.buffyStandalone);
			for (int l = 0; l < samplesPerRow4Buffy; l++)
			{
				int num5 = l * num4;
				for (int m = 0; m < labwareSettings.dstLabwareRows; m++)
				{
					List<Tuple<string, string>> list2 = new List<Tuple<string, string>>();
					for (int n = 0; n < num4; n++)
					{
						string text4 = $"{(char)(65 + m)}{num5 + n + 1:D2}";
						string item3 = "";
						if (dictionary.ContainsKey(text4))
						{
							item3 = dictionary[text4];
						}
						Tuple<string, string> item4 = Tuple.Create(text4, item3);
						list2.Add(item4);
					}
					srcTubeCorrespondingBarcodes.Add(list2);
				}
			}
		}

		private void ReadBarcodes(List<string> strs, Dictionary<string, string> barcode_Position, Dictionary<string, string> barcode_plateBarcode, Dictionary<string, string> barcodesThisPlate, int barcodeColumnIndex, string plateBarcode, string vendorName)
		{
			int num = 1;
			int count = 2;
			if (vendorName == "WG")
			{
				count = 0;
			}
			strs = strs.Skip(count).ToList();
			foreach (string str in strs)
			{
				if (str == "")
				{
					continue;
				}
				string[] array = str.Split(',');
				string description = Utility.GetDescription(num);
				string barcode = array[barcodeColumnIndex];
				barcode = barcode.Replace("\"", "");
				barcodesThisPlate.Add(description, barcode);
				if (barcode == "noTube" || barcode == "error")
				{
					continue;
				}
				if (barcodesThisPlate.Where((KeyValuePair<string, string> x) => x.Value == barcode).Count() > 1)
				{
					List<string> list = (from x in barcodesThisPlate
						where x.Value == barcode
						select x.Key).ToList();
					throw new Exception($"Position at {list[0]} and {list[1]}'s barcodes:{barcode} are duplicated.");
				}
				barcode_Position.Add(barcode, description);
				barcode_plateBarcode.Add(barcode, plateBarcode);
				num++;
			}
		}

		private int GetBarcodeColumnIndex()
		{
			string key = ConfigurationManager.AppSettings["2DBarcodeVendor"];
			return new Dictionary<string, int>
			{
				{ "HR", 1 },
				{ "WG", 2 },
				{ "INK", 1 }
			}[key];
		}

		private string GetPlateBarcode4Ink(List<string> strs)
		{
			return strs[1].Replace("Plate barcode:", "");
		}

		private string GetPosition(List<string> strs)
		{
			List<string> newStrs = new List<string>();
			strs.ForEach(delegate(string x)
			{
				newStrs.Add(x.Replace("\"", ""));
			});
			if (GlobalVars.Instance.Barcode2DVendor.ToLower() == "baiquan")
			{
				int num = int.Parse(newStrs[1]);
				int rowIndex = newStrs[2][0] - 65;
				return Utility.GetDescription(PositionGenerator.GetWellID(num - 1, rowIndex));
			}
			return Utility.GetDescription(PositionGenerator.ParseWellID(newStrs[GlobalVars.Instance.FileStruct.dstPosition]));
		}

		private bool IsValidBarcode(string barcode)
		{
			for (int i = 0; i < barcode.Length; i++)
			{
				if (!char.IsDigit(barcode[i]))
				{
					return false;
				}
			}
			return true;
		}

	
	}
}
