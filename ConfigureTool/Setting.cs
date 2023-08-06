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
    public class Setting
    {
       
        string sLabwareSettingFileName = Utility.GetExeFolder() + "\\labwareSettings.xml";
        string sPipettingFileName = Utility.GetExeFolder() + "\\pipettingSettings.xml";
        PipettingSettings pipettingSettings = new PipettingSettings();
        LabwareSettings labwareSettings = new LabwareSettings();
        public Dictionary<string,string> Load(bool isConfigSettings, bool isLabwareSettings, bool isPipettingSettings)
        {
          
            if (isConfigSettings)
            {
                return LoadConfigSettings();
            }
          
            string s = File.ReadAllText(sPipettingFileName);
            pipettingSettings = Utility.Deserialize<PipettingSettings>(s);
            s = File.ReadAllText(sLabwareSettingFileName);
            labwareSettings = Utility.Deserialize<LabwareSettings>(s);
            if (isLabwareSettings)
            {
                return GetSetting(labwareSettings);
            }
            if(isPipettingSettings)
            {
                return GetSetting(pipettingSettings);
            }
            throw new Exception("unsupported settings");
        }


        public void Save(bool isConfigSettings, bool isLabwareSettings, bool isPipettingSettings,Dictionary<string,string> settings)
        {
            if(isConfigSettings)
            {
                SaveConfigSettings(settings);
            }
            else if(isPipettingSettings)
            {
                SetSetting(pipettingSettings, settings);
                Utility.SaveSettings(pipettingSettings, sPipettingFileName);
            }
            else if(isLabwareSettings)
            {
                SetSetting(labwareSettings, settings);
                Utility.SaveSettings(labwareSettings, sLabwareSettingFileName);
            }
            else
                throw new Exception("config file not supported!");
        }
        internal void SetSetting(Object obj, Dictionary<string, string> settings)
        {
            var fieldsType = obj.GetType();
            Dictionary<string, string> dict = new Dictionary<string, string>();

            FieldInfo[] fields = fieldsType.GetFields(BindingFlags.Public
           | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                //settings[fields[i].SetValue()
                string key = fields[i].Name;
                object subObj = ParseString(settings[key]);
                fields[i].SetValue(obj, subObj);
            }
        }


        object ParseString(string s)
        {
            bool result = false;
            double dVal = 0;
            int val = 0;
            if (bool.TryParse(s, out result))
            {
                return result;
            }
            else if (int.TryParse(s, out val))
            {
                return val;
            }
            else if (double.TryParse(s, out dVal))
            {
                return dVal;
            }
            return s;
        }
           

        internal Dictionary<string, string> GetSetting(Object settings)
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

        internal Dictionary<string, string> LoadConfigSettings()
        {
            return ConfigFile.Read();
        }



        internal void SaveConfigSettings(Dictionary<string, string> settings)
        {
            ConfigFile.Save(settings);
        }
      
    }
}
