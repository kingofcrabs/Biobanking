using Settings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckPlateBarcodes
{
    class Program
    {
        static void Main(string[] args)
        {
            LabwareSettings labwareSettings = new LabwareSettings();
            PipettingSettings pipettingSettings = new PipettingSettings();
            string xmlFolder = Utility.GetExeFolder();
            var sLabwareSettingFileName = xmlFolder + "\\labwareSettings.xml";
            var sPipettingFileName = xmlFolder + "\\pipettingSettings.xml";
            string s = File.ReadAllText(sPipettingFileName);
            pipettingSettings = Utility.Deserialize<PipettingSettings>(s);
            s = File.ReadAllText(sLabwareSettingFileName);
            labwareSettings = Utility.Deserialize<LabwareSettings>(s);

            int samplesPerRow = Utility.GetSamplesPerRow4Plasma(labwareSettings, pipettingSettings, false);
            if (labwareSettings.gridsPerCarrier == 1)
                samplesPerRow = 1;
            int maxSampleCntPerLabware = samplesPerRow * labwareSettings.dstLabwareRows;

            string sampleCountFile = Utility.GetOutputFolder() + "SampleCount.txt";
            string sSampleCnt = File.ReadAllText(sampleCountFile);
            int totalSampleCnt = int.Parse(sSampleCnt);

            //C:\biobanking\Output
            //int buffyNeedPlate = buffyStandalone ? 1 : 0;
            int labwaresNeeded =  (totalSampleCnt + maxSampleCntPerLabware - 1) / maxSampleCntPerLabware;

            string dstBarcodeFolder = GetDstBarcodeFoler();
            var di = new DirectoryInfo(dstBarcodeFolder);
            var files = di.EnumerateFiles("*.txt").ToList();
            string checkPlateResult = Utility.GetOutputFolder()  + "checkPlateResult.txt";
            Utility.WriteExecuteResult(false, checkPlateResult);
            if(files.Count < labwaresNeeded)
            {
                Console.WriteLine(string.Format("Need {0} plates, only {1} plates found!",labwaresNeeded,files.Count));
                Console.WriteLine("Press any key to exit!"); 
                Console.ReadKey();
                return;
            }

            int totalSlicePerSample = pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice;

            for(int plateIndex = 0; plateIndex < labwaresNeeded; plateIndex++)
            {
                int neededSamples = maxSampleCntPerLabware;
                if(plateIndex == labwaresNeeded-1)
                    neededSamples = totalSampleCnt - (labwaresNeeded - 1) * maxSampleCntPerLabware;
                try
                {
                    CheckFile(files[plateIndex], neededSamples, samplesPerRow, totalSlicePerSample, labwareSettings);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Press any key to exit!");
                    Console.ReadKey();
                }
            }
            Utility.WriteExecuteResult(true, checkPlateResult);

        }

        private static void CheckFile(FileInfo fileInfo, int neededSamples, int samplesPerRow, int totalSlicePerSample, LabwareSettings labwareSettings)
        {
            string sFile = fileInfo.FullName;
            List<string> strs = File.ReadAllLines(sFile).ToList();
            string plateBarcode = sFile.Substring(sFile.LastIndexOf("\\") + 1);
            plateBarcode = plateBarcode.Replace(".txt", "");

            Dictionary<string, string> position_barcode = new Dictionary<string, string>();
            foreach (var s in strs)
            {
                if (s == "")
                    continue;
                var subStrs = s.Split(',');
                string position = subStrs[0];
                var barcode = subStrs[1];
                barcode = barcode.Replace("\"", "");
                if (barcode == "" || barcode == "NOREAD" || barcode == "NOTUBE")
                {
                    continue; //ignore empty barcodes.
                }
                position_barcode.Add(position, barcode);
            }

            int currentSampleID = 1;
            bool needFurtherTest = true;
            for (int subRegionIndex = 0; subRegionIndex < samplesPerRow; subRegionIndex++)
            {
                int startColumn = subRegionIndex * totalSlicePerSample;
                for (int rowIndex = 0; rowIndex < labwareSettings.dstLabwareRows; rowIndex++)
                {
                    List<Tuple<string, string>> subRegionPosition_Barcodes = new List<Tuple<string, string>>();
                    for (int slice = 0; slice < totalSlicePerSample; slice++)
                    {
                        if (!needFurtherTest)
                            break;
                        string well = string.Format("{0}{1:D2}", (char)('A' + rowIndex), startColumn + slice + 1);
                        //if (!IsValidBarcode(barcodesThisPlate[well]))
                        //    throw new Exception(string.Format("{0}处的条码:{1}非法！", well, barcodesThisPlate[well]));
                        bool bok = position_barcode.ContainsKey(well) && position_barcode[well] != "NOREAD" && position_barcode[well] != "NOTUBE";
                        if(!bok)
                        {
                            throw new Exception(string.Format("No valid barcode found for plate:{0} at:{1}", plateBarcode, well));
                        }
                        if (currentSampleID == neededSamples)
                        {
                            needFurtherTest = false;
                        }
                        currentSampleID++;
                        
                    }
                    
                    
                }
            }
        }

        private static string GetDstBarcodeFoler()
        {
            string exePath = Utility.GetExeFolder() + "Biobanking.exe";
            Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
            Utility.WriteExecuteResult(false, "result.txt");
            return config.AppSettings.Settings["DstBarcodeFolder"].Value;
           
        }

        /*
          */
    }
}
