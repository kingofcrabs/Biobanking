using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;

namespace Monitor
{
    class RunResultReader
    {
        static RunResult runResult = null;
        static public RunResult Read()
        {
            //if (runResult != null)
            //    return runResult;
            string sResultFile = ConfigurationManager.AppSettings["runResultFile"];
            string sContent = File.ReadAllText(sResultFile, Encoding.Default);
            runResult = Utility.Deserialize<RunResult>(sContent);
            return runResult;
        }
    }


    public class RunResult
    {
        public int plasmaTotalSlice;
        public int buffySlice;
        public double plasmaVolume;
        public double buffyVolume;
        public List<int> plasmaRealSlices = new List<int>();
    }

}
