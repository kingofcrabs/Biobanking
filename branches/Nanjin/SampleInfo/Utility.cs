using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SampleInfo.Properties;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Xml;
using System.Reflection;
using System.Configuration;


namespace SampleInfo
{
    public sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding { get { return Encoding.UTF8; } }
    }

   
    class Utility
    {
        static string sSaveFolder = "";
      
        static public string GetExeFolder()
        {
            string s = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return s+"\\";
        }

        static public void WriteExecuteResult(bool bok, string sPath)
        {

            sPath = GetSaveFolder() + sPath;
            
            using (StreamWriter sw = new StreamWriter(sPath))
            {
                sw.WriteLine(bok.ToString());
            }
        }

        static public void WriteExecuteResult(int SampleCount)
        {
            string sPath;
            sPath = GetSaveFolder() + stringRes.SampleCountFile;

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
            sPath = GetSaveFolder() + sPath;
            using (StreamReader sr = new StreamReader(sPath))
            {
                result=sr.ReadLine();
            }
            return result;

        }

        static public string GetSaveFolder()
        {
            if (sSaveFolder != "")
                return sSaveFolder;

            sSaveFolder = ConfigurationManager.AppSettings["SaveFolder"];
            if (!Directory.Exists(sSaveFolder))
                Directory.CreateDirectory(sSaveFolder);
            return sSaveFolder;
        }

        static public string GetDataFolder()
        {
            string s = GetExeFolder() + stringRes.dataFolder;
            return s;
        }

        public static void SaveSettings<T>(T settings, string sFile)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            Stream stream = new FileStream(sFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
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
