using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutomateBarcode.TissueManagement;



namespace AutomateBarcode
{
    class Global
    {
        static Global inst = null;
        public int plasmaSlice;
        public int buffySlice;
        public double plasmaVolume;
        public double buffyVolume;
        public List<string> expectedBarcodes = new List<string>();
        public List<string> srcBarcodes = new List<string>();
        public ResourcesPortTypeClient server = new ResourcesPortTypeClient();
        public Dictionary<int, int> smpPlasmaSlices = new Dictionary<int, int>();
        
        static public Global Instance
        {
            get
            {
                if (inst == null)
                    inst = new Global();
                return inst;
            }
        
        }

        internal void SetResult(RunResult runResult)
        {
            plasmaSlice = runResult.plasmaTotalSlice;
            buffySlice = runResult.buffySlice;
            plasmaVolume = runResult.plasmaVolume;
            buffyVolume = runResult.buffyVolume;
            int curSmpID = 1;
            foreach (var slice in runResult.plasmaRealSlices)
            {
                smpPlasmaSlices.Add(curSmpID++, slice);
            }
        }
    }
}
