using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biobanking
{

    public enum CarrierType
    {
        SampleTube16Pos = 0,
        Waste,
        Eppendorf16Pos,
        MicroPlate96Well,
        Other,  
    };

    public class EVOScriptReader
    {
        public string sScriptFile = ConfigurationManager.AppSettings["ScriptFile"];

        public Dictionary<int, CarrierType> Read()
        {
            if (!File.Exists(sScriptFile))
                throw new Exception("Cannot find script file at:" + sScriptFile);
            List<string> sGridDescriptions = new List<string>();
            List<string> sContents = File.ReadAllLines(sScriptFile).ToList();
            sGridDescriptions = sContents.Where(s => s.Contains("998")).ToList();
            return ParseAll(sGridDescriptions);
        }

        private Dictionary<int, CarrierType> ParseAll(List<string> sGridDescriptions)
        {
            Dictionary<int, CarrierType> label_basicDef_dict
                = new Dictionary<int, CarrierType>();

            int grid = 0;
            int maxGrid = Math.Min(sGridDescriptions.Count - 1, 69);
            for (int i = 0; i < maxGrid; i++)
            {
                string s = sGridDescriptions[i];
                if (s == "998;0;")
                    grid++;
                if (s == "998;0;" || s == "998;1;" || s == "998;4;0;System;")
                    continue;


                label_basicDef_dict.Add(grid, ParseType(s));
                grid++;
                i++;
            }
            return label_basicDef_dict;
        }

        private CarrierType ParseType(string sLabels)
        {
            if (sLabels.Contains("DiTi Waste"))
                return CarrierType.Waste;
            if (sLabels.Contains("Tube Eppendorf 16 Pos"))
                return CarrierType.Eppendorf16Pos;
            if (sLabels.Contains("Tube 16*"))
                return CarrierType.SampleTube16Pos;
            if (sLabels.Contains("96 Well"))
                return CarrierType.MicroPlate96Well;
            else
                return CarrierType.Other;
        }
    }
    

 
}
