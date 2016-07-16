using Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConfigureTool
{
    class Setting
    {
        public Dictionary<string,string> Load(bool configSettings, bool isLabwareSettings, bool isPipettingSettings)
        {
          
            if (configSettings)
            {
                return LoadConfigSettings();
            }
            string sLabwareSettingFileName;
            string sPipettingFileName;
            string xmlFolder = Utility.GetExeFolder();
            sLabwareSettingFileName = xmlFolder + "\\labwareSettings.xml";
            sPipettingFileName = xmlFolder + "\\pipettingSettings.xml";
            string s = File.ReadAllText(sPipettingFileName);
            var pipettingSettings = Utility.Deserialize<PipettingSettings>(s);
            s = File.ReadAllText(sLabwareSettingFileName);
            var labwareSettings = Utility.Deserialize<LabwareSettings>(s);
            if (isLabwareSettings)
            {
                return GetSetting(labwareSettings);
            }
            if(isPipettingSettings)
            {
                return GetSetting(pipettingSettings);
            }
            throw new Exception("不支持的设置！");
        }

        private Dictionary<string, string> GetSetting(Object settings)
        {
            var fieldsType = settings.GetType();
            Dictionary<string, string> dict = new Dictionary<string, string>();

            FieldInfo[] fields = fieldsType.GetFields(BindingFlags.Public
           | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                dict.Add(fields[i].Name, fields[i].GetValue(settings).ToString());
            }
            return dict;
        }

        private Dictionary<string, string> LoadConfigSettings()
        {
            return ConfigFile.Read();
        }

        public void SaveConfigSettings(Dictionary<string,string> settings)
        {
            ConfigFile.Save(settings);
        }
      
    }
}
