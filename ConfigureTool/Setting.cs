using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigureTool
{
    class Setting
    {
        public Dictionary<string,string> Load(bool configSettings, bool labwareSettings, bool pipettingSettings)
        {

            if(configSettings)
            {
                return LoadConfigSettings();
            }
            
            throw new NotImplementedException(); 
        }

        private Dictionary<string, string> LoadConfigSettings()
        {
            return ConfigFile.Read();
        }

      
    }
}
