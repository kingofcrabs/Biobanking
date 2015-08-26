using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Biobanking
{
    public class RunResult
    {
        public int plasmaTotalSlice;
        public int buffySlice;
        public double plasmaVolume;
        public double buffyVolume;
        public List<int> plasmaRealSlices = new List<int>();
        
    }
}
