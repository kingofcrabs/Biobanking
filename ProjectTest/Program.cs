using System;
using Biobanking;
using System.Collections.Generic;
using ConfigureTool;
using System.IO;
using Settings;
using System.Diagnostics;
using System.Security.Cryptography;


namespace ProjectTest
{
    class Program
    {
       static void Main(string[] args)
        {
            string path = System.AppDomain.CurrentDomain.BaseDirectory;
            for (int j = 0; j < 4; ++j)
            {
                int i = path.LastIndexOf("\\");
                string str = path.Substring(0, i);
                path = str;
            }

           
            Console.WriteLine(path);
            string SourceFilePath = path + "\\ProjectTest\\depends\\biobanking\\TestCaseSet.txt";
            //string SourceFilePath = @"C:\biobanking\TestCaseSet.txt";
            Dictionary<string,bool> testResult = new Dictionary<string,bool>();
            StreamReader sr = new StreamReader(SourceFilePath);
            string s = sr.ReadLine();
            s = sr.ReadLine();
            while (s != null)
            {
                testResult.Add(s, TestMethod(s,path));
                s = sr.ReadLine();               
            }
            Console.ReadKey();
        }

        static private bool TestMethod(string config, string path)
        {
            ConfigureToolSettings(config,path);
            RunBiobancking(config,path);
            bool testCaseResult = CompareWorklist(config,path);
            Console.WriteLine(testCaseResult);
            //Console.ReadKey();
            return testCaseResult;
        }

