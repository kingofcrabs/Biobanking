using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using Settings;

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
            int totalSlicePerSample = pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice + pipettingSettings.dstRedCellSlice;
            if (labwareSettings.dstLabwareColumns == 1)
                return 1;
            return labwareSettings.dstLabwareColumns / totalSlicePerSample;
        }
    }

    public enum PipettingApproach
    {
        Z_Tight = 0,
        Z_Rotate90_Tight,
        Z_UseNewRow,
        Z_Rotate_UseNewColumn
    }


    public class DetectedHeight
    {
        public double Z1; //1/10 mm
        public double Z2;

    }


    public class DestRack
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public PipettingApproach Alignment { get; set; }
    }

    public class DestRackInfos
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private List<DestRack> rackInfos;
        public List<DestRack> RackInfos
        {
            get
            {
                return rackInfos;
            }
        }

        public void ReadConfigureFile(string sConfigPath)
        {

            if (!File.Exists(sConfigPath))
            {
                log.Error("Configure file does not exist!");
                return;
            }

            bool bFirstRow = true;
            using (StreamReader sr = new StreamReader(sConfigPath, System.Text.Encoding.Default))
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
                        DestRack destRackInfo = ExtractRackInfo(sContent.Split('\t').ToList());
                        rackInfos.Add(destRackInfo);
                    }
                }
            }

        }

        private DestRack ExtractRackInfo(List<string> list)
        {
            if (list.Count < 4)
            {
                throw new Exception("配置文件格式错误，配置项不全");
            }
            DestRack rack = new DestRack();
            rack.Name = list[0];
            rack.Alignment = (PipettingApproach)Enum.Parse(typeof(PipettingApproach), list[1]);
            rack.Width = int.Parse(list[2]);
            rack.Height = int.Parse(list[3]);
            return rack;
        }

    }
}
