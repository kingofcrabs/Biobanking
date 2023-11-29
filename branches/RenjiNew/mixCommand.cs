using Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biobanking
{
    class mixCommand:worklistCommand
    {
        private int mixVolume = 850;
        private int mixTimes = 3;
       
        public List<string> GenerateMixForBatch(int rackIndex, int sampleIndexInRack, List<int> thisBatchTipVolumes)
        {
            List<string> strs = new List<string>()
			{
				GetComment("start pipetting"),
				"W;"
			};
            int maxVolume = thisBatchTipVolumes.Max();
            int slices = (maxVolume - 1000) / 1000 + 1;
            int finishedSampleCnt = GetGlobalSampleIndex(rackIndex, sampleIndexInRack);
            for (int i = 0; i < slices; i++)
            {
                strs.AddRange(GenerateMixSlice(thisBatchTipVolumes, i, finishedSampleCnt, labwareSettings.sourceLabwareStartGrid, false));
            }
            strs.AddRange(GenerateMixSlice(thisBatchTipVolumes, slices - 1, finishedSampleCnt, labwareSettings.sourceLabwareStartGrid, true));
            return strs;
        }
        private IEnumerable<string> GenerateMixSlice(List<int> thisBatchTipVolumes, int sliceIndex, int finishedSampleCnt, int srcGrid, bool mixAtBottom)
        {
            List<int> nums = new List<int>();
            List<string> strs = new List<string>();
            
            while (finishedSampleCnt >= 16)
            {
                finishedSampleCnt = finishedSampleCnt - 16;
            }
            List<POINT> wells = PositionGenerator.GetWells(finishedSampleCnt, thisBatchTipVolumes.Count, 1, 16);
            nums.Clear();
            for (int i = 0; i < thisBatchTipVolumes.Count; i++)
            {
                int vol = (thisBatchTipVolumes[i] > (sliceIndex + 1) * mixVolume ? mixVolume : 0);
                if (mixAtBottom)
                {
                    vol = Math.Min(mixVolume, thisBatchTipVolumes[i]);
                }
                nums.Add(vol);
            }
            List<string> strs1 = new List<string>();
            string str = GenerateAspirateCommand(wells, nums, "MixBetter", srcGrid, 0, 1, 16);
            string str1 = GenerateDispenseCommand(wells, nums, "MixBetter", srcGrid, 0, 1, 16, 0);
            string str2 = GenerateMixCommand(wells, nums, "MixAtTop", srcGrid, 0, 1, 16, mixTimes, 0);
            string str3 = GenerateMixCommand(wells, nums, "MixAtBottom", srcGrid, 0, 1, 16, mixTimes, 0);
            if (!mixAtBottom)
            {
                strs1.Add(str);
                strs1.Add(str1);
                strs1.Add(str2);
            }
            else
            {
                strs1.Add(str3);
            }
            return strs1;
        }

        protected string GenerateAspirateCommand(List<POINT> wells, List<int> volumes, string liquidClass, int gridPos, int site, int width, int height)
        {
            return this.GenerateAspirateOrDispenseCommand(wells, volumes, liquidClass, gridPos, site, width, height, true, 0);
        }

        List<double> Convert2DoubleList(List<int> vols)
        {
            List<double> dblVolumes = new List<double>();
            vols.ForEach(x => dblVolumes.Add(x));
            return dblVolumes;
        }
        private string GenerateAspirateOrDispenseCommand(List<POINT> wells, List<int> volumes, string liquidClass, int gridPos, int site, int width, int height, bool aspirate, int startTip = 0)
        {
            int tipSelection = 0;

            tipSelection = GetTipSelection(Convert2DoubleList(volumes));
            List<POINT> pOINTs = new List<POINT>();
            for (int i = 0; i < wells.Count; i++)
            {
                if (volumes[i + startTip] != 0)
                {
                    pOINTs.Add(wells[i]);
                }
            }
            string str = "";
            for (int j = 0; j < 12; j++)
            {
                string str1 = "";
                str1 = (j >= volumes.Count ? "0," : string.Format("\"{0}\",", volumes[j]));
                str = string.Concat(str, str1);
            }
            string wellSelection = GetWellSelection(width, height, pOINTs);
            string str2 = (aspirate ? "Aspirate" : "Dispense");
            object[] objArray = new object[] { str2, tipSelection, liquidClass, str, gridPos, site, wellSelection };
            return string.Format("B;{0}({1},\"{2}\",{3}{4},{5},1,\"{6}\", 0, 0);", objArray);
        }

        protected string GenerateDispenseCommand(List<POINT> wells, List<int> volumes, string liquidClass, int gridPos, int site, int width, int height, int startTip = 0)
        {
            return this.GenerateAspirateOrDispenseCommand(wells, volumes, liquidClass, gridPos, site, width, height, false, startTip);
        }

        protected string GenerateMixCommand(List<POINT> wells, List<int> volumes, string liquidClass, int gridPos, int site, int width, int height, int mixTimes, int startTip = 0)
        {
            int tipSelection = 0;
            tipSelection = GetTipSelection(Convert2DoubleList(volumes));
            List<POINT> pOINTs = new List<POINT>();
            for (int i = 0; i < wells.Count; i++)
            {
                if (volumes[i + startTip] != 0)
                {
                    pOINTs.Add(wells[i]);
                }
            }
            string str = "";
            for (int j = 0; j < 12; j++)
            {
                string str1 = "";
                str1 = (j >= volumes.Count ? "0," : string.Format("\"{0}\",", volumes[j]));
                str = string.Concat(str, str1);
            }
            string wellSelection = GetWellSelection(width, height, pOINTs);
            object[] objArray = new object[] { "Mix", tipSelection, liquidClass, str, gridPos, site, wellSelection, mixTimes };
            return string.Format("B;{0}({1},\"{2}\",{3}{4},{5},1,\"{6}\",{7}, 0, 0);", objArray);
        }
    }
}
