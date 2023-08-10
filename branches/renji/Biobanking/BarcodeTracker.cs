using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using Biobanking.ExcelExporter;
using Settings;

namespace Biobanking
{
	public class BarcodeTracker
	{
		private List<TrackInfo> trackInfos = new List<TrackInfo>();

		private PipettingSettings pipettingSettings;

		private List<List<Tuple<string, string>>> correspondingbarcodes;

		private Dictionary<string, string> barcode_plateBarcodes = new Dictionary<string, string>();

		private Dictionary<string, string> barcode_Position = new Dictionary<string, string>();

		private List<PatientInfo> patientInfos;

		private int sampleIndex;

		public BarcodeTracker(PipettingSettings pipettingSettings, LabwareSettings labwareSettings, List<PatientInfo> patientInfos)
		{
			this.patientInfos = patientInfos;
			this.pipettingSettings = pipettingSettings;
			ExcelReader excelReader = new ExcelReader();
			correspondingbarcodes = excelReader.ReadBarcodes(labwareSettings, pipettingSettings, barcode_plateBarcodes, barcode_Position);
			if (patientInfos.Count > correspondingbarcodes.Count)
			{
				throw new Exception($"source barcodes' count:{patientInfos.Count} > dest barcodes' count :{correspondingbarcodes.Count}");
			}
		}

		private bool IsValidBarcode(string s)
		{
			for (int i = 0; i < s.Length; i++)
			{
				if (char.IsDigit(s[i]))
				{
					return true;
				}
			}
			return false;
		}

		internal void Track(List<double> plasmaVols, int sliceIndex, string description)
		{
			int num = 0;
			foreach (double plasmaVol in plasmaVols)
			{
				if (plasmaVol > 0.0)
				{
					Tuple<string, string> tuple = correspondingbarcodes[sampleIndex + num][sliceIndex];
					string item = tuple.Item2;
					if (item == "")
					{
						throw new Exception($"cannot find barcode for position:{tuple.Item1}！");
					}
					if (!IsValidBarcode(item))
					{
						throw new Exception($"Sample:{sampleIndex + num + 1}, slice:{sliceIndex + 1}'s barcode:{item} is invalid!");
					}
					int num2 = (int)Math.Min(pipettingSettings.maxVolumePerSlice, plasmaVol);
					if (patientInfos.Count <= sampleIndex + num)
					{
						throw new Exception($"Cannot find sample:{sampleIndex + num + 1}'s source barcode");
					}
					PatientInfo patientInfo = patientInfos[sampleIndex + num];
					TrackInfo item2 = new TrackInfo(patientInfo.id, item, description, num2.ToString(), barcode_plateBarcodes[item], barcode_Position[item], patientInfo.name, patientInfo.age, patientInfo.seqNo);
					trackInfos.Add(item2);
				}
				num++;
			}
			if (sliceIndex + 1 == pipettingSettings.dstPlasmaSlice)
			{
				TrackBuffy(plasmaVols.Count);
			}
			int num3 = ((pipettingSettings.dstRedCellSlice > 0) ? pipettingSettings.GetTotalSlice() : pipettingSettings.dstPlasmaSlice);
			if (sliceIndex + 1 == num3)
			{
				sampleIndex += plasmaVols.Count;
			}
		}

		private void TrackBuffy(int thisBatchCnt)
		{
			if (pipettingSettings.dstbuffySlice == 0)
			{
				return;
			}
			int num = pipettingSettings.buffyVolume / pipettingSettings.dstbuffySlice;
			for (int i = 0; i < thisBatchCnt; i++)
			{
				PatientInfo patientInfo = patientInfos[sampleIndex + i];
				for (int j = 0; j < pipettingSettings.dstbuffySlice; j++)
				{
					if (sampleIndex + i >= correspondingbarcodes.Count)
					{
						throw new Exception($"cannot find the corresponding barcode for sample:{sampleIndex + i}");
					}
					if (pipettingSettings.dstPlasmaSlice + j >= correspondingbarcodes[sampleIndex + i].Count)
					{
						throw new Exception($"cannot find the corresponding barcode for sample:{sampleIndex + i}, slice:{pipettingSettings.dstPlasmaSlice + j}");
					}
					string item = correspondingbarcodes[sampleIndex + i][pipettingSettings.dstPlasmaSlice + j].Item2;
					TrackInfo item2 = new TrackInfo(patientInfo.id, item, GlobalVars.Instance.BuffyName, num.ToString(), barcode_plateBarcodes[item], barcode_Position[item], patientInfo.name, patientInfo.age, patientInfo.seqNo);
					trackInfos.Add(item2);
				}
			}
		}

