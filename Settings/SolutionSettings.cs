﻿using System;
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
    public class TubeSetting
    {
        public double r_mm;
        public double msdZDistance;
        public double msdStartPositionAboveBuffy;
        public TubeSetting()
        {
            r_mm = 5;
            msdZDistance = 4;
            msdStartPositionAboveBuffy = 1;
        }
    }

    [Serializable]
    public class TubeSettings
    {
        public List<TubeSetting> Settings;
        public int selectIndex;
        public TubeSettings()
        {
            Settings = new List<TubeSetting>();
            //Settings.Add(new TubeSetting());
            //selectIndex = 0;
        }

    }

    [Serializable]
    public class PipettingSettings
    {
        public int buffyAspirateLayers;
        public double r_mm;
        public int dstPlasmaSlice;
        public int dstbuffySlice;
        public int deltaXYForMSD;
        public int buffyVolume;
        public int safeDelta;
        public double buffySpeedFactor;
        public double plasmaGreedyVolume;
        public int dstRedCellSlice;
        public double redCellGreedyVolume;
        public double redCellBottomHeight;
        public bool giveUpNotEnough;
        public double msdZDistance;
        public double msdStartPositionAboveBuffy;
        public bool onlyOneSlicePerRegion;
        public int airGap;
        public PipettingSettings()
        {
            buffyAspirateLayers = 6;
            dstPlasmaSlice = 5;
            dstbuffySlice = 1;
            deltaXYForMSD = 13;
            safeDelta = 2;
            r_mm = 5.5;
            buffySpeedFactor = 2.5;
            buffyVolume = 300;
            plasmaGreedyVolume = 0;
            dstRedCellSlice = 0;
            redCellGreedyVolume = 300;
            redCellBottomHeight = 80; //8mm
            giveUpNotEnough = false;
            msdZDistance = 4;
            msdStartPositionAboveBuffy = 1;
            onlyOneSlicePerRegion = false;
            airGap = 70;
        }
    }


    [Serializable]
    public class LabwareSettings
    {
        public int tipCount;
        public int dstLabwareRows;
        public int dstLabwareStartGrid;
        public int dstLabwareColumns;
        public int sourceWells;
        public int sourceLabwareStartGrid;
        public int sourceLabwareGrids;
        public int wasteGrid;
        public int regions;
        public int gridsPerRegion;
        public int sitesPerRegion;

        public LabwareSettings()
        {
            sourceLabwareStartGrid = 1;
            dstLabwareStartGrid = 3;
            sourceLabwareGrids = 2;
            tipCount = 2;
            sourceWells = 10;
            wasteGrid = 21;
            regions = 1;
            dstLabwareRows = 8;
            dstLabwareColumns = 6;
            gridsPerRegion = 1;
            sitesPerRegion = 1;
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
            int totalSlice = pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice + pipettingSettings.dstRedCellSlice;
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

            Stream stream = new FileStream(sFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
            xs.Serialize(stream, settings);
            stream.Close();
        }

        private void SaveSettings(LabwareSettings labwareSettings)
        {
            XmlSerializer xs = new XmlSerializer(typeof(LabwareSettings));
            string sFile = GetExeFolder() + "\\" + stringRes.labwareSettingFileName;

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