using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace Biobanking
{
    class SampleInfo
    {
        public int ID;
        public int bloodPlasmaSlice;
        public int buffyCoatSlice;
        public double Z1;
        public double Z2;
    }


    internal class InfoReader
    {
        
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        string GetExeFolder()
        {
            string s = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return s;
        }

        public SampleInfo ReadInfo()
        {
            SampleInfo infos = new SampleInfo();
     
            string sConfigFilePath = GetExeFolder() + "\\setting.csv";
            if (!File.Exists(sConfigFilePath))
            {
                log.Error("Setting file does not exist!");
                return null;
            }

            bool bFirstRow = true;
            using (StreamReader sr = new StreamReader(sConfigFilePath, System.Text.Encoding.Default))
            {
                string sContent = "";
                while (true)
                {
                    sContent = sr.ReadLine();
                    if (sContent == null)
                        break;

                    if (bFirstRow)
                    {
                        bFirstRow = false;
                    }
                    else
                    {
                       return ExtractInfo(sContent.Split(',').ToList() );
                        //infos.Add(sContent.Split(',').ToList());
                    }
                }
            }
            return null;
        }



        private SampleInfo ExtractInfo(List<string> list)
        {
            SampleInfo info = new SampleInfo();
            for (int i = 0; i < list.Count; i++)
            {
                int val = int.Parse(list[i]);
                switch (i)
                {
                    case 0:
                        info.ID = val;
                        break;
                    case 1:
                        info.bloodPlasmaSlice = val;
                        break;
                    case 2:
                        info.buffyCoatSlice = val;
                        break;

                }

            }

            return info;
        }
        //public void SaveSettings(SolutionSettings settings)
        //{

        //    XmlSerializer xs = new XmlSerializer(typeof(SolutionSettings));
        //    string sFile = GetExeFolder() + "\\" + stringRes.solutionSettingFileName;
         
        //    Stream stream = new FileStream(sFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
        //    xs.Serialize(stream, settings);
        //    stream.Close();
        //}

        //public void LoadSettings(ref SolutionSettings settings)
        //{
        //    XmlSerializer xs = new XmlSerializer(typeof(SolutionSettings));
        //    string sFile = GetExeFolder() + "\\" + stringRes.solutionSettingFileName;
        //    if (!File.Exists(sFile))
        //    {
        //        SaveSettings(settings);
        //        return;
        //    }
        //    Stream stream = new FileStream(sFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        //    settings = xs.Deserialize(stream) as SolutionSettings;
        //    stream.Close();
        //}

    }

    struct POINT
    {
        public double x;
        public double y;
        public POINT(double v1, double v2)
        {
            x = v1;
            y = v2;
        }
    }

    class worklistGenerator
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        const int thousand = 1000;
        //SolutionSettings settings = new SolutionSettings();

        PipettingSettings pipettingSetting = new PipettingSettings();
        LabwareSettings labwareSettings = new LabwareSettings();

        List<string> tubeTables = new List<string>();
        const int sourceGrid = 4;
        const string liquidClass = "blood";
        const string sourcePrefix = "source";
        const int maxTubeCount = 12;
        const string destContainer = "dest";
        

        public worklistGenerator()
        {
            SettingsHelper settingHelper = new SettingsHelper();
            settingHelper.LoadSettings(ref pipettingSetting, ref labwareSettings);

        }

  
        public bool DoJob()
        {
            InfoReader reader = new InfoReader();
            //reader.LoadSettings(ref settings);
            List<SampleInfo> infos = new List<SampleInfo>();
            infos.Add(reader.ReadInfo());

            //check capacity
            bool bok = CheckCapacity();
            if (!bok)
            {
                log.Error("微孔板数量不够！");
                return false;
            }
            ReadZValues(ref infos);
            WriteSampleCount(infos.Count);
            
            for (int i = 0; i < infos.Count; i++)
            {
                generateForSample(infos[i]);
            }
            return true;
        }

        private void WriteSampleCount(int sampleCount)
        {
 	        string sOutput = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\output\\";
            //create folders
            if (!Directory.Exists(sOutput))
                Directory.CreateDirectory(sOutput);

            Write2File(sOutput + "sampleCount.txt",sampleCount.ToString());
            
        }

        private void ReadZValues(ref List<SampleInfo> infos)
        {

            //1st get the lines count
            string sReportFile = @"C:\BuffyEx\data\LastReport.txt";
            int lineCount = 0;
            using (StreamReader sr = new StreamReader(sReportFile))
            {
                while(true)
                {
                    string s = sr.ReadLine();
                    if( s == null)
                        break;

                    lineCount++;
                }

            }

            int nSamples = (lineCount - 1) / 2;
            for (int i = 0; i < nSamples - 1; i++)
            {
                SampleInfo curInfo = new SampleInfo();
                curInfo.bloodPlasmaSlice = infos[0].bloodPlasmaSlice;
                curInfo.buffyCoatSlice = infos[0].buffyCoatSlice;
                curInfo.ID = infos.Count+1;
                infos.Add(curInfo);
            }
            using (StreamReader sr = new StreamReader(sReportFile))
            {
                string sContent = "";
                bool bFirstRow = true;
                int nHeightColumn = 0;
                int curRow = 0;
                while (true)
                {
                    sContent = sr.ReadLine();
                    if (sContent == null)
                        break;
                   
                    if (bFirstRow)
                    {
                        bFirstRow = false;
                        nHeightColumn = GetHeightColumn(sContent);
                    }
                    else
                    {
                        int infoIndex = (curRow - 1) % nSamples;
                        if (infoIndex < infos.Count)
                        {
                            string[] vals = sContent.Split('\t');
                            if (curRow < (lineCount + 1) / 2)
                                infos[infoIndex].Z1 = double.Parse(vals[nHeightColumn])*10 ;
                            else
                                infos[infoIndex].Z2 = double.Parse(vals[nHeightColumn])*10 ;
                        }
                        
                    }
                    curRow++;
                 
                }


            }
        }

        private int GetHeightColumn(string sContent)
        {
            string[] strs = sContent.Split('\t');
            for (int i = 0; i < strs.Length; i++)
            {
                if (strs[i].ToLower() == "height")
                {

                    return i;
                }

            }
            throw new Exception("没有找到名为height的列。");
            
        }



        private void generateForSample(SampleInfo sampleInfo)
        {
            string sOutput = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\output\\";
            //create folders
            if (!Directory.Exists(sOutput))
                Directory.CreateDirectory(sOutput);
            

            sOutput = sOutput + "\\"+ sampleInfo.ID.ToString()+"\\";
            if (!Directory.Exists(sOutput))
                Directory.CreateDirectory(sOutput);


            double r = pipettingSetting.r_mm;
            double area = 3.14159265 * r * r;
            double volumeBloodPlasma = Math.Abs(( sampleInfo.Z1 - sampleInfo.Z2) * area ); //ul

            Write2File(sOutput + "plasmaDstSliceCount.txt", sampleInfo.bloodPlasmaSlice.ToString());
            Write2File(sOutput + "buffyDstSliceCount.txt", sampleInfo.buffyCoatSlice.ToString());
            Write2File(sOutput + "buffyLayerCount.txt", pipettingSetting.buffyAspirateLayers.ToString());
            Write2File(sOutput + "plasmaEachVolume.txt", (volumeBloodPlasma/sampleInfo.bloodPlasmaSlice).ToString());
            Write2File(sOutput + "eachLayerPointCount.txt", pipettingSetting.pointsCount.ToString());
            Write2File(sOutput + "Z1.txt", sampleInfo.Z1.ToString());
            Write2File(sOutput + "Z2.txt", sampleInfo.Z2.ToString());
            using(StreamWriter sw = new StreamWriter(sOutput + "xyMoves.txt"))
            {
                List<POINT>pts = GenerateAspiratePts(r);
                //for each layer, move x,y then aspirate, then move z
                for (int layer = 0; layer < sampleInfo.buffyCoatSlice; layer++)
                {
                    //bool needDispense = (layer + 1) % (sampleInfo.buffyCoatSlice / 2) == 0;
                    foreach (POINT pt in pts)
                    {
                        log.InfoFormat("Pt position is:{0} , {1} ", pt.x, pt.y);
                        if( layer == 0)
                            sw.WriteLine(string.Format("{0},{1}",(int)pt.x,(int)pt.y));
                    }
                }
            }

        }

        private double ReadCrossSectionArea()
        {
            double r = pipettingSetting.r_mm;
            double area = 3.14159265 * r * r;
            string curFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            using (StreamReader sr = new StreamReader(curFolder+"\\crossSectionArea.txt"))
            {
                area = double.Parse( sr.ReadLine());

            }
            return area;
   
        }


        private void Write2File(string fileName, string s)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.WriteLine(s);
            }
        }


        private void Write2File(string fileName, List<string> strs)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                foreach(string s in strs)
                    sw.WriteLine(s);
            }
        }


        private List<POINT> GenerateAspiratePts(double r)
        {
            List<POINT> pts = new List<POINT>();
            int v = (int)(r*Math.Sqrt(2)*4.5);
            pts.Add(new POINT(-v , v));
            pts.Add(new POINT(-v,-v));
            pts.Add(new POINT(v,-v) );
            pts.Add(new POINT(v,v));
            return pts;
        }

        private string GetWellSelectionString(int wellIndex)
        {
            string s = "0110";
            string[] table = { "100", "200", "400", "800", "@00", "P00", "p00", "010", "020", "040", "080", "0@0", "0P0", "0p0", "001", "002" };
            return s+table[wellIndex];
        }

        private bool CheckCapacity()
        {
            //throw new NotImplementedException();
            return true;
        }
    }
}