		internal void WriteResult()
		{
			Utility.SaveSettings(trackInfos, Utility.GetOutputFolder() + "trackinfo.xml");
			string text = Utility.GetOutputFolder() + DateTime.Now.ToString("yyyyMMdd") + "\\";
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			WriteResult2SqlServer();
			Save2Excel(text);
		}

		private void Save2Excel(string sFolder)
		{
			string text = sFolder + "csv\\";
			string text2 = sFolder + "excel\\";
			CreateIfNotExist(text);
			CreateIfNotExist(text2);
			string text3 = DateTime.Now.ToString("HHmmss");
			if (!Directory.Exists(sFolder))
			{
				Directory.CreateDirectory(sFolder);
			}
			string text4 = text + text3 + ".csv";
			string sExcel = text2 + text3 + ".xls";
			string text5 = ConfigurationManager.AppSettings["ExcelTemplate"].ToLower();
			if (text5 == "beijinguniv")
			{
				Save2ExcelForBeiJingUniv(text4, sExcel);
			}
			else
			{
				Save2ExcelDefault(text4);
			}
		}

		private void Save2ExcelDefault(string sCSVFile)
		{
			DefaultExcelTemplate.Save2Excel(trackInfos, sCSVFile);
		}

		private void Save2ExcelForBeiJingUniv(string sCSV, string sExcel)
		{
			BeiJingUnivExcelTemplate.Save2Excel(trackInfos, sCSV, sExcel);
		}

		private void WriteResult2SqlServer()
		{
			string text = ConfigurationManager.AppSettings["ConnectionString"];
			if (text == "")
			{
				Console.WriteLine("No sql connection string.");
				return;
			}
			Console.WriteLine("Writing result into sql, it takes a long time, please wait...");
			SqlConnection sqlConnection = new SqlConnection();
			sqlConnection.ConnectionString = text;
			sqlConnection.Open();
			HashSet<string> srcBarcodes = new HashSet<string>();
			
			foreach (TrackInfo trackInfo in trackInfos)
			{
				srcBarcodes.Add(trackInfo.sourceBarcode);
				new SqlCommand($"insert into interface_tecan_info\r\n(SourceBarcode,DestBarcode,Volume," +
                    $"TypeDescription,DestPlateBarcode,PositionInPlate) values \r\n('{trackInfo.sourceBarcode}','{trackInfo.dstBarcode}','{trackInfo.volume}','{trackInfo.description}','{trackInfo.plateBarcode}','{trackInfo.position}')", sqlConnection).ExecuteNonQuery();
			}

			//CentrifugeTime
			//CentrifugeType
			string centrifugeInfoFile = Utility.GetOutputFolder()+ "CentrifugeInfo.txt";
			if (!File.Exists(centrifugeInfoFile))
				throw new Exception("Cannot find CentrifugeInfo.txt");
			var strs = File.ReadAllLines(centrifugeInfoFile);
			string sCentrifugeTime = strs[0];
			string sCentrifugeType = strs[1];
			foreach (var barcode in srcBarcodes)
            {
				new SqlCommand($"insert into interface_tecan_centrifuge_info\r\n(CentrifugeTime,CentrifugeType,SourceBarcode)" +
			$"values \r\n('{sCentrifugeTime}','{sCentrifugeType}','{barcode}')", sqlConnection).ExecuteNonQuery();

			}
			sqlConnection.Close();
		}

		private void CreateIfNotExist(string csvFolder)
		{
			if (!Directory.Exists(csvFolder))
			{
				Directory.CreateDirectory(csvFolder);
			}
		}
	}
}
