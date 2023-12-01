using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace SampleInfo
{

    [Serializable]
    public class SolutionSettings
    {
        private PipettingSettings pipettingSettings;
        private LabwareSettings labwareSettings;
        public SolutionSettings()
        {
            pipettingSettings = new PipettingSettings();
            labwareSettings = new LabwareSettings();
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
        //public int mixTimes;
        public int safeDelta;
        public double buffySpeedFactor;
        public double plasmaGreedyVolume;
        public int dstRedCellSlice;
        public double redCellGreedyVolume;
        public double redCellBottomHeight;
        public bool giveUpNotEnough;
        public double msdZDistance;
        public double msdStartPositionAboveBuffy;
        public PipettingSettings()
        {
            buffyAspirateLayers = 6;
            dstPlasmaSlice = 5;
            dstbuffySlice = 1;
            deltaXYForMSD = 13;
            //mixTimes = 2;
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
            
        }
    }
    [Serializable]
    public class LabwareSettings
    {
        public int tipCount;
        public int dstRowCount;
        public int dstPlasmaStartGrid;
        public int dstBuffyStartGrid;
        public int dstRedCellStartGrid;

        public int sourceWells;
        public int sourceLabwareStartGrid;
        public int sourceLabwareGrids;
        public int wasteGrid;
        public int regions;
        public LabwareSettings()
        {
            sourceLabwareStartGrid = 1;
            dstPlasmaStartGrid = 3;
            dstBuffyStartGrid = 10;
            sourceLabwareGrids = 2;
            tipCount = 2;
            sourceWells = 10;
            wasteGrid = 21;
            dstRowCount = 16;
            dstRedCellStartGrid = 5;
            regions = 1;
        }
    }
}
