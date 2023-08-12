using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Xml;
using System.Reflection;
using System.Configuration;


namespace Settings
{

    [Serializable]
    public class TrackInfo
    {
        public string sourceBarcode;
        public string dstBarcode;
        public string description;
        public string volume;
        public string plateBarcode;
        public string position;
        public string name;
        public string seqNo;
        public TrackInfo()
        {

        }
        public TrackInfo(string src, string dst, string desc, string v,string plateBarcode, string position,string name = "",string seqNo = "")
        {
            sourceBarcode = src;
            dstBarcode = dst;
            description = desc;
            volume = v;
            this.plateBarcode = plateBarcode;
            this.position = position;
            this.name = name;
            this.seqNo = seqNo;
        }
    }


    public sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding { get { return Encoding.UTF8; } }
    }


    public class Utility
    {
        #region
        static public string GetExeFolder()
        {
            string s = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return s+"\\";
        }

        static public string GetDescription(int sampleID)
        {
            int sampleIndex = sampleID - 1;
            int colIndex = sampleIndex / 8;
            int rowIndex = sampleIndex - colIndex * 8;
            return string.Format("{0}{1:D2}", (char)('A' + rowIndex), colIndex + 1);
        }


        static public void WriteExecuteResult(bool bok, string sPath)
        {

            sPath = GetOutputFolder() + sPath;
            
            using (StreamWriter sw = new StreamWriter(sPath))
            {
                sw.WriteLine(bok.ToString());
            }
        }

        static public void WriteExecuteResult(int SampleCount)
        {
            string sPath;
            sPath = GetOutputFolder() + stringRes.SampleCountFile;

            if (File.Exists(sPath))
            {
                try
                {
                    File.Delete(sPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            using (StreamWriter sw = new StreamWriter(sPath))
            {
                sw.WriteLine(SampleCount);
            }
        }

        static public string ReadFolder(string sPath)
        {
            string result="";
            sPath = GetOutputFolder() + sPath;
            using (StreamReader sr = new StreamReader(sPath))
            {
                result=sr.ReadLine();
            }
            return result;

        }


        static public string GetExeParentFolder()
        {
            string s = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            int index = s.LastIndexOf("\\");
            return s.Substring(0, index) + "\\";
        }

        static public string GetConfigFolder()
        {
            string sConfigFolder = GetExeParentFolder() + "Config\\";
            CreateIfNotExist(sConfigFolder);
            return sConfigFolder;
        }

        private static void CreateIfNotExist(string sFolder)
        {
            if (!Directory.Exists(sFolder))
                Directory.CreateDirectory(sFolder);
        }

        public static int ParseWellID(string sNewWell, LabwareSettings labwareSettings)
        {
            int rowIndex = sNewWell[0] - 'A';
            int colIndex = int.Parse(sNewWell.Substring(1))-1;
            return GetWellID(rowIndex, colIndex, labwareSettings);
        }

        public static string GetOutputFolder()
        {
            string sExeParent = GetExeParentFolder();
            string sOutputFolder = sExeParent + "Output\\";
            CreateIfNotExist(sOutputFolder);
            return sOutputFolder;
        }

        #endregion
        public static void SaveSettings<T>(T settings, string sFile)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            Stream stream = new FileStream(sFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            xs.Serialize(stream, settings);
            stream.Close();
        }

        static public void Write2File(string fileName, List<string> strs)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                foreach (string s in strs)
                    sw.WriteLine(s);
            }
        }

        static public string GetBloodTypeFile()
        {
            return GetOutputFolder() + "bloodType.txt";
        }

        static public void Write2File(string fileName, string s)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.WriteLine(s);
            }
        }
        public static int CalculateDestLabwareNeededCnt(int totalSampleCnt,LabwareSettings labwareSettings, PipettingSettings pipettingSettings,bool buffyStandalone)
        {
            int samplesPerRow = Utility.GetSamplesPerRow4Plasma(labwareSettings, pipettingSettings,buffyStandalone);
            if (labwareSettings.gridsPerCarrier == 1)
                samplesPerRow = 1;
            int maxSampleCntPerLabware = samplesPerRow * labwareSettings.dstLabwareRows;
           
            //int buffyNeedPlate = buffyStandalone ? 1 : 0;
            return (totalSampleCnt + maxSampleCntPerLabware - 1) / maxSampleCntPerLabware;
        }

        public static int GetSamplesPerRow4Buffy(LabwareSettings labwareSettings, PipettingSettings pipettingSettings)
        {
            int buffySlice = pipettingSettings.dstbuffySlice;
            if (labwareSettings.dstLabwareColumns == 1)
                return 1;
            return labwareSettings.dstLabwareColumns / buffySlice;
        }

        public static int GetWellID(int rowIndex, int colIndex, LabwareSettings labwareSettings)
        {
            int rowCnt = labwareSettings.dstLabwareRows;
            return colIndex * rowCnt + rowIndex + 1;
        }

        public static string GetWellDescription(int wellID, LabwareSettings labwareSettings)
        {
            int colIndex = (wellID - 1) / labwareSettings.dstLabwareRows;
            int rowIndex = wellID - colIndex * labwareSettings.dstLabwareRows - 1;
            return $"{(char)('A' + rowIndex)}{(colIndex + 1).ToString("00")}";
        }

        public static int GetSamplesPerRow4Plasma(LabwareSettings labwareSettings, PipettingSettings pipettingSettings, bool buffyStandalone)
        {
            int buffySlice = buffyStandalone ? 0 : pipettingSettings.dstbuffySlice;
            int totalSlicePerSample = buffySlice + pipettingSettings.dstPlasmaSlice;
            if (pipettingSettings.onlyOneSlicePerLabware)
                totalSlicePerSample = 1;
            if (labwareSettings.dstLabwareColumns == 1)
                return 1;
            return labwareSettings.dstLabwareColumns / totalSlicePerSample;
        }
        public static string Serialize<T>(T value)
        {
            if (value == null)
            {
                return null;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            XmlWriterSettings settings = new XmlWriterSettings();

           //StringWriter textWriter = new StringWriter()

            using (StringWriterWithEncoding textWriter = new StringWriterWithEncoding(Encoding.UTF7))
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(textWriter, settings))
                {
                    serializer.Serialize(xmlWriter, value);
                }
                return textWriter.ToString();
            }
        }

        public static T Deserialize<T>(string xml)
        {
            if (string.IsNullOrEmpty(xml))
            {
                return default(T);
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlReaderSettings settings = new XmlReaderSettings();
 
            using (StringReader textReader = new StringReader(xml))
            {
                using (XmlReader xmlReader = XmlReader.Create(textReader, settings))
                {
                    return (T)serializer.Deserialize(xmlReader);
                }
            }
        }

        public static bool IsValidBarcode(string s)
        {
            foreach (char ch in s)
            {
                if (char.IsDigit(ch))
                    return true;
            }
            return false;
        }
    }

    public struct PatientInfo
    {
        public string id;
        public string name;
        public string seqNo;
        public PatientInfo(string id, string name, string seqNo)
        {
            this.id = id;
            this.name = name;
            this.seqNo = seqNo;
        }
    }

    public sealed class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding encoding;

        public StringWriterWithEncoding(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public override Encoding Encoding
        {
            get { return encoding; }
        }
    }
}
