using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace Biobanking
{
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
    class Utility
    {
        static public string GetOutputFolder()
        {
            return   Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\output\\";
        }

        static public void Write2File(string fileName, List<string> strs)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                foreach (string s in strs)
                    sw.WriteLine(s);
            }
        }
        public static string Serialize<T>(T value)
        {
            if (value == null)
            {
                return null;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            XmlWriterSettings settings = new XmlWriterSettings();


            using (StringWriterWithEncoding textWriter = new StringWriterWithEncoding(Encoding.Default))
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(textWriter, settings))
                {
                    serializer.Serialize(xmlWriter, value);
                }
                return textWriter.ToString();
            }
        }
        static public void Write2File(string fileName, string s)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.WriteLine(s);
            }
        }

        internal static int GetSamplesPerRow(LabwareSettings labwareSettings, PipettingSettings pipettingSettings)
        {
            int totalRow = labwareSettings.dstLabwareRows;
            int totalSlicePerSample = pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice + pipettingSettings.dstRedCellSlice;
            if (labwareSettings.dstLabwareColumns == 1)
                return 1;
            return labwareSettings.dstLabwareRows / totalSlicePerSample;
        }
    }
}
