using Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TubeChecker
{
    class GlobalVars
    {
        static GlobalVars instance = null;
        public static GlobalVars Instance
        {
            get
            {
                if (instance == null)
                    instance = new GlobalVars();
                return instance;
            }
        }
        private GlobalVars()
        {
            string sFileStruct = Settings.Utility.GetExeFolder() + "fileStruct.xml";
            if (File.Exists(sFileStruct))
            {
                var sContent = File.ReadAllText(sFileStruct);
                FileStruct = Settings.Utility.Deserialize<FileStruct>(sContent);
            }
            else
            {
                FileStruct = new FileStruct();
                Settings.Utility.SaveSettings(FileStruct, sFileStruct);
            }

        }
        public FileStruct FileStruct { get; set; }
    }
}
