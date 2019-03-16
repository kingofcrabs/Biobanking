using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using System.Xml;

namespace Settings
{
    [Serializable]
    public class CalibrationItem
    {
        //public int tipVolume;
        public int volumeUL;
        public double height;
        public CalibrationItem(double h, int v)
        {
            volumeUL = v;
            height = h;
        }
        public CalibrationItem()
        {
        }
    }

    [Serializable]
    public class CalibrationItems
    {
        public CalibrationItems(List<CalibrationItem> items)
        {
            calibItems = new List<CalibrationItem>(items);
        }

        public CalibrationItems()
        {
            calibItems = new List<CalibrationItem>();
        }

        public List<CalibrationItem> calibItems { get; set; }
    }


    [Serializable]
    public class FileStruct
    {
        public int noIndex; //sample id;
        public int srcBarcodeIndex; // barcode index in source file, it is always 0
        public int dstBarcodeIndex; // column index in dst file
        //public FileStruct(int idIndex, int srcBarcodeIndex, int dstBarcodeIndex)
        //{
        //    noIndex = idIndex;
        //    this.srcBarcodeIndex = srcBarcodeIndex;
        //    this.dstBarcodeIndex = dstBarcodeIndex;
        //}
        public FileStruct()
        {
            noIndex = 0;
            srcBarcodeIndex = 0;
            dstBarcodeIndex = 2;
            dstPosition = 1;
        }



        public int dstPosition { get; set; }
    }
  
    //[Serializable]
    //public class TubeSetting
    //{
    //    public double r_mm;
    //    //public double msdZDistance;
    //    //public double msdStartPositionAboveBuffy;
    //    public TubeSetting()
    //    {
    //        r_mm = 5;
    //        //msdZDistance = 4;
    //        //msdStartPositionAboveBuffy = 1;
    //    }
    //}

    //[Serializable]
    //public class TubeSettings
    //{
    //    public List<TubeSetting> Settings;
    //    public int selectIndex;
    //    public TubeSettings()
    //    {
    //        Settings = new List<TubeSetting>();
    //        //Settings.Add(new TubeSetting());
    //        //selectIndex = 0;
    //    }
    //}

    [Serializable]
    public class PipettingSettings
    {
        public int buffyAspirateLayers;
        public int dstPlasmaSlice;
        public int dstbuffySlice;
        public int deltaXYForMSD;
        public int buffyVolume;
        public int safeDelta;
        public double buffySpeedFactor;
        public double plasmaGreedyVolume;
        public double minVolumeAsp;
        public int dstRedCellSlice;
        public double redCellGreedyVolume;
        public bool giveUpNotEnough;
        public double msdZDistance;
        public double msdStartPositionAboveBuffy;
        public bool onlyOneSlicePerLabware;
        public bool msdXYTogether;
        public int airGap;
        public int bottomOffset;
        public int maxVolumePerSlice;
        public int retractHeightcm;
        public double plasmaRatio;
        public bool buffyStandalone;
        public PipettingSettings()
        {
            msdXYTogether = false;
            buffyAspirateLayers = 6;
            dstPlasmaSlice = 5;
            dstbuffySlice = 1;
            deltaXYForMSD = 13;
            safeDelta = 2;
            buffySpeedFactor = 2.5;
            buffyVolume = 300;
            plasmaGreedyVolume = 0;
            dstRedCellSlice = 0;
            redCellGreedyVolume = 500;
            buffyStandalone = false;
            //redCellBottomHeight = 80; //8mm
            giveUpNotEnough = false;
            msdZDistance = 4;
            minVolumeAsp = 100;
            msdStartPositionAboveBuffy = 1;
            onlyOneSlicePerLabware = false;
            airGap = 70;
            bottomOffset = 388;
            retractHeightcm = 10;
            maxVolumePerSlice = 5000;
            plasmaRatio = 0.6666;
        }

        public int GetTotalSlice()
        {
            return dstbuffySlice + dstPlasmaSlice + dstRedCellSlice;
        }
    }


    [Serializable]
    public class LabwareSettings
    {
        public int tipCount;
        public int dstLabwareRows;
        public int dstLabwareColumns;
        public int dstLabwareStartGrid;
        
        public int sourceWells;
        public int sourceLabwareGrids;
        public int sourceLabwareStartGrid;
        public int wasteGrid;
        public int dstCarrierCnt;
        
