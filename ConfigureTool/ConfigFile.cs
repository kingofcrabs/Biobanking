using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Settings;

namespace ConfigureTool
{
    class ConfigFile
    {
        static public Dictionary<string,string>  Read()
        {
            string sFolder = Utility.GetExeFolder();
            string exePath = sFolder + "Biobanking.exe";
            Configuration config =  ConfigurationManager.OpenExeConfiguration(exePath);
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach( string key in config.AppSettings.Settings.AllKeys)
            {
                dict.Add(key, config.AppSettings.Settings[key].Value);
            }
            return dict;

        }

        static public void  Save(Dictionary<string, string> collection)
        {
            string sFolder = Utility.GetExeFolder();
            string exePath = sFolder + "Biobanking.exe.config";
            Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
            config.AppSettings.Settings.Clear();
            foreach(var pair in collection)
            {
                config.AppSettings.Settings.Add(pair.Key,pair.Value);
            }
            
            config.Save(ConfigurationSaveMode.Modified);
        }
    }
}
