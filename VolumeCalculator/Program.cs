using Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VolumeCalculator
{
    class Program
    {
        static void Main(string[] args)
        {
            
            while(true)
            {
                Console.WriteLine("Please input height.");
                string sHeight = Console.ReadLine();
                while (sHeight == "")
                    sHeight = Console.ReadLine();
                MappingCalculator calculator = new MappingCalculator(Utility.GetExeFolder() + Settings.stringRes.calibFileName);
                double h = double.Parse(sHeight);
                //var tipVol = calculator.GetTipVolumeFromHeight(h);
                var vol = calculator.GetVolumeFromHeight(h);
                Console.WriteLine(string.Format("volume is:{1}",vol));
                Console.WriteLine("Press x to exit!");
                if (Console.ReadKey().Key == ConsoleKey.X)
                {
                    Console.WriteLine();
                    break;
                }
            }
        }
    }
   

    public class MappingCalculator
    {
        List<CalibrationItem> calibItems;
        public MappingCalculator(string sFile)
        {
            string sContent = File.ReadAllText(sFile);
            calibItems = Utility.Deserialize<CalibrationItems>(sContent).calibItems;
            calibItems = calibItems.OrderBy(x => x.volumeUL).ToList();
        }

        private void GetVolumeAndTipVolume(double height, ref int volume, ref int tipVolume)
        {
            double maxH = calibItems.Max(x => x.height);
            double minH = calibItems.Min(x => x.height);
            CalibrationItem higher = calibItems.Last();
            CalibrationItem lower = calibItems.First();
            if (height < maxH)//find first higher
            {
                for (int i = 1; i < calibItems.Count; i++)
                {
                    if (height < calibItems[i].height)
                    {
                        higher = calibItems[i];
                        lower = calibItems[i - 1];
                        break;
                    }
                }
            }
            else
            {
                higher = calibItems.Last();
                lower = calibItems[calibItems.Count - 2];
            }
            double vDiff = higher.volumeUL - lower.volumeUL;
            //double tipVDiff = higher.tipVolume - lower.tipVolume;
            double hDiff = higher.height - lower.height;
            double vhRatio = vDiff / hDiff;
            //double tipVhRatio = tipVDiff / hDiff;
            double currentDiff = height - lower.height;
            double volumeOffset = currentDiff * vhRatio;
            //int tipVOffset = (int)(currentDiff * tipVhRatio);
            volume = lower.volumeUL + (int)volumeOffset;
            //tipVolume = lower.tipVolume + tipVOffset;

        }
        private void GetTipVolumeAndHegiht(double v, ref int tipVol, ref double height)
        {
            double maxV = calibItems.Max(x => x.volumeUL);
            double minV = calibItems.Min(x => x.volumeUL);
            CalibrationItem higher = calibItems.Last();
            CalibrationItem lower = calibItems.First();
            if (v < maxV)//find first higher
            {
                for (int i = 1; i < calibItems.Count; i++)
                {
                    if (v < calibItems[i].volumeUL)
                    {
                        higher = calibItems[i];
                        lower = calibItems[i - 1];
                        break;
                    }
                }
            }
            else
            {
                higher = calibItems.Last();
                lower = calibItems[calibItems.Count - 2];
            }
            double vDiff = higher.volumeUL - lower.volumeUL;
            //double tipVDiff = higher.tipVolume - lower.tipVolume;
            double hDiff = higher.height - lower.height;
            double hvRatio = hDiff / vDiff;
            //double tipVVRatio = tipVDiff / vDiff;
            double currentVDiff = v - lower.volumeUL;
            double heightOffset = currentVDiff * hvRatio;
            //double tipVOffset = currentVDiff * tipVVRatio;
            height = heightOffset + lower.height;
            //tipVol = (int)(tipVOffset + lower.tipVolume);
        }
        public double GetVolumeFromHeight(double height)
        {
            int volume = 0;
            int tipVolume = 0;
            GetVolumeAndTipVolume(height, ref volume, ref tipVolume);
            return volume;
        }

        public int GetTipVolumeFromHeight(double height)
        {
            int volume = 0;
            int tipVolume = 0;
            GetVolumeAndTipVolume(height, ref volume, ref tipVolume);
            return tipVolume;
        }

        public double GetHeightFromVolume(double v)
        {
            int tipVolume = 0;
            double height = 0;
            GetTipVolumeAndHegiht(v, ref tipVolume, ref height);
            return height;
        }

        public int GetTipVolumeFromVolume(double v)
        {
            int tipVolume = 0;
            double height = 0;
            GetTipVolumeAndHegiht(v, ref tipVolume, ref height);
            return tipVolume;
        }
    }
}
