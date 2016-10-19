using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Configuration;
using System.IO;

namespace Monitor
{
    class Helper
    {
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

        static public string GetDataFolder()
        {
            return GetExeFolder() + "\\Data\\";
        }

        static public int ReadSampleCount()
        {
            string sCountFilePath = ConfigurationManager.AppSettings["ReportFolder"] + "SampleCount.txt";
            using (StreamReader sr = new StreamReader(sCountFilePath))
            {
                string s = sr.ReadLine();
                return int.Parse(s);
            }
        }
    }
}
