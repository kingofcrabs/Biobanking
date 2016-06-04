using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;
using setTipVolCommand = System.Collections.Generic.List<string>;
using System.Configuration;
using Settings;

namespace Biobanking
{
   
    class SciRobotHelper
    {
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

        public void ReadZValues(ref List<DetectedInfo> heights)
        {

            //1st get the lines count
            string sReportFile = @"C:\BuffyEx\data\LastReport.txt";
            int lineCount = 0;
            using (StreamReader sr = new StreamReader(sReportFile))
            {
                while (true)
                {
                    string s = sr.ReadLine();
                    if (s == null)
                        break;
                    lineCount++;
                }
            }

            int nSamples = (lineCount - 1) / 2;
            heights = new List<DetectedInfo>();
            for (int i = 0; i < nSamples; i++)
                heights.Add(new DetectedInfo());
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
                    if (sContent == "")
                        continue;
                    if (bFirstRow)
                    {
                        bFirstRow = false;
                        nHeightColumn = GetHeightColumn(sContent);
                    }
                    else
                    {
                        int infoIndex = (curRow - 1) % nSamples;
                            string[] vals = sContent.Split('\t');
                            if (curRow < (lineCount + 1) / 2)
                                heights[infoIndex].Z1 = 10*double.Parse(vals[nHeightColumn]); //unit is mm, scirobot unit is cm
                            else
                                heights[infoIndex].Z2 = 10*double.Parse(vals[nHeightColumn]);
                        
                    }
                    curRow++;
                }
            }
        }
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
    class worklistGenerator2
    {
        PipettingSettings pipettingSetting = new PipettingSettings();
        LabwareSettings labwareSettings = new LabwareSettings();
        List<int> destPlasmaPos = new List<int>();
        PositionGenerator positionGenerator;
        MappingCalculator mappingCalculator;
        BarcodeTracker barcodeTracker;
        string breakPrefix = "B;";
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        const string BBPlasmaFast = "BB_Plasma_Fast";
        const string BBPlasmaMedium = "BB_Plasma_Medium";
        const string BBPlasmaSlow = "BB_Plasma_Slow";
        const string BB_Buffy = "BB_Buffy";
        const string BB_Buffy_Mix = "BB_Buffy_Mix";
        const int maxSourceCountOneRack = 10;
        List<DetectedInfo> detectInfos = null;
        public bool DoJob()
        {
            SettingsHelper settingHelper = new SettingsHelper();
            log.Info("load settings");
            settingHelper.LoadSettings(ref pipettingSetting, ref labwareSettings);
            settingHelper.SaveSettings(pipettingSetting);
            detectInfos = ResultReader.Instance.Read();
            if(GlobalVars.Instance.TrackBarcode)
                barcodeTracker = new BarcodeTracker(pipettingSetting, labwareSettings, detectInfos.Select(x=>x.sBarcode).ToList());
            log.Info("read heights");
            mappingCalculator = new MappingCalculator(Settings.Utility.GetExeFolder() + Settings.stringRes.calibFileName);
            //PreparePlasmaDestPositions();
            positionGenerator = new PositionGenerator(pipettingSetting, labwareSettings,detectInfos.Count);
            string errMsg = "";
         
            bool bok = settingHelper.IsValidSetting(labwareSettings,pipettingSetting,ref errMsg);
            if (!bok)
            {
                throw new Exception("Invalid setting:" + errMsg);
            }

            int maxSampleAllowed = positionGenerator.AllowedSamples();
            if (maxSampleAllowed < detectInfos.Count)
                throw new Exception(string.Format("max allowed sample is: {0}",maxSampleAllowed));

            string sOutput = Utility.GetOutputFolder();
            int sourceRackCount = (int)Math.Ceiling((double)detectInfos.Count / labwareSettings.sourceWells);
            WriteRacksCount(sourceRackCount);
            string sOrgOutPut = sOutput;
            for (int srcRack = 0; srcRack < sourceRackCount; srcRack++)
            {
                sOutput = sOrgOutPut + "\\srcRack" + (srcRack + 1).ToString() + "\\";
                if (!Directory.Exists(sOutput))
                    Directory.CreateDirectory(sOutput);

                int thisRackSamples = srcRack == sourceRackCount - 1 ? (detectInfos.Count - srcRack * labwareSettings.sourceWells) : labwareSettings.sourceWells;
                Utility.Write2File(sOutput + "totalSample.txt", thisRackSamples.ToString());
                int batchNum = (int)Math.Ceiling((double)thisRackSamples / labwareSettings.tipCount);
                Utility.Write2File(sOutput + "batchCount.txt", batchNum.ToString());
                for (int startSample = 0; startSample < thisRackSamples; startSample += labwareSettings.tipCount)
                {
                    List<DetectedInfo> heightsThisTime = new List<DetectedInfo>();
                    for (int tip = 0; tip < labwareSettings.tipCount; tip++)
                    {
                        if (tip + startSample >= thisRackSamples)
                            break;
                        heightsThisTime.Add(detectInfos[srcRack*labwareSettings.sourceWells+ startSample + tip]);
                    }
                    GenerateForBatch(sOutput,srcRack, startSample, heightsThisTime);
                }
            }
            if(GlobalVars.Instance.TrackBarcode)
                barcodeTracker.WriteResult();
            
            return true;
        }

        private void GenerateForBatch(string sOutput,int rackIndex, int sampleIndexInRack, List<DetectedInfo> heightsThisTime)
        {
        

            bool bNeedUseLastFour = NeedUseLastFour(sampleIndexInRack);
            log.InfoFormat("rack index : {0}, start sample : {1}", rackIndex, sampleIndexInRack);
             //batch index
            int batchID = 1+ sampleIndexInRack / labwareSettings.tipCount;

            int tipOffSet = GetTipOffSet(bNeedUseLastFour);
            int ditiMask = 0;
            for (int i = tipOffSet; i < tipOffSet + heightsThisTime.Count; i++)
                ditiMask += (int)Math.Pow(2, i);

            //create batch file
            string suffix = ".gwl";
            string sBatchFile = sOutput + string.Format("\\worklist{0}{1}",batchID,suffix);
            
            if (File.Exists(sBatchFile))
                File.Delete(sBatchFile);
            FileStream fs = new FileStream(sBatchFile, FileMode.CreateNew);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            sw.WriteLine("W;");
         
                
            string sNotifierFolder = ConfigurationManager.AppSettings["NotifierFolder"];
          
            //0 get diti
            WriteComment(string.Format("batch id is: {0}", batchID), sw);
            sw.WriteLine(string.Format(breakPrefix + "GetDiti2({0},\"DiTi 1000ul LiHa\",0,0,10,70);", ditiMask));

            //1 aspirate plasmas
            double area = mappingCalculator.GetArea(); ;
            List<POINT> ptsAsp = positionGenerator.GetSrcWells(sampleIndexInRack, heightsThisTime.Count);  //.GetSrcWellsForCertainSliceOfOneBatch(batchIndex);
            int plasmaSlice = pipettingSetting.dstPlasmaSlice;
            List<string> strsSetTipVolume = new List<string>();
            List<double> plasmaVols = new List<double>();
            for (int slice = 0; slice < plasmaSlice; slice++)
            {
                string sExe = sNotifierFolder + string.Format("Notifier.exe Pipetting;{0};{1};{2}", rackIndex,batchID-1, slice);
                if(sNotifierFolder != "")
                sw.WriteLine(string.Format(breakPrefix + "Execute(\"{0}\",2,\"\",2);", sExe));
         
                WriteComment(string.Format("Processing slice: {0}, plasma part", slice + 1), sw);
                plasmaVols = GenerateForSlice(slice, plasmaSlice,ptsAsp,rackIndex,sampleIndexInRack, heightsThisTime,sw);
                if(GlobalVars.Instance.TrackBarcode)
                    barcodeTracker.Track(plasmaVols,slice);
            }
            
            //2 aspirate & dispense buffy
            bool bhasBuffyCoat = pipettingSetting.dstbuffySlice > 0;//ResultReader.Instance.HasBuffyCoat();
            bool inSameColumn = IsDstWellsInSamColumn(rackIndex, sampleIndexInRack, ptsAsp.Count);
            int globalSampleIndex = GetGlobalSampleIndex(rackIndex, sampleIndexInRack);
            //int secondRegionStartSampleIndex = GetEndIndexForFirstRegion(rackIndex, startSample) + 1;

            if (bhasBuffyCoat)
            {
                string sExe = sNotifierFolder + string.Format("Notifier.exe Pipetting;{0};{1};{2}", rackIndex,batchID-1, pipettingSetting.dstPlasmaSlice);
                if(sNotifierFolder != "")
                sw.WriteLine(string.Format(breakPrefix + "Execute(\"{0}\",2,\"\",2);", sExe));
                //3 为buffy设置tipVolume
                List<double> buffyvolumes = new List<double>();
               
                if(bNeedUseLastFour)
                {
                    buffyvolumes.AddRange(new List<double> { 0, 0, 0, 0 });
                    POINT ptZero = new POINT(0, 0);
                    ptsAsp.InsertRange(0, new List<POINT>() { ptZero, ptZero, ptZero, ptZero });
                }
                for (int tipIndex = 0; tipIndex < heightsThisTime.Count; tipIndex++)
                {
                    // set tip_volume 
                    double z2 = heightsThisTime[tipIndex].Z2;
                    int volume2Set = 8000; //high enough
                    WriteSetVolString(tipIndex + tipOffSet, volume2Set, sw);
                    buffyvolumes.Add(10); //为了让tip_volumen_x起作用，加10ul
                }
                int srcGrid = GetSrcGrid(rackIndex);
                string strAspirateBuffy = GenerateAspirateCommand(ptsAsp, buffyvolumes, BB_Buffy, srcGrid, 0, labwareSettings.sourceWells);
                sw.WriteLine(strAspirateBuffy);
                
                //4 asp buffy using MSD
                WriteMSDCommands(sw, heightsThisTime, tipOffSet);


                //5 dispense buffy
                if (inSameColumn)
                {
                    WriteDispenseBuffy(rackIndex, sampleIndexInRack,  heightsThisTime.Count,bNeedUseLastFour,false, sw);
                }
                else//need to dispense to different column
                {
                    int endIndexFirstColumn = GetEndIndexForFirstColumn(rackIndex, sampleIndexInRack);
                    int firstColumnSampleCount = endIndexFirstColumn - rackIndex * labwareSettings.sourceWells - sampleIndexInRack + 1;
                    WriteDispenseBuffy(rackIndex, sampleIndexInRack, firstColumnSampleCount, bNeedUseLastFour, true, sw);
                    int secondColumnStartSampleIndex = endIndexFirstColumn + 1;
                    WriteDispenseBuffy(rackIndex, secondColumnStartSampleIndex, heightsThisTime.Count - firstColumnSampleCount, bNeedUseLastFour, true, sw);
                }
            }

            sw.WriteLine(string.Format(breakPrefix + "DropDiti(255,{1},2,10,70,0);", ditiMask,labwareSettings.wasteGrid));

            int endSampleID = rackIndex * 16 + sampleIndexInRack + heightsThisTime.Count;
            if (endSampleID >= detectInfos.Count)
            {
                string sFinishedCommand = sNotifierFolder + string.Format("Notifier.exe Pipetting;true");
                sw.WriteLine(string.Format(breakPrefix + "Execute(\"{0}\",2,\"\",2);", sFinishedCommand));
            }
            sw.Close();
            
        }

        private int CalculateBuffyTipVolume(double z2)
        {
            return mappingCalculator.GetTipVolumeFromHeight(z2 + pipettingSetting.msdStartPositionAboveBuffy);
        }

        private int GetTipOffSet(bool bNeedUseLastFour)
        {
             return bNeedUseLastFour ? 4 : 0;
        }

        private bool NeedUseLastFour(int startSample)
        {
            if (labwareSettings.tipCount != 4)
                return false;
            int remCnt = startSample % 16;
            return 16 - remCnt <= 4;
        }

        private List<double> GenerateForSlice(int slice, int totalSlice, List<POINT> ptsAsp, int srcRackIndex,int sampleIndexInRack, List<DetectedInfo> heightsThisTime, StreamWriter sw,bool isRedCell = false)
        {
            List<double> vols = new List<double>();
            List<List<double>> volumesList = new List<List<double>>();
            List<List<setTipVolCommand>> eachTimesCommands = new List<List<List<string>>>();
            List<List<double>> eachTimesHeights = new List<List<double>>();
            for (int i = 0; i < 10; i++)
            {
                volumesList.Add(new List<double>());
            }

            for (int i = 0; i < 10; i++)
            {
                //tipVolumesStrs.Add(new List<string>());
                eachTimesCommands.Add(new List<List<string>>());
                eachTimesHeights.Add(new List<double>());
            }
                       
            int maxVolPerTip = 850;
            int maxVolmaxVolumePerSlice = pipettingSetting.maxVolumePerSlice;
            //1 设置tipvolume, 
            bool bNeedUseLastFour = NeedUseLastFour(sampleIndexInRack);
            int tipOffset = GetTipOffSet(bNeedUseLastFour);
            for (int tipIndex = 0; tipIndex < heightsThisTime.Count; tipIndex++)
            {
                // set tip_volume 
                double z1 = heightsThisTime[tipIndex].Z1;
                double z2 = heightsThisTime[tipIndex].Z2;
                double aspPositionVolume = CalcuAspiratePositionVolume(slice, totalSlice, z1, z2);
                double volumeTheTip = CalculateAspirateVolume(slice, totalSlice, z1, z2);
                vols.Add(volumeTheTip);
                volumeTheTip = Math.Min(maxVolmaxVolumePerSlice, volumeTheTip);
                for (int times = 0; times < 10; times++)
                {

                    if (volumeTheTip < maxVolPerTip)
                    {
                        double tmpVol = volumeTheTip;
                        volumesList[times].Add(tmpVol);
                        volumeTheTip = 0;
                    }
                    else
                    {
                        volumeTheTip -= maxVolPerTip;
                        if (volumeTheTip < 15) //avoid too small liquid;
                        {
                            volumesList[times].Add(maxVolPerTip + volumeTheTip);
                            volumeTheTip = 0;
                        }
                        else
                        {
                            volumesList[times].Add(maxVolPerTip);
                        }
                    }

                    //现在volumeTheTip中就是这次吸完后剩下的体积
                    double adjustedVolume = aspPositionVolume + volumeTheTip;
                    double tipVolume2Set = mappingCalculator.GetTipVolumeFromVolume(aspPositionVolume); /*CalculateTipVolume(adjustedHeight, heightsThisTime[tipIndex], slice == totalSlice - 1);*/
                    eachTimesCommands[times].Add(GetSetVolString(tipIndex + tipOffset, tipVolume2Set));
                    eachTimesHeights[times].Add(mappingCalculator.GetHeightFromVolume(aspPositionVolume));
                }
            }
            //foreach (List<double> volumes in volumesList)
            for (int times = 0; times < volumesList.Count; times++)
            {
                string sLiquidClass = BBPlasmaFast;
                List<double> volumes = volumesList[times];                          //所有枪头这次需要吸液的量

                if (volumes.Sum() == 0)
                    continue;

                List<double> heights = eachTimesHeights[times];

                WriteComment(string.Format("aspirate times : {0}", times+1), sw);
                //set tipvolume for the tip need to do aspiration
                double smalleastDiff =  GetSmallestDiff(volumes, heights, heightsThisTime);
                if (smalleastDiff < 10)
                {
                    sLiquidClass = BBPlasmaMedium;
                    if (smalleastDiff < 5)
                    {
                        sLiquidClass = BBPlasmaSlow;
                        //if (pipettingSetting.fixedPositionNearBuffy)
                        //{
                        //    var thisTimeCommands = eachTimesCommands[times];
                        //    for (int tipIndex = 0; tipIndex < heightsThisTime.Count; tipIndex++)
                        //    {
                        //        if (volumes[tipIndex] != 0)
                        //        {
                        //            WriteComment(string.Format("aspirate at height: {0} for tip {1}", heights[tipIndex], tipIndex + tipOffset + 1), sw);
                        //            foreach (string s in thisTimeCommands[tipIndex])
                        //                sw.WriteLine(s);
                        //        }
                        //    }
                        //}
                    }
                }
                
                ProcessSliceOnce(ptsAsp, volumes, sLiquidClass, srcRackIndex, slice, sampleIndexInRack, sw);
            }
            return vols;
        }

        private double GetSmallestDiff(List<double> volumes, List<double> heights , List<DetectedInfo> heightsThisTime)
        {
            double smalleastDiff = 999;
            for (int tipIndex = 0; tipIndex < heightsThisTime.Count; tipIndex++)
            {
                if (volumes[tipIndex] != 0)
                {
                    double diff = heights[tipIndex] - heightsThisTime[tipIndex].Z2;
                    if (diff < smalleastDiff)
                        smalleastDiff = diff;
                }
            }
            return smalleastDiff;
        }

        private double CalculateAspirateVolume(int curSlice, int totalSlice, double z1, double z2)
        {
            double area = mappingCalculator.GetArea();
            double aspirateVol = 0;
            bool plasmaGreed = (pipettingSetting.plasmaGreedyVolume != 0);
          
            double totalPlasmaVolume = mappingCalculator.GetVolumeFromHeight(z1) - mappingCalculator.GetVolumeFromHeight(z2)
                    - pipettingSetting.safeDelta * area;
            if (plasmaGreed)
            {
                double greedyVolume = pipettingSetting.plasmaGreedyVolume;
                double endVolume = (curSlice + 1) * greedyVolume;
                double startVolume = curSlice * greedyVolume;
                if (startVolume >= totalPlasmaVolume)
                {
                    aspirateVol = 0;
                }
                else
                {
                    aspirateVol = endVolume > totalPlasmaVolume ? totalPlasmaVolume - startVolume : greedyVolume;
                    if (pipettingSetting.giveUpNotEnough)
                    {
                        if (aspirateVol < greedyVolume)
                            aspirateVol = 0;
                    }
                }

                if (endVolume < totalPlasmaVolume && curSlice == totalSlice - 1) //最后一管还是不能吸干净
                {
                    log.Debug("cannot aspirate all the plasma within specified slices & greedy approach.");
                }
            }
            else
                aspirateVol = totalPlasmaVolume / totalSlice;//平均吸取

            if (aspirateVol < 0)
                aspirateVol = 0;
            return aspirateVol;
        }

        private double CalcuAspiratePositionVolume(int curSlice, int totalSlice, double z1, double z2)
        {
            double area = mappingCalculator.GetArea();
            double totalVolume = mappingCalculator.GetVolumeFromHeight(z1) - mappingCalculator.GetVolumeFromHeight(z2)
                    - pipettingSetting.safeDelta * area;
            double safeHeight = z2 + pipettingSetting.safeDelta;
            double needVolume;
            if (pipettingSetting.plasmaGreedyVolume != 0)// greedy
            {
                //see whether enough
                needVolume = (curSlice + 1) * pipettingSetting.plasmaGreedyVolume;
                if (needVolume > totalVolume)
                    return safeHeight;
            }
            else
            {
                needVolume = (curSlice + 1) * totalVolume / totalSlice;
            }
            return mappingCalculator.GetVolumeFromHeight(z1) - needVolume;
        }

        private int GetSrcGrid(int rackIndex)
        {
            return rackIndex + labwareSettings.sourceLabwareStartGrid;
        }

        private void ProcessSliceOnce(List<POINT> ptsAspOrg, List<double> volumes, string liquidClass ,
             int srcRackIndex,int sliceIndex, int sampleIndexInRack,StreamWriter sw)
        {
            //有时候，液体需要被喷到不同行，
            bool inSameColumn = IsDstWellsInSamColumn(srcRackIndex, sampleIndexInRack, ptsAspOrg.Count);
            int srcGrid = GetSrcGrid(srcRackIndex);
            int globalSampleIndex = GetGlobalSampleIndex(srcRackIndex, sampleIndexInRack); //
            bool bNeedUseLastFour = NeedUseLastFour(sampleIndexInRack);
            List<POINT> ptsAsp = new List<POINT>(ptsAspOrg);
            if (bNeedUseLastFour)
            {
                POINT ptZero = new POINT(0, 0);
                ptsAsp.InsertRange(0, new List<POINT>() { ptZero, ptZero, ptZero, ptZero });
                volumes.InsertRange(0, new List<double>() { 0, 0, 0, 0 });
            }

            //2 吸，喷
            string strAspirate = GenerateAspirateCommand(ptsAsp, volumes, liquidClass, srcGrid,0, labwareSettings.sourceWells);
            sw.WriteLine(strAspirate);

            if (inSameColumn)
            {
                List<POINT> ptsDisp = positionGenerator.GetDestWells(srcRackIndex, sliceIndex, sampleIndexInRack, ptsAspOrg.Count);
                int grid = 0, site = 0;
                CalculateDestGridAndSite(globalSampleIndex, sliceIndex, ref grid, ref site);
                if(bNeedUseLastFour)
                {
                    POINT ptZero = new POINT(0, 0);
                    ptsDisp.InsertRange(0, new List<POINT>() { ptZero, ptZero, ptZero, ptZero });
                }
                string strDispense = GenerateDispenseCommand(ptsDisp, volumes, liquidClass, grid,site, labwareSettings.dstLabwareRows);
                sw.WriteLine(strDispense);
            }
            else
            {
                //throw new Exception("try to dispense to different regions.");
                int endIndexFirstColumn = GetEndIndexForFirstColumn(srcRackIndex, sampleIndexInRack);
                int firstColumnSampleCount = endIndexFirstColumn - srcRackIndex * labwareSettings.sourceWells - sampleIndexInRack + 1;
                List<POINT> ptsDisp = positionGenerator.GetDestWells(srcRackIndex,sliceIndex, sampleIndexInRack, firstColumnSampleCount);
                int grid = 0, site = 0;
                CalculateDestGridAndSite4OneSlicePerRegion(sliceIndex, ref grid, ref site);
                List<double> volumes1;
                List<double> volumes2;
                SplitVolumes2Region(volumes, out volumes1, out volumes2, firstColumnSampleCount);
                string strDispense = GenerateDispenseCommand(ptsDisp, volumes1, liquidClass, grid, site, labwareSettings.dstLabwareRows);
                sw.WriteLine(strDispense);
                int secondColumnStartSampleIndex = endIndexFirstColumn + 1;
                ptsDisp = positionGenerator.GetDestWells(srcRackIndex,sliceIndex, sampleIndexInRack + firstColumnSampleCount, ptsAsp.Count - firstColumnSampleCount);
                strDispense = GenerateDispenseCommand(ptsDisp, volumes2, liquidClass, grid, site, labwareSettings.dstLabwareRows);
                sw.WriteLine(strDispense);
            }
        }

        //private void ProcessSliceDirectCommands(List<POINT> ptsAsp, List<double> volumes, int srcRackIndex, int sliceIndex,
        //    int sampleIndexInRack, StreamWriter sw)
        //{
        //    var sampleCnt = ptsAsp.Count;
        //    bool bNeedUseLastFour = NeedUseLastFour(sampleIndexInRack);
        //    int tipOffset = GetTipOffSet(bNeedUseLastFour);
        //    //string sMoveLiha = string.Format(breakPrefix + "MoveLiha({0},{1},{2},1,\"{3}\",0,1,0,10,0,0);", ditiMask, grid, site, sWellSelection);
        //    //sw.WriteLine(sMoveLiha);
        //    WriteComment("Set end speed for plungers", sw);
        //    string sSEP = GetSEPString(sampleCnt, 2400, tipOffset);
        //    WriteComand(sSEP, sw);
        //    WriteComment("Set stop speed for plungers", sw);
        //    string sSPP = GetSPPString(sampleCnt, 1500, tipOffset);
        //    WriteComand(sSPP, sw);
        //    WriteComment("Move plunger to absolut position 0 (0ul -> dispense all liquid plus part of airgap)", sw);
        //    string sPPA = GetPPAString(sampleCnt, 0, tipOffset);
        //    WriteComand(sPPA, sw);

        //    WriteComment(string.Format("Move LiHa up to {0}cm", pipettingSetting.retractHeightcm), sw);
        //    var sMoveAbsoluteZ = GetMoveLihaAbsoluteZSlow(sampleCnt, pipettingSetting.retractHeightcm, tipOffset);
        //    WriteComand(sMoveAbsoluteZ, sw);

        //    WriteComment("Set end speed for plungers", sw);
        //    sSEP = GetSEPString(sampleCnt, 2400, tipOffset);
        //    WriteComand(sSEP, sw);
        //    WriteComment("Set stop speed for plungers", sw);
        //    sSPP = GetSPPString(sampleCnt, 1500, tipOffset);
        //    WriteComand(sSPP, sw);
        //    WriteComment(string.Format("Aspirate air gap: {0}", pipettingSetting.airGap), sw);
        //    sPPA = GetPPAString(sampleCnt, pipettingSetting.airGap, tipOffset);
        //    WriteComand(sPPA, sw);
        //}

        private int GetGlobalSampleIndex(int rackIndex, int startSample)
        {
            return rackIndex * labwareSettings.sourceWells + startSample;
        }

        private void SplitVolumes2Region(List<double> volumes, out List<double> volumes1, out List<double> volumes2, int firstRegionSampleCount)
        {
            volumes1 = new List<double>();
            volumes2 = new List<double>();
            for (int i = 0; i < volumes.Count; i++)
            {
                if (i < firstRegionSampleCount)
                {
                    volumes1.Add(volumes[i]);
                    volumes2.Add(0);
                }
                else
                {
                    volumes2.Add(volumes[i]);
                }
            }
        }

        private int GetEndIndexForFirstColumn(int srcRackIndex, int startSample)
        {
            int nStartSampleIndex = srcRackIndex * labwareSettings.sourceWells + startSample;
            int columnIndex = nStartSampleIndex / labwareSettings.dstLabwareRows;
            return (columnIndex + 1) * labwareSettings.dstLabwareRows - 1;
        }

        public int GetMaxGrid()
        {
            string sModel = ConfigurationManager.AppSettings[stringRes.f];
            int model = int.Parse(sModel);
            if (model != 75 && model != 100 && model != 150 && model != 200)
            {
                string errMsg = "EVOModel must be one of 75,100,150 & 200!";
                throw new Exception("Invalid EVOModel: " + errMsg);
            }
            int grid = 27;
            switch (model)
            {
                case 100:
                    grid = 30;
                    break;
                case 150:
                    grid = 45;
                    break;
                case 200:
                    grid = 60;
                    break;
                default:
                    grid = 27;
                    break;
            }
            return grid;
        }
        private void CalculateDestGridAndSite(int sampleIndex, int slice,ref int grid, ref int site)
        {
            CalculateDestPlasmaGridAndSite(sampleIndex,slice, ref grid, ref site);
        }

        //in each region, only 1 slice would be dispensed to
        private void CalculateDestGridAndSite4OneSlicePerRegion(int sliceIndex, ref int grid, ref int site)
        {
            int startGrid = labwareSettings.dstLabwareStartGrid;
            int sitesPerGrid = labwareSettings.sitesPerRegion;
            int additionalGrids = labwareSettings.gridsPerRegion * (sliceIndex / sitesPerGrid);
            grid = startGrid + additionalGrids;
            site = sliceIndex % labwareSettings.sitesPerRegion;
        }

        private void CalculateDestPlasmaGridAndSite(int sampleIndex, int slice,ref int grid, ref int site)
        {
            int totalSlicePerSample = pipettingSetting.dstbuffySlice + pipettingSetting.dstPlasmaSlice;// + pipettingSetting.dstRedCellSlice;
            int samplesPerRow = Utility.GetSamplesPerRow(labwareSettings, pipettingSetting);
            if (labwareSettings.gridsPerRegion == 1)
                samplesPerRow = 1;
            int regionSampleCount = samplesPerRow* labwareSettings.dstLabwareRows*labwareSettings.sitesPerRegion;
           
            int nRegionIndex = sampleIndex / regionSampleCount;
            if (nRegionIndex > labwareSettings.regions)
                throw new Exception("Regions out of range, there is only: " + string.Format("{0} regions in configuration file!",labwareSettings.regions));

            int startGrid =labwareSettings.dstLabwareStartGrid;
            int maxGrid = GetMaxGrid();
            if (startGrid > maxGrid)
            {
                throw new Exception(string.Format("the destination grid: {0} exceeds the maximum grid： {1}", startGrid, maxGrid));
            }
            int actualGridsPerRegion = labwareSettings.gridsPerRegion;
            if (actualGridsPerRegion == 1)//如果冻存管载架上只有一列位置，则region的大小决定于plasma和buffy的份数
                actualGridsPerRegion = pipettingSetting.dstPlasmaSlice + pipettingSetting.dstbuffySlice;
            int regionGridsUsed = nRegionIndex * actualGridsPerRegion;
            int sliceGridsUsed = labwareSettings.gridsPerRegion == 1 ? slice : 0;//如果冻存管载架上有多列，则每份封装的Grid位置不变

            int sampleIndexInTheRegion = sampleIndex % regionSampleCount;
            site = sampleIndexInTheRegion / (labwareSettings.dstLabwareRows*samplesPerRow);
            grid = sliceGridsUsed + regionGridsUsed + startGrid;
        }

        private void CalculateDestRedCellGridAndSite(int sampleIndex, int slice, ref int grid, ref int site)
        {
            int redCellSliceUsedGrid = labwareSettings.gridsPerRegion == 1 ? slice : 0;//如果冻存管载架上有多列，则每份封装的Grid位置不变
            CalculateDestPlasmaGridAndSite(sampleIndex, 0, ref grid, ref site);
            grid += pipettingSetting.dstPlasmaSlice + pipettingSetting.dstbuffySlice + redCellSliceUsedGrid;

        }
        private void CalculateDestBuffyGridAndSite(int sampleIndex, ref int grid, ref int site)
        {
            CalculateDestPlasmaGridAndSite(sampleIndex, 0, ref grid, ref site);
            if( labwareSettings.gridsPerRegion == 1)
                grid += pipettingSetting.dstPlasmaSlice;
        }


        private bool IsDstWellsInSamColumn(int srcRackIndex, int startSample, int sampleCount)
        {
            //if (isPlasma && !pipettingSetting.mov2NextRegionEachPlasmaRack) //无视rack的变化，总是同一region
            //    return true;
            int nStartSampleIndex = srcRackIndex * labwareSettings.sourceWells + startSample;
            int nEndSampleIndex = nStartSampleIndex + sampleCount - 1;
            int wellsCount = labwareSettings.dstLabwareRows;

            int nCol1 = nStartSampleIndex / wellsCount;
            int nCol2 = nEndSampleIndex / wellsCount;
            return nCol1 == nCol2;
        }

        private List<string> GetSetVolString(int tipIndex, double volume2Set)
        {
            string sVarName = string.Format("Tip_Volume_{0}", tipIndex+1);
            string sVal =  ((int)volume2Set).ToString();
            string s = string.Format(breakPrefix + "Variable({0}, {1}, 0, \"\", 0, 1.000000, 10.000000, 0, 2, 0, 0);", sVarName, sVal);
            return new List<string>() { s };
        }


        private void WriteSetVolString(int tipIndex, double volume2Set, StreamWriter sw)
        {
            List<string> strs = GetSetVolString(tipIndex, volume2Set);
            foreach (string s in strs)
            {
                sw.WriteLine(s);
            }
        }



        private void WriteDispenseBuffyNoCheck(List<POINT> pts, int grid,int site, StreamWriter sw, int tipOffset)
        {
            log.Info("WriteDispenseBuffy for certain region");
            int sampleCnt = pts.Count;
            int ditiMask = GetTipSelection(sampleCnt,tipOffset);
            List<double> vols = new List<double>();
            for(int i = 0; i < 8; i++)
            {
                vols.Add(0);
            }
            for( int i = 0; i< sampleCnt;i++)
            {
                if (tipOffset + i < 8)
                    vols[tipOffset + i] = 10;
            }
            string sVolumes = "";
            for (int i = 0; i < 12; i++)
            {
                string sTmp = "";
                if (i < vols.Count) // has the volume
                    sTmp = string.Format("\"{0}\",", vols[i]);
                else
                    sTmp = "0,";
                sVolumes += sTmp;
            }

             //List<POINT> pts = positionGenerator.GetDestWellsForCertainSliceOfOneBatch(batchIndex,0,false);
            string sWellSelection = GetWellSelection(labwareSettings.dstLabwareColumns, labwareSettings.dstLabwareRows, pts);
            string sDispense = string.Format(breakPrefix + "{0}({1},\"{2}\",{3}{4},{5},1,\"{6}\", 0, 0);", "Dispense", ditiMask, BB_Buffy, sVolumes, grid, site, sWellSelection);
            WriteComment("Write Dispense for sample tracking", sw);
            sw.WriteLine(sDispense);
            //string sMoveLiha = string.Format(breakPrefix + "MoveLiha({0},{1},{2},1,\"{3}\",0,1,0,10,0,0);", ditiMask, grid, site, sWellSelection);
            //sw.WriteLine(sMoveLiha);
            WriteComment("Set end speed for plungers", sw);
            string sSEP = GetSEPString(sampleCnt, 2400, tipOffset);
            WriteComand(sSEP, sw);
            WriteComment("Set stop speed for plungers", sw);
            string sSPP = GetSPPString(sampleCnt, 1500 , tipOffset);
            WriteComand(sSPP, sw);
            WriteComment("Move plunger to absolut position 0 (0ul -> dispense all liquid plus part of airgap)", sw);
            string sPPA = GetPPAString(sampleCnt,0, tipOffset);
            WriteComand(sPPA, sw);

            WriteComment(string.Format("Move LiHa up to {0}cm",pipettingSetting.retractHeightcm), sw);
            var sMoveAbsoluteZ = GetMoveLihaAbsoluteZSlow(sampleCnt, pipettingSetting.retractHeightcm, tipOffset);
            WriteComand(sMoveAbsoluteZ, sw);

            WriteComment("Set end speed for plungers", sw);
            sSEP = GetSEPString(sampleCnt, 2400, tipOffset);
            WriteComand(sSEP, sw);
            WriteComment("Set stop speed for plungers", sw);
            sSPP = GetSPPString(sampleCnt, 1500, tipOffset);
            WriteComand(sSPP, sw);
            WriteComment(string.Format("Aspirate air gap: {0}", pipettingSetting.airGap), sw);
            sPPA = GetPPAString(sampleCnt, pipettingSetting.airGap, tipOffset);
            WriteComand(sPPA, sw);
            //WriteComment("Move tips up.", sw);
            //sw.WriteLine(sMoveLiha);

            List<double> volumes = new List<double>();
            int startTip = 0;
            int buffySlice = pipettingSetting.dstbuffySlice;
            if (buffySlice > 1) //need dispense the buffy
            {
                volumes.Clear();
                for (int i = 0; i < startTip + sampleCnt; i++)
                {
                    if (i < startTip || buffySlice == 0)
                        volumes.Add(0);
                    else
                    {
                        volumes.Add(pipettingSetting.buffyVolume * (buffySlice - 1) / buffySlice);
                    }
                }
                WriteComment("aspirate buffy from slice 1", sw);
                string strAsp = GenerateAspirateCommand(pts, volumes, BB_Buffy_Mix, grid, site, labwareSettings.dstLabwareRows);
                sw.WriteLine(strAsp);

                for (int i = 0; i < volumes.Count; i++)
                    volumes[i] = volumes[i] / (buffySlice - 1);
                List<POINT> ptsDisp = new List<POINT>(pts);
                for (int slice = 1; slice < pipettingSetting.dstbuffySlice; slice++)
                {
                    int sliceUsedGrid = 0;
                    if (labwareSettings.dstLabwareColumns == 1)
                        sliceUsedGrid = slice;
                    else
                    {
                        ptsDisp = ChangePositions(pts, slice);
                    }
                    string strDispense = GenerateDispenseCommand(ptsDisp, volumes, BB_Buffy_Mix, grid + sliceUsedGrid, site, labwareSettings.dstLabwareRows);
                    WriteComment(string.Format("Dispensing buffy slice: {0}", slice + 1), sw);
                    sw.WriteLine(strDispense);
                }
            }
        }

        private List<POINT> ChangePositions(List<POINT> pts, int distance2Org)
        {
            List<POINT> ptsDisp = new List<POINT>();
            foreach (POINT pt in pts)
            {
                ptsDisp.Add(new POINT(pt.x + distance2Org, pt.y));
            }
            return ptsDisp;
        }


        private void WriteDispenseBuffy(List<POINT> pts,int grid, int site, StreamWriter sw, int tipOffset = 0)
        {
            //for safe liha movement.
            double maxWell = pts.Select(x => x.y).Max();
            int endTip = tipOffset + pts.Count;
         
            if( maxWell < 4 || maxWell > 13)
            {
                bool bCanMatch = true;
                if (maxWell - endTip >= 12 || endTip - maxWell >= 5)
                {
                    bCanMatch = false;
                }
                string sModel = ConfigurationManager.AppSettings[stringRes.f];
                int model = int.Parse(sModel);
                if (!bCanMatch && model!=75 )
                    throw new Exception("Cannot match tips at labware edge");
            }
            WriteDispenseBuffyNoCheck(pts, grid,site, sw, tipOffset);
        }

        private void WriteDispenseBuffy(int rackIndex, 
            int sampleIndexThisRack,
            int samplesCountThisBatch,
            bool bNeedUseLastFour, bool bOnlyOneSlicePerRegion, StreamWriter sw)
        {
            log.Info("Write DispenseBuffy");
            int slice = pipettingSetting.dstPlasmaSlice;
            if (bOnlyOneSlicePerRegion) //same as first slice
                slice = 0;
            List<POINT> ptsDisp = positionGenerator.GetDestWells(rackIndex, slice,sampleIndexThisRack, samplesCountThisBatch);

            int grid = 0, site = 0;
            if(bOnlyOneSlicePerRegion)
            {
                CalculateDestGridAndSite4OneSlicePerRegion(pipettingSetting.dstPlasmaSlice, ref grid, ref site);
            }
            else
            {
                CalculateDestBuffyGridAndSite(GetGlobalSampleIndex(rackIndex, sampleIndexThisRack), ref grid, ref site);
            }
                
            int tipShift = bNeedUseLastFour ? 4 : 0;
            WriteDispenseBuffy(ptsDisp, grid, site, sw, tipShift);
        }

        private void WriteMSDCommands(StreamWriter sw, List<DetectedInfo> detectedInfos, int tipOffset)
        {
            log.Info("Write MSD");
            int samplesInTheBatch = detectedInfos.Count;
            List<double> heights = detectedInfos.Select(x => x.Z2).ToList();
            foreach(var h in heights)
            {
                log.InfoFormat("Height: {0}", h + pipettingSetting.safeDelta);
            }
            //move tips to absolute position
            MoveTipsToAbsolutePosition(sw, heights, tipOffset);

            int buffyVol = pipettingSetting.buffyVolume;
            int aspSpeed = 9;
            double speedFactor = pipettingSetting.buffySpeedFactor;
            int speedXY = (int)(60 * speedFactor);
            double area = mappingCalculator.GetArea(); 
            double totalHmm = pipettingSetting.buffyVolume * 10 / area;
            double adjustedZPerLayer = (totalHmm + pipettingSetting.msdZDistance * 10) / (2 * pipettingSetting.buffyAspirateLayers);
            
            int deltaXY = pipettingSetting.deltaXYForMSD;
            
            int accXY = 2000;
            int numSegments = 5;
           
            double aspVolumePerSpiral = buffyVol / (pipettingSetting.buffyAspirateLayers*2.0);
            int dialutorSteps =(int)(3.1 * aspVolumePerSpiral);
            int aspSpeedSteps = (int)(3.1 * aspSpeed * speedFactor * buffyVol / 300.0);
            //int tipOffset = bNeedUseLastFour ? 4 : 0;
            int tipSel = GetTipSelection(samplesInTheBatch,tipOffset);

            WriteComment("Set Move values",sw);
            string sSEP = GetSEPString(samplesInTheBatch, aspSpeedSteps, tipOffset);
            WriteComand(sSEP, sw);
            int totalZ = 0;
            for (int i = 0; i < pipettingSetting.buffyAspirateLayers; i++)
            {
                WriteComment(string.Format("Move LiHa spiral out -times: {0}",i+1), sw);
                //WriteComment("MSD deltaDistance, NrOfHalfSpirals, TipSelect, DilutorDistance, ZTrackingDistance, XYSpeed,", sw);
                string sMSDCommand = GetMSDCommand(deltaXY, numSegments, tipSel, dialutorSteps, speedXY, accXY);
                WriteComand(sMSDCommand, sw);
                WriteComment(string.Format("Move LiHa deltaZ down -times: {0} distance:{1}", i + 1, adjustedZPerLayer), sw);
                double adjustedZ = (i + 1) * adjustedZPerLayer;
                int thisLayerDeltaZ = (int)(adjustedZ - totalZ + 0.5);
                totalZ += thisLayerDeltaZ;
                string sMoveLihaDown = GetMoveLihaDown(samplesInTheBatch, -thisLayerDeltaZ, tipOffset);
                WriteComand(sMoveLihaDown, sw);
                WriteComment(string.Format("Move LiHa spiral in -times: {0}",i+1),sw);
                deltaXY = -deltaXY;
                sMSDCommand = GetMSDCommand(deltaXY, numSegments, tipSel, dialutorSteps, speedXY, accXY);
                WriteComand(sMSDCommand, sw);
                deltaXY = -deltaXY;
                if (i == pipettingSetting.buffyAspirateLayers - 1)
                    break;
                WriteComment(string.Format("Move LiHa deltaZ down -times: {0}",i+1), sw);
                WriteComand(sMoveLihaDown, sw);
            }
        }

     

        private string GetCommandForAllTips(string sCommandPrefix, int samplesInTheBatch, int val, int tipOffset = 0)
        {
            string s = sCommandPrefix;
            for (int i = 0; i < tipOffset; i++ )
            {
                s +=  ",";
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
        private void MoveTipsToAbsolutePosition(StreamWriter sw, List<double> heights, int tipOffset)
        {
            string s = "C5PAZ";
            for (int i = 0; i < tipOffset; i++)
            {
                s += ",";
            }
            for (int i = 0; i < heights.Count; i++)
            {
                double h = pipettingSetting.bottomOffsetmm + (heights[i] + pipettingSetting.msdStartPositionAboveBuffy) * 10;
                s += ((int)h).ToString() + ",";
            }
            for (int i = heights.Count + tipOffset; i < 8; i++)
            {
                s += ",";
            }
            WriteComand(s, sw);
        }

        private string GetPPAString(int samplesThisBatch, int pos, int tipOffset)
        {
            string s = "C5PPA";
            return GetCommandForAllTips(s, samplesThisBatch, pos, tipOffset);
        }

        //zposition, 0 is somewhere near table, so we assume 1500 => 15cm a good position
        private string GetMoveLihaAbsoluteZ(int samplesInTheBatch, int zPosition, int tipOffset)
        {
            string s = "C5PAZ";
            return GetCommandForAllTips(s, samplesInTheBatch, zPosition, tipOffset);
        }

        private string GetMoveLihaAbsoluteZSlow(int samplesInTheBatch, int zPosition, int tipOffset)
        {
            string s = "C5MAZ";
            return GetCommandForAllTips(s, samplesInTheBatch, zPosition * 100, tipOffset) + "300";
        }

        private string GetMoveLihaDown(int samplesInTheBatch, int deltaZ, int tipOffset)
        {
            string s = "C5PRZ";
            return GetCommandForAllTips(s, samplesInTheBatch, deltaZ, tipOffset);
        }
        private string GetSEPString(int samplesInTheBatch, int aspSpeedSteps,int tipOffset)
        {
            string sSEP = "C5SEP";
            return GetCommandForAllTips(sSEP, samplesInTheBatch, aspSpeedSteps,tipOffset);
        }
        private string GetSPPString(int samplesThisBatch, int speed, int tipOffset)
        {
            string sSPP = "C5SPP";
            return GetCommandForAllTips(sSPP, samplesThisBatch, speed, tipOffset);
        }
        private string GetMSDCommand(int deltaXY, int numSegments, int tipSel, int dialutorSteps,  int speedXY, int accXY)
        {
            bool bTogether = bool.Parse(ConfigurationManager.AppSettings["MSDXYTogether"]);
            string sDeltaXY = bTogether ? deltaXY.ToString() : string.Format("{0},{0}", deltaXY);

            string s = string.Format("C5MSD{0},{1},{2},{3},0,{4},{5}", sDeltaXY, numSegments, tipSel, dialutorSteps, speedXY, accXY);
            return s;
        }
       
        private int GetTipSelection(int samplesInTheBatch, int startTip = 0)
        {
            int tip = 0;
            for (int i = 0; i < samplesInTheBatch; i++)
                tip += (int)Math.Pow(2, i+startTip);
            return tip;
        }

        private int GetTipSelection(List<double>volumes)
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


        private void WriteComand(string sSEP, StreamWriter sw)
       {
            string s = string.Format(breakPrefix + "Command(\"{0}\",1,1,,,2,2,0);",sSEP);
            sw.WriteLine(s);
        }

        private void WriteComment(string sComment, StreamWriter sw)
        {
            string s = string.Format(breakPrefix + "Comment(\"{0}\");", sComment);
            sw.WriteLine(s);
        }

        private void WriteVariable(string sName, string sVal, StreamWriter sw)
        {
            string s = string.Format(breakPrefix + "Variable({0}, \"{1}\", 0, \"\", 0, 1.000000, 10.000000, 0, 2, 0, 0)", sName, sVal);
            //Variable(Tip_Volume_1, "1000", 0, "", 0, 1.000000, 10.000000, 0, 2, 0, 0);
            sw.WriteLine(s);
        }
        private void WriteVariable(string sName, int nVal, StreamWriter sw)
        {
            string sVal = nVal.ToString();
            WriteVariable(sName, sVal, sw);
        }

        private string GenerateAspirateCommand(List<POINT> wells, List<double> volumes, string liquidClass, int gridPos, int site, int height)
        {
            return GenerateAspirateOrDispenseCommand(wells, volumes, liquidClass, gridPos,site, height, true);
        }

        private string GenerateDispenseCommand(List<POINT> wells, List<double> volumes, string liquidClass, int gridPos,int site, int height)
        {
            return GenerateAspirateOrDispenseCommand(wells, volumes, liquidClass, gridPos, site, height,false);
        }

        private string GenerateAspirateOrDispenseCommand(List<POINT> wells, List<double> volumes, string liquidClass, int gridPos, int site, int height, bool aspirate)
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
                if( liquidClass != BB_Buffy_Mix) //如果是吸样，除了吸buffy_mix,其他情况labware孔的宽度都是1,
                    width = 1;
            }

            string sWellSelection = GetWellSelection(width, height, not0Wells); 
            string sAspOrDis = aspirate ? "Aspirate" : "Dispense";
            return string.Format(breakPrefix + "{0}({1},\"{2}\",{3}{4},{5},1,\"{6}\", 0, 0);", sAspOrDis, tipMask, liquidClass,sVolumes, gridPos,site, sWellSelection);

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
        private string GetWellSelection(int width, int height,List<POINT> wells)
        {
            string selString = string.Format("{0:X2}{1:X2}", width, height);
            int bitCounter = 0;
            int bitMask = 0;
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    bool bSel = wells.IndexOf(new POINT(x+1, y+1)) != -1;
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

        //private double CalculateTipVolume(double aspHeight, DetectedHeight detectHeight, bool isLastSlicePlasma,bool isBuffy = false)
        //{

        //    double safeHeight = detectHeight.Z2 + pipettingSetting.safeDelta;
        //    if (isBuffy)
        //        aspHeight = detectHeight.Z2 + 1;
        //    else if (aspHeight < safeHeight)
        //    {
        //       aspHeight = safeHeight;
        //    }
        //    return mappingCalculator.GetTipVolume(aspHeight);
        //}
 
        private void WriteRacksCount(int n)
        {
            var sOutput = Utility.GetOutputFolder();
            Utility.Write2File(sOutput + "RacksCount.txt", n.ToString());
        }
    }
}
