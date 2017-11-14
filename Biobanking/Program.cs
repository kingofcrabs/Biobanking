using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Settings;
using System.IO;

namespace Biobanking
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            Console.WriteLine("Version is :" + stringRes.version);
            worklistGenerator generator = new worklistGenerator();
            bool bok = false;
#if DEBUG
#else
            try
#endif
            {
                Utility.Write2File(Utility.GetOutputFolder() + "result.txt", bok.ToString());
                if(args.Length != 0)
                {
                    CopyConfigurationFiles(args[0]);
                }

                bok = generator.DoJob();
            }
#if DEBUG
#else
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                Console.WriteLine("Press any key to exit!");
                Console.ReadKey();
                Utility.Write2File(Utility.GetOutputFolder() + "errMsg.txt", ex.Message + ex.StackTrace);
                log.Info(ex.Message);
            }
#endif
            Utility.Write2File(Utility.GetOutputFolder() + "result.txt", bok.ToString());
        }

        private static void CopyConfigurationFiles(string srcFolder)
        {
            log.InfoFormat("source folder is: {0}", srcFolder);
            string sDestFolder = Utility.GetExeFolder();
            DirectoryInfo di = new DirectoryInfo(srcFolder);
            var fileInfos = di.GetFiles("*.xml");
            foreach(var fileInfo in fileInfos)
            {
                string dstFile = sDestFolder + fileInfo.Name;
                File.Copy(fileInfo.FullName, dstFile, true);
            }
        }
    }
}
