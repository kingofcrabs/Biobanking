using Microsoft.Office.Interop.Excel;
using Settings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckPlateID
{
    class Program
    {
        static void Main(string[] args)
        {
            string exePath = Utility.GetExeFolder() + "Biobanking.exe";
            Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
            string conStr = config.AppSettings.Settings["sqlConnectionString"].Value;
            string dstBarcodeFolder = config.AppSettings.Settings["DstBarcodeFolder"].Value;
            SqlConnection con = new SqlConnection();
            con.ConnectionString = conStr; //"server=192.168.10.128;database=BioBank_tecan;uid=sa;pwd=wonders,1";
            con.Open();


            List<string> plateIDs = ReadPlateIDs(dstBarcodeFolder);
            Utility.WriteExecuteResult(false,"result.txt");
            foreach(string plateID in plateIDs)
            {
                Console.WriteLine(string.Format("Check plateID :{0}", plateID));
                string checkPlateIDStr = string.Format("select count(*) from biobank_tecan.dbo.v_getbox_sample_usespace where kjxxid = {0}", plateID);
                SqlCommand checkCommand = new SqlCommand(checkPlateIDStr, con);
                int count = (int)checkCommand.ExecuteScalar();
                if(count == 0)
                {
                    Console.WriteLine(string.Format("Cannot find plateID:{0} in database!", plateID));
                    Console.WriteLine(string.Format("Press any key to exit!"));
                    Console.ReadKey();
                    return;
                }
            }
            con.Close();
            Utility.WriteExecuteResult(true, "result.txt");
        }

        private static List<string> ReadPlateIDs(string folder)
        {
            List<string> plateIDs = new List<string>();
            var di = new DirectoryInfo(folder);
            var files = di.EnumerateFiles("*.csv").ToList();
            foreach(var file in files)
            {
                var sFile = file.FullName;
                var strs = File.ReadAllLines(sFile).ToList();
                string firstLine = strs[0];
                firstLine = firstLine.ToLower();
                var indexOfID = firstLine.IndexOf("id:");
                if (indexOfID == -1)
                    throw new Exception("cannot find Plate ID！");
                string plateBarcode = strs[0].Substring(indexOfID + 3);
                if (plateBarcode == "")
                    throw new Exception(string.Format("Plate ID is empty in file：{0}", sFile));
                plateIDs.Add(plateBarcode);
            }
            return plateIDs;
        }
    }
}
