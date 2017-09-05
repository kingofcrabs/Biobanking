using Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biobanking
{
    class LayoutChecker
    {
        LabwareSettings labwareSettings;
        PipettingSettings pipettingSettings;
        int sampleCnt;
        public LayoutChecker(LabwareSettings labwareSettings, PipettingSettings pipettingSettings,int sampleCnt)
        {
            this.labwareSettings = labwareSettings;
            this.pipettingSettings = pipettingSettings;
            this.sampleCnt = sampleCnt;
        }

        public void Check()
        {
            EVOScriptReader scriptReader = new EVOScriptReader();
            var dict = scriptReader.Read();
            //check start grid is 16 pos sample tubes
            int startGrid = labwareSettings.sourceLabwareStartGrid;
            int nGridCnt = labwareSettings.sourceLabwareGrids;
            for(int i = 0; i< nGridCnt; i++)
            {
                int grid = i + startGrid;
                if (!dict.ContainsKey(grid))
                    throw new Exception(string.Format("Source grid:{0} doesnot exist!", grid));
                if( dict[grid] != CarrierType.SampleTube16Pos)
                    throw new Exception(string.Format("Source grid:{0}'s labware is NOT 16 position sample tube!", grid));
            }


            //check dest grid is MicroPlate
            startGrid = labwareSettings.dstLabwareStartGrid;
            nGridCnt = labwareSettings.dstCarrierCnt;
            int gridsPerCarrier = labwareSettings.gridsPerCarrier;
            for(int i = 0; i< nGridCnt; i++)
            {
                int grid = startGrid + gridsPerCarrier * i;
                if (!dict.ContainsKey(grid))
                    throw new Exception(string.Format("Dest grid:{0} doesnot exist!", grid));
                if (dict[grid] != CarrierType.MicroPlate96Well)
                    throw new Exception(string.Format("Dest grid:{0}'s labware is NOT 96 microPlate!", grid));
            }

            nGridCnt = (sampleCnt + 15) / 16;
            //check transfer tubes
            if(pipettingSettings.needTransfer)
            {
                startGrid = labwareSettings.transferGrid;
                for (int i = 0; i < nGridCnt; i++)
                {
                    int grid = i + startGrid;
                    if (!dict.ContainsKey(grid))
                    {
                        throw new Exception(string.Format("Transfer grid:{0} doesnot exist!", grid));
                    }
                    if (dict[grid] != CarrierType.Eppendorf16Pos)
                    {
                        throw new Exception(string.Format("Transfer grid:{0}'s labware is NOT 16 position eppendorf sample tube!", grid));
                    }
                }
              
            }

            if (labwareSettings.buffyTransferGrid < labwareSettings.transferGrid)
                throw new Exception("Buffy transfer grid cannot < plasma transfer grid!");

            //check 
            if(pipettingSettings.dstbuffySlice == 2)
            {
                startGrid = labwareSettings.buffyTransferGrid;
                for (int i = 0; i < nGridCnt; i++)
                {
                    int grid = i + startGrid;
                    if (!dict.ContainsKey(grid))
                    {
                        throw new Exception(string.Format("Transfer grid for buffy:{0} doesnot exist!", grid));
                    }
                    if (dict[grid] != CarrierType.Eppendorf16Pos)
                    {
                        throw new Exception(string.Format("Transfer grid for buffy:{0}'s labware is NOT 16 position eppendorf sample tube!", grid));
                    }
                }
            }

        }
    }
}
