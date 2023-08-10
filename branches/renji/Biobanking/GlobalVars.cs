using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Settings;

namespace Biobanking
{
	internal class GlobalVars
	{
		private static GlobalVars instance;

		public static GlobalVars Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new GlobalVars();
				}
				return instance;
			}
		}

		public bool TrackBarcode { get; set; }

		public string ResultFile { get; set; }

		public string DstBarcodeFolder { get; set; }

		public string SrcBarcodeFile { get; set; }

		public FileStruct FileStruct { get; set; }

		public string BloodDescription { get; set; }

		public bool IsRedCell { get; set; }

		public string BuffyName => TranslateDescription("Buffy");

		public string Barcode2DVendor { get; set; }

		private GlobalVars()
		{
			DstBarcodeFolder = ConfigurationManager.AppSettings["DstBarcodeFolder"];
			SrcBarcodeFile = ConfigurationManager.AppSettings["SrcBarcodeFile"];
			ResultFile = ConfigurationManager.AppSettings[stringRes.reportPath];
			string text = Utility.GetExeFolder() + "fileStruct.xml";
			ConfigurationManager.OpenExeConfiguration(Utility.GetExeFolder() + "SampleInfo.exe");
			BloodDescription = File.ReadAllText(Utility.GetBloodTypeFile());
			IsRedCell = BloodDescription == "RedCell";
			BloodDescription = TranslateDescription(BloodDescription);
			Barcode2DVendor = ConfigurationManager.AppSettings["2DBarcodeVendor"];
			TrackBarcode = DstBarcodeFolder != "";
			if (File.Exists(text))
			{
				string xml = File.ReadAllText(text);
				FileStruct = Utility.Deserialize<FileStruct>(xml);
			}
			else
			{
				FileStruct = new FileStruct();
				Utility.SaveSettings(FileStruct, text);
			}
		}

		public static string TranslateDescription(string BloodDescription)
		{
			if (ConfigurationManager.AppSettings["2DBarcodeVendor"] != "HR")
			{
				return BloodDescription;
			}
			return new Dictionary<string, string>
			{
				{ "Plasma", "血浆" },
				{ "Serum", "血清" },
				{ "Buffy", "白膜" },
				{ "RedCell", "红细胞" }
			}[BloodDescription];
		}
	}
}
