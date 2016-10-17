using System.Collections.Generic;

namespace Biobanking
{
    public class RunResult
    {
        public int buffySlice;
        public int buffyVolume;
        public double plasmaVolume;
        public int plasmaTotalSlice;
        public List<int> plasmaRealSlices;
        public RunResult()
        {
            plasmaRealSlices = new List<int>();
        }
    }
}