using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AddReagent
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter the sample count.");
            string sCnt = Console.ReadLine();
            int cnt = int.Parse(sCnt);
            string srcLabware = ConfigurationManager.AppSettings["sourceLabware"];
            int dstLabwareWellCnt = int.Parse(ConfigurationManager.AppSettings["dstLabwareWellCnt"]);
            List<int> wells = new List<int>();
            for (int i = 0; i < cnt; i++)
                wells.Add(i + 1);
            int batchID = 1;
            int vol = int.Parse(ConfigurationManager.AppSettings["volume"]);
            List<string> strs = new List<string>();
            while(wells.Count > 0)
            {
                var batchWells =  wells.Take(dstLabwareWellCnt).ToList();
                string dstLabware = string.Format("dst{0}",batchID++);
                wells = wells.Skip(batchWells.Count).ToList();
                strs.Add(GenerateRCommand(dstLabwareWellCnt,srcLabware, dstLabware, batchWells, vol));
            }

            string sFile = GetOutputFolder() + "r.gwl";
            File.WriteAllLines(sFile, strs);
        }

        static public string GetExeFolder()
        {
            string s = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return s;
        }

        static public string GetExeParentFolder()
        {
            string s = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            int index = s.LastIndexOf("\\");
            return s.Substring(0, index) + "\\";
        }

     
        static public string GetOutputFolder()
        {
            string folder = GetExeParentFolder() + "\\Output\\";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }


        private static string GenerateRCommand(int dstLabwareWellCnt,string srcLabel, string destLabel, List<int> dstWells, int vol)
        {
            //R;AspirateParameters;DispenseParameters;Volume;LiquidClass;NoOfDitiRe
            //uses;NoOfMultiDisp;Direction[;ExcludeDestWell]*
            //AspirateParameters =
            //SrcRackLabel; SrcRackID; SrcRackType; SrcPosStart; SrcPosEnd;
            //and
            //DispenseParameters =
            //DestRackLabel; DestRackID; DestRackType; DestPosStart; DestPosEnd;
            
            string aspParameters = string.Format("{0};;;{1};{2};", srcLabel, 1, 8);
            int maxWell = dstWells.Max();
            int minWell = dstWells.Min();
            maxWell = (maxWell - 1) % dstLabwareWellCnt + 1;
            minWell = (minWell - 1) % dstLabwareWellCnt + 1;

            string exclude = "";
            for (int wellID = minWell; wellID < maxWell; wellID++)
            {
                if (!dstWells.Contains(wellID))
                {
                    exclude += ";";
                    exclude += wellID;
                }
            }
            string dspParameters = string.Format("{0};;;{1};{2};", destLabel, minWell, maxWell);
            //R;AspirateParameters;DispenseParameters;Volume;LiquidClass;NoOfDitiRe
            //uses;NoOfMultiDisp;Direction[;ExcludeDestWell]*
            int noOfMultiDisp = 0;
            if (vol <= 50)
            {
                noOfMultiDisp = 12;
            }
            else if (vol <= 110)
            {
                noOfMultiDisp = 6;
            }
            else if (vol <= 200)
            {
                noOfMultiDisp = 4;
            }
            else if (vol <= 300)
            {
                noOfMultiDisp = 3;
            }
            else
            {
                noOfMultiDisp = 2;
            }


            string rCommand = string.Format("R;{0}{1}{2};{3};{4};{5};{6}{7}",
                aspParameters,
                dspParameters,
                vol,
                "",
                1,
                noOfMultiDisp,
                0,
                exclude);

            return rCommand;
        }
    }
}