        static private bool CompareWorklist(string config,string basePath)
        {
            string ExpectFilePath = basePath + "\\ProjectTest\\depends\\biobanking\\output\\" + config;
            //string ExpectFilePath = @"C:\biobanking\output\" + config;
            string RealFilePath = basePath +"\\Biobanking\\bin\\Output";

            for (int i = 1; i < 7; ++i)
            {
                string ExpectOutputPath = ExpectFilePath + "\\srcRack" + i.ToString() + "\\";
                string RealOutputPath = RealFilePath + "\\srcRack" + i.ToString() + "\\";
                DirectoryInfo dir = new DirectoryInfo(ExpectOutputPath);
                if (Directory.Exists(ExpectOutputPath))
                {
                    foreach (FileInfo dChild in dir.GetFiles("worklist*"))
                    {
                        string WorkListFileName = dChild.FullName;
                        bool valid = isValidFileContent(dChild.FullName, RealOutputPath + dChild.Name);
                        if (!valid)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            return true;
        }


        public static bool isValidFileContent(string filePath1, string filePath2)
        {
            //创建一个哈希算法对象 
            using (HashAlgorithm hash = HashAlgorithm.Create())
            {
                using (FileStream file1 = new FileStream(filePath1, FileMode.Open), file2 = new FileStream(filePath2, FileMode.Open))
                {
                    byte[] hashByte1 = hash.ComputeHash(file1);//哈希算法根据文本得到哈希码的字节数组 
                    byte[] hashByte2 = hash.ComputeHash(file2);
                    string str1 = BitConverter.ToString(hashByte1);//将字节数组装换为字符串 
                    string str2 = BitConverter.ToString(hashByte2);
                    return (str1 == str2);//比较哈希码 
                }
            }
        }

        static private void RunBiobancking(string config, string basePath)
        {
            Process p = new Process();
            p.StartInfo.FileName = basePath + "\\Biobanking\\bin\\Debug\\Biobanking.exe";
            //p.StartInfo.FileName = "E:\\projects\\biobanking\\trunk\\Biobanking\\bin\\Debug\\Biobanking.exe";
            p.Start();
            p.WaitForExit();

            //string ExpectFilePath = @"C:\biobanking\output\" + config;
            //string RealFilePath = @"E:\projects\biobanking\trunk\Biobanking\bin\Output";

            //for (int i = 1; i < 7; ++i)
            //{
            //    string ExpectOutputPath = ExpectFilePath + "\\srcRack" + i.ToString();
            //    string RealOutputPath = RealFilePath + "\\srcRack" + i.ToString();
            //    DirectoryInfo di = new DirectoryInfo(RealOutputPath);
            //    if (Directory.Exists(RealOutputPath))
            //    {
            //        CopyOldLabFilesToNewLab(RealOutputPath, ExpectOutputPath);
            //        di.Delete(true);
            //    }
            //}
        }

      
        static private void ConfigureToolSettings(string config, string sourcePath)
        {
            Dictionary<string, string> currentSettings = new Dictionary<string, string>();
            Setting settings = new Setting();
            currentSettings = settings.LoadConfigSettings();
            if (config.Substring(0, 1) == "b")
            {
                currentSettings["2DBarcodeVendor"] = "baiquan";
            }
            currentSettings["reportPath"] = sourcePath + "\\ProjectTest\\depends\\biobanking\\data" + config.Substring(10, 2) + ".txt";
            //currentSettings["reportPath"] = @"C:\biobanking\data" + config.Substring(10, 2) + ".txt";
            settings.SaveConfigSettings(currentSettings);

            string sLabwareSettingFileName = sourcePath + "\\Biobanking\\bin\\Debug\\labwareSettings.xml";
            //string sLabwareSettingFileName = "E:\\projects\\biobanking\\trunk\\Biobanking\\bin\\Debug\\labwareSettings.xml";
            string s = File.ReadAllText(sLabwareSettingFileName);
            LabwareSettings labwareSettings = new LabwareSettings();
            labwareSettings = Utility.Deserialize<LabwareSettings>(s);
            currentSettings = settings.GetSetting(labwareSettings);
            currentSettings["tipCount"] = config.Substring(1,1);
            settings.SetSetting(labwareSettings, currentSettings);
            Utility.SaveSettings(labwareSettings, sLabwareSettingFileName);


            string sPipettingFileName = sourcePath + "\\Biobanking\\bin\\Debug\\pipettingSettings.xml";
            s = File.ReadAllText(sPipettingFileName);
            PipettingSettings pipettingSettings = new PipettingSettings();
            pipettingSettings = Utility.Deserialize<PipettingSettings>(s);
            currentSettings = settings.GetSetting(pipettingSettings);
            currentSettings["dstPlasmaSlice"] = config.Substring(2, 1);
            currentSettings["dstbuffySlice"] = config.Substring(3, 1); 
            currentSettings["buffyVolume"] = config.Substring(4, 3); 
            currentSettings["plasmaGreedyVolume"] = config.Substring(7, 3);
            currentSettings["buffyStandalone"] = Convert.ToBoolean(Convert.ToInt16(config.Substring(12, 1))) ? "True" : "False";
            settings.SetSetting(pipettingSettings, currentSettings);
            Utility.SaveSettings(pipettingSettings, sPipettingFileName);


        }


      
        /// <summary>
        /// 拷贝oldlab的文件到newlab下面
        /// </summary>
        /// <param name="sourcePath">lab文件所在目录(@"~\labs\oldlab")</param>
        /// <param name="savePath">保存的目标目录(@"~\labs\newlab")</param>
        /// <returns>返回:true-拷贝成功;false:拷贝失败</returns>
        static public bool CopyOldLabFilesToNewLab(string sourcePath, string savePath)
        {
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            #region //拷贝labs文件夹到savePath下
            try
            {
                string[] labDirs = Directory.GetDirectories(sourcePath);//目录
                string[] labFiles = Directory.GetFiles(sourcePath);//文件
                if (labFiles.Length > 0)
                {
                    for (int i = 0; i < labFiles.Length; i++)
                    {
                        if (Path.GetFileName(labFiles[i]) != ".lab")//排除.lab文件
                        {
                            File.Copy(sourcePath + "\\" + Path.GetFileName(labFiles[i]), savePath + "\\" + Path.GetFileName(labFiles[i]), true);
                        }
                    }
                }
                if (labDirs.Length > 0)
                {
                    for (int j = 0; j < labDirs.Length; j++)
                    {
                        Directory.GetDirectories(sourcePath + "\\" + Path.GetFileName(labDirs[j]));

                        //递归调用
                        CopyOldLabFilesToNewLab(sourcePath + "\\" + Path.GetFileName(labDirs[j]), savePath + "\\" + Path.GetFileName(labDirs[j]));
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            #endregion
            return true;
        }

        



    }


}
