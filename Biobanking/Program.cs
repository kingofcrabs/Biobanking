using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace Biobanking
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            Console.WriteLine("Version is :" + stringRes.version);
            worklistGenerator2 generator = new worklistGenerator2();
            bool bok = false;
            //try
            {
                bok = generator.DoJob();
            }
            //catch(Exception ex)
            //{
            //    Utility.Write2File(Utility.GetOutputFolder() + "errMsg.txt", ex.Message + ex.StackTrace);
            //    log.Info(ex.Message);
            //}
            Utility.Write2File(Utility.GetOutputFolder() + "result.txt", bok.ToString());
        }

        

    }
}
