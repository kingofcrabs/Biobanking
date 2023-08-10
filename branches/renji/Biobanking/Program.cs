using System;
using System.IO;
using System.Reflection;
using log4net;
using Settings;

namespace Biobanking
{
	internal class Program
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static void Main(string[] args)
		{
			Console.WriteLine("Version is :" + stringRes.version);
			worklistGenerator worklistGenerator2 = new worklistGenerator();
			bool flag = false;
			try
			{
				Utility.Write2File(Utility.GetOutputFolder() + "result.txt", flag.ToString());
				if (args.Length != 0)
				{
					CopyConfigurationFiles(args[0]);
				}
				flag = worklistGenerator2.DoJob();
			}
			catch (Exception ex)
			{
				flag = false;
				Console.WriteLine(ex.Message + ex.StackTrace);
				Console.WriteLine("Press any key to exit!");
				Console.ReadKey();
				Utility.Write2File(Utility.GetOutputFolder() + "errMsg.txt", ex.Message + ex.StackTrace);
				log.Info(ex.Message);
			}
			Utility.Write2File(Utility.GetOutputFolder() + "result.txt", flag.ToString());
		}

		private static void CopyConfigurationFiles(string srcFolder)
		{
			log.InfoFormat("source folder is: {0}", srcFolder);
			string exeFolder = Utility.GetExeFolder();
			FileInfo[] files = new DirectoryInfo(srcFolder).GetFiles("*.xml");
			foreach (FileInfo fileInfo in files)
			{
				string destFileName = exeFolder + fileInfo.Name;
				File.Copy(fileInfo.FullName, destFileName, overwrite: true);
			}
		}
	}
}
