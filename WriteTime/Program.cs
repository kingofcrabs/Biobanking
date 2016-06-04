using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Settings;
using System.IO;

namespace WriteTime
{
    class Program
    {
        static void Main(string[] args)
        {
            string sFile = Utility.GetOutputFolder() + "barcodeTime.txt";
            File.WriteAllText(sFile, DateTime.Now.ToString("hhmm"));
        }
    }
}
