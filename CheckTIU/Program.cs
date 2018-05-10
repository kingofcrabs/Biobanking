using Settings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckTIU
{
    class Program
    {
        static void Main(string[] args)
        {

            string exePath = Utility.GetExeFolder() + "Biobanking.exe";
            Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
            bool unitIsMM = bool.Parse(config.AppSettings.Settings["UnitIsMM"].Value);

            string tiuFile = ConfigurationManager.AppSettings["TIUFile"];
            string lastLine = File.ReadAllLines(tiuFile).Last();
            string[] vals = lastLine.Split(',');
            double ratio = 1;
            if (!unitIsMM)
                ratio = 0.1;

            double Z2 = double.Parse(vals[2]) * ratio;
            File.WriteAllText(Utility.GetOutputFolder() + "Z2Valid.txt", (Z2 > 10).ToString());

        }
    }
}
