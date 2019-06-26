using Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biobanking
{
    class worklistCommand
    {

        protected const string BBPlasmaFast = "BB_Plasma_Fast";
        protected const string BBPlasmaMedium = "BB_Plasma_Medium";
        protected const string BBPlasmaSlow = "BB_Plasma_Slow";
        protected const string BB_Buffy = "BB_Buffy";
        protected const string BB_Buffy_Mix = "BB_Buffy_Mix";
        protected string breakPrefix = "B;";
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected PipettingSettings pipettingSettings = new PipettingSettings();
        protected LabwareSettings labwareSettings = new LabwareSettings();

        public worklistCommand()
        {
            SettingsHelper settingHelper = new SettingsHelper();
            log.Info("load settings");
            settingHelper.LoadSettings(ref pipettingSettings, ref labwareSettings);
            string errMsg = "";
            bool bok = settingHelper.IsValidSetting(labwareSettings, pipettingSettings, ref errMsg);
            if (!bok)
            {
                throw new Exception("Invalid setting:" + errMsg);
            }
        }

        protected int GetTipSelection(int samplesInTheBatch, int startTip = 0)
        {
            int tip = 0;
            for (int i = 0; i < samplesInTheBatch; i++)
                tip += (int)Math.Pow(2, i + startTip);
            return tip;
        }

        protected int GetTipSelection(List<double> volumes)
        {
            int tip = 0;
            for (int i = 0; i < volumes.Count; i++)
            {
                if (volumes[i] == 0)
                    continue;
                tip += (int)Math.Pow(2, i);
            }
            return tip;
        }

        protected void WriteComand(string sSEP, StreamWriter sw)
        {
            string s = string.Format(breakPrefix + "Command(\"{0}\",1,1,,,2,2,0);", sSEP);
            sw.WriteLine(s);
        }

        protected string GetComment(string sComment)
        {
            return string.Format(breakPrefix + "Comment(\"{0}\");", sComment);
        }

        protected void WriteComment(string sComment, StreamWriter sw)
        {
            string s = GetComment(sComment);
            sw.WriteLine(s);
        }

        protected void WriteVariable(string sName, string sVal, StreamWriter sw)
        {
            string s = string.Format(breakPrefix + "Variable({0}, \"{1}\", 0, \"\", 0, 1.000000, 10.000000, 0, 2, 0, 0)", sName, sVal);
            //Variable(Tip_Volume_1, "1000", 0, "", 0, 1.000000, 10.000000, 0, 2, 0, 0);
            sw.WriteLine(s);
        }
        protected void WriteVariable(string sName, int nVal, StreamWriter sw)
        {
            string sVal = nVal.ToString();
            WriteVariable(sName, sVal, sw);
        }

        protected string GenerateAspirateCommand(List<POINT> wells, List<double> volumes, string liquidClass, int gridPos, int site, int height)
        {
            return GenerateAspirateOrDispenseCommand(wells, volumes, liquidClass, gridPos, site, height, true);
        }

        protected string GenerateDispenseCommand(List<POINT> wells, List<double> volumes, string liquidClass, int gridPos, int site, int height)
        {
            return GenerateAspirateOrDispenseCommand(wells, volumes, liquidClass, gridPos, site, height, false);
        }

        protected string GenerateAspirateOrDispenseCommand(List<POINT> wells, List<double> volumes, string liquidClass, int gridPos, int site, int height, bool aspirate)
        {
            //B; Aspirate(3, "Water free dispense", "20", "20", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 1, "0110300", 0, 0);
            int tipMask = GetTipSelection(volumes);
            List<POINT> not0Wells = new List<POINT>();
            for (int i = 0; i < wells.Count; i++)
            {
                //if (volumes[i+startTip] != 0) //always start from 0 now,
                if (volumes[i] != 0)
                    not0Wells.Add(wells[i]);
            }
            string sVolumes = "";
            for (int i = 0; i < 12; i++)
            {
                string sTmp = "";
                if (i < volumes.Count) // has the volume
                    sTmp = string.Format("\"{0}\",", volumes[i]);
                else
                    sTmp = "0,";
                sVolumes += sTmp;
            }


            int width = labwareSettings.dstLabwareColumns;
            if (aspirate)
            {
                if (liquidClass != BB_Buffy_Mix) //如果是吸样，除了吸buffy_mix,其他情况labware孔的宽度都是1,
                    width = 1;
            }

            string sWellSelection = GetWellSelection(width, height, not0Wells);
            string sAspOrDis = aspirate ? "Aspirate" : "Dispense";
            return string.Format(breakPrefix + "{0}({1},\"{2}\",{3}{4},{5},1,\"{6}\", 0, 0);", sAspOrDis, tipMask, liquidClass, sVolumes, gridPos, site, sWellSelection);

        }
        protected int GetGlobalSampleIndex(int rackIndex, int startSample)
        {
            return rackIndex * labwareSettings.sourceWells + startSample;
        }
        public static string Chr(int asciiCode)
        {
            if (asciiCode >= 0 && asciiCode <= 255)
            {
                byte[] byteArray = new byte[] { (byte)asciiCode };
                string strCharacter = System.Text.Encoding.Default.GetString(byteArray);
                return (strCharacter);
            }
            else
            {
                throw new Exception("ASCII Code is not valid.");
            }
        }
        protected string GetWellSelection(int width, int height, List<POINT> wells)
        {
            string selString = string.Format("{0:X2}{1:X2}", width, height);
            int bitCounter = 0;
            int bitMask = 0;
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    bool bSel = wells.IndexOf(new POINT(x + 1, y + 1)) != -1;
                    if (bSel)
                        bitMask |= (1 << bitCounter);
                    if (++bitCounter > 6)
                    {
                        string tmpChar = Chr(48 + bitMask);
                        selString += tmpChar;
                        bitCounter = 0;
                        bitMask = 0;
                    }
                }
            }
            if (bitCounter > 0)
                selString += (char)('0' + bitMask);
            return selString;
        }

        protected List<string> GetSetVolString(int tipIndex, double volume2Set)
        {
            string sVarName = string.Format("Tip_Volume_{0}", tipIndex + 1);
            string sVal = ((int)volume2Set).ToString();
            string s = string.Format(breakPrefix + "Variable({0}, {1}, 0, \"\", 0, 1.000000, 10.000000, 0, 2, 0, 0);", sVarName, sVal);
            return new List<string>() { s };
        }


        protected void WriteSetVolString(int tipIndex, double volume2Set, StreamWriter sw)
        {
            List<string> strs = GetSetVolString(tipIndex, volume2Set);
            foreach (string s in strs)
            {
                sw.WriteLine(s);
            }
        }


        protected string GetCommandForAllTips(string sCommandPrefix, int samplesInTheBatch, int val, int tipOffset = 0)
        {
            string s = sCommandPrefix;
            for (int i = 0; i < tipOffset; i++)
            {
                s += ",";
            }
            for (int i = 0; i < samplesInTheBatch; i++)
            {
                s += val.ToString() + ",";
            }
            for (int i = samplesInTheBatch + tipOffset; i < 8; i++)
            {
                s += ",";
            }
            return s;
        }
        protected void MoveTipsToAbsolutePosition(StreamWriter sw, List<double> heights, int tipOffset)
        {
            string s = "C5PAZ";
            for (int i = 0; i < tipOffset; i++)
            {
                s += ",";
            }
            for (int i = 0; i < heights.Count; i++)
            {
                double h = pipettingSettings.bottomOffset + (heights[i] + pipettingSettings.msdStartPositionAboveBuffy) * 10;
                s += ((int)h).ToString() + ",";
            }
            for (int i = heights.Count + tipOffset; i < 8; i++)
            {
                s += ",";
            }
            WriteComand(s, sw);
        }

        protected string GetPPAString(int samplesThisBatch, int pos, int tipOffset)
        {
            string s = "C5PPA";
            return GetCommandForAllTips(s, samplesThisBatch, pos, tipOffset);
        }

        protected string GetPPRString(int samplesThisBatch, int pos, int tipOffset)
        {
            string s = "C5PPR";
            return GetCommandForAllTips(s, samplesThisBatch, pos, tipOffset);
        }


        //zposition, 0 is somewhere near table, so we assume 1500 => 15cm a good position
        protected string GetMoveLihaAbsoluteZ(int samplesInTheBatch, int zPosition, int tipOffset)
        {
            string s = "C5PAZ";
            return GetCommandForAllTips(s, samplesInTheBatch, zPosition, tipOffset);
        }

        protected string GetMoveLihaAbsoluteZSlow(int samplesInTheBatch, int zPosition, int tipOffset)
        {
            string s = "C5MAZ";
            return GetCommandForAllTips(s, samplesInTheBatch, zPosition * 100, tipOffset) + "300";
        }

        protected string GetMoveLihaDown(int samplesInTheBatch, int deltaZ, int tipOffset)
        {
            string s = "C5PRZ";
            return GetCommandForAllTips(s, samplesInTheBatch, deltaZ, tipOffset);
        }
        protected string GetSEPString(int samplesInTheBatch, int aspSpeedSteps, int tipOffset)
        {
            string sSEP = "C5SEP";
            return GetCommandForAllTips(sSEP, samplesInTheBatch, aspSpeedSteps, tipOffset);
        }
        protected string GetSPPString(int samplesThisBatch, int speed, int tipOffset)
        {
            string sSPP = "C5SPP";
            return GetCommandForAllTips(sSPP, samplesThisBatch, speed, tipOffset);
        }
        protected string GetMSDCommand(int deltaXY, int numSegments, int tipSel, int dialutorSteps, int speedXY, int accXY)
        {
            bool bTogether = pipettingSettings.msdXYTogether;//bool.Parse(ConfigurationManager.AppSettings["MSDXYTogether"]);
            string sDeltaXY = bTogether ? deltaXY.ToString() : string.Format("{0},{0}", deltaXY);

            string s = string.Format("C5MSD{0},{1},{2},{3},0,{4},{5}", sDeltaXY, numSegments, tipSel, dialutorSteps, speedXY, accXY);
            return s;
        }
    }
}
