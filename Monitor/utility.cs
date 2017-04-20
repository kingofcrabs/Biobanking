using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Configuration;
using System.Xml.Serialization;

namespace Monitor
{
    public sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding { get { return Encoding.UTF8; } }
    }

    [Serializable]
    public class PipettingSettings
    {
        public int buffyAspirateLayers;
        public double r_mm;
        public int dstPlasmaSlice;
        public int dstbuffySlice;
        public int deltaXYForMSD;
        public int buffyVolume;
        public int mixTimes;
        public int safeDelta;
        public double buffySpeedFactor;
        public double plasmaGreedyVolume;
        public int dstRedCellSlice;
        public double redCellGreedyVolume;
        public double redCellBottomHeight;

        public PipettingSettings()
        {
            buffyAspirateLayers = 6;
            dstPlasmaSlice = 5;
            dstbuffySlice = 2;
            deltaXYForMSD = 13;
            mixTimes = 2;
            safeDelta = 2;
            r_mm = 5.5;
            buffySpeedFactor = 2.5;
            buffyVolume = 300;
            plasmaGreedyVolume = 0;
            dstRedCellSlice = 0;
            redCellGreedyVolume = 300;
            redCellBottomHeight = 80; //8mm
        }
    }
    class Utility
    {

        static public string GetExeFolder()
        {
            string s = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return s;
        }

  


        public static void SaveSettings<T>(T settings, string sFile)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));

            Stream stream = new FileStream(sFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            xs.Serialize(stream, settings);
            stream.Close();

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

        
        public static PipettingSettings LoadPipettingSettings()
        {
            PipettingSettings pipettingSettings = new PipettingSettings();
            string sFile = GetExeFolder() + "\\pipettingSettings.XML";

            if (!File.Exists(sFile))
                throw new Exception("Failed to load pipettingSetting from xml, file doesnot exist!");


            string sPipettingXMLContent = File.ReadAllText(sFile);
            pipettingSettings = Utility.Deserialize<PipettingSettings>(sPipettingXMLContent);
            return pipettingSettings;

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