        public int gridsPerCarrier;
        public int sitesPerCarrier;
        public int wasteTroughGrid;
        public LabwareSettings()
        {
            sourceLabwareStartGrid = 1;
            dstLabwareStartGrid = 15;
            
            sourceLabwareGrids = 2;
            tipCount = 2;
            sourceWells = 10;
            wasteGrid = 21;
            dstCarrierCnt = 1;
            
            dstLabwareRows = 8;
            dstLabwareColumns = 6;
            gridsPerCarrier = 1;
            sitesPerCarrier = 1;
            wasteTroughGrid = 18;
        }

   
    }


    public class SettingsHelper
    {

        public bool IsValidSetting(LabwareSettings labwareSettings, PipettingSettings pipettingSettings, ref string errMsg)
        {
            if (labwareSettings.sourceLabwareGrids > labwareSettings.dstLabwareStartGrid)
            {
                errMsg = "plasma start grid must > source sample start grid";
                return false;
            }

            //if (labwareSettings.dstBuffyStartGrid < labwareSettings.dstLabwareStartGrid)
            //{
            //    errMsg = "buffy start grid must > plasma start grid";
            //    return false;
            //}

            //if (labwareSettings.dstRedCellStartGrid < labwareSettings.dstBuffyStartGrid)
            //{
            //    errMsg = "red cell start grid must > buffy start grid";
            //    return false;
            //}

            //if (pipettingSettings.dstPlasmaSlice == 0)
            //{
            //    errMsg = "destination plasma slice must > 0";
            //    return false;
            //}

            int possibleGrids = labwareSettings.dstLabwareStartGrid - labwareSettings.sourceLabwareStartGrid;
            if (labwareSettings.dstLabwareStartGrid < labwareSettings.sourceLabwareStartGrid + labwareSettings.sourceLabwareGrids)
            {
                errMsg = string.Format("there is only {0} grids between source sample start grid & plasma start grid, but there is {1} racks of source racks!", possibleGrids, labwareSettings.sourceLabwareGrids);
                return false;
            }


            //如果冻存管载架只有一列位置，那么Region的列数plasma+ buffy数量决定，
            //如果冻存管载架有多列位置N，那么Region的列数由N决定
            int columnsPerRegion = labwareSettings.dstLabwareColumns;
            int totalSlice = pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice;
            if (columnsPerRegion == 1)
                columnsPerRegion = totalSlice;

            if (totalSlice > columnsPerRegion)
            {
                errMsg = string.Format("there is only {0} columns in the region, but there is {1} slice of plasma,buffy & red cell to dispense!", columnsPerRegion, totalSlice);
                return false;
            }
            return true;
        }



        string GetExeFolder()
        {
            string s = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return s;
        }



        public void SaveSettings(PipettingSettings settings)
        {

            XmlSerializer xs = new XmlSerializer(typeof(PipettingSettings));
            string sFile = GetExeFolder() + "\\" + stringRes.pipettingSettingFileName;
            if (File.Exists(sFile))
                File.Delete(sFile);
            Stream stream = new FileStream(sFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
            xs.Serialize(stream, settings);
            stream.Close();
        }

        private void SaveSettings(LabwareSettings labwareSettings)
        {
            XmlSerializer xs = new XmlSerializer(typeof(LabwareSettings));
            string sFile = GetExeFolder() + "\\" + stringRes.labwareSettingFileName;
            if (File.Exists(sFile))
                File.Delete(sFile);
            Stream stream = new FileStream(sFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
            xs.Serialize(stream, labwareSettings);
            stream.Close();

        }

        public void LoadSettings(ref PipettingSettings pipettingSettings, ref LabwareSettings labwareSettings)
        {
            XmlSerializer xs = new XmlSerializer(typeof(PipettingSettings));
            string sFile = GetExeFolder() + "\\" + stringRes.pipettingSettingFileName;
            if (!File.Exists(sFile))
            {
                SaveSettings(pipettingSettings);
                return;
            }
            Stream stream = new FileStream(sFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            pipettingSettings = xs.Deserialize(stream) as PipettingSettings;
            stream.Close();

            xs = new XmlSerializer(typeof(LabwareSettings));
            sFile = GetExeFolder() + "\\" + stringRes.labwareSettingFileName;
            if (!File.Exists(sFile))
            {
                SaveSettings(labwareSettings);
                return;
            }
            stream = new FileStream(sFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            labwareSettings = xs.Deserialize(stream) as LabwareSettings;
            stream.Close();


        }


    }
}
