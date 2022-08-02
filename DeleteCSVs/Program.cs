using ConfigureTool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeleteCSVs
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var dict = ConfigFile.Read();
            var dstBarcodeFolder = dict["DstBarcodeFolder"];
            var csvFiles = Directory.EnumerateFiles(dstBarcodeFolder,"*.csv");
            foreach (var file in csvFiles)
                File.Delete(file);
            Console.WriteLine($"{csvFiles.Count()} files has been deleted");
        }
    }
}
