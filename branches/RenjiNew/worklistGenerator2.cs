﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;
using setTipVolCommand = System.Collections.Generic.List<string>;
using System.Configuration;
using Settings;
using System.Threading;

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
                                heights[infoIndex].ZLiquid = 10*double.Parse(vals[nHeightColumn]); //unit is mm, scirobot unit is cm
                            else
                                heights[infoIndex].ZBuffy = 10*double.Parse(vals[nHeightColumn]);
                        
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
    class worklistGenerator:worklistCommand
    {
        
        List<int> destPlasmaPos = new List<int>();
        PositionGenerator positionGenerator;
        MappingCalculator mappingCalculator;
        BarcodeTracker barcodeTracker;
        
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        const int maxSourceCountOneRack = 10;
        List<DetectedInfo> detectInfos = null;
        List<PatientInfo> patientInfos = null;
        public bool DoJob(bool realData)
        {
            if (realData)
                detectInfos = ResultReader.Instance.Read();
            else
                detectInfos = GenerateTestInfos();
            patientInfos = ResultReader.Instance.ReadPatientInfos();
            if(patientInfos != null)
            {
                patientInfos = patientInfos.Take(detectInfos.Count).ToList();
                Console.WriteLine(string.Format("{0} samples", patientInfos.Count));
            }
        
            if (GlobalVars.Instance.TrackBarcode)
                barcodeTracker = new BarcodeTracker(pipettingSettings, labwareSettings, patientInfos);
            log.Info("read heights");
            mappingCalculator = new MappingCalculator(Settings.Utility.GetExeFolder() + Settings.stringRes.calibFileName);
            positionGenerator = new PositionGenerator(pipettingSettings, labwareSettings,detectInfos.Count);
            int maxSampleAllowed = positionGenerator.AllowedSamples();
            if (maxSampleAllowed < detectInfos.Count)
                throw new Exception(string.Format("max allowed sample is: {0}",maxSampleAllowed));

            string sOutput = Utility.GetOutputFolder();
            int sourceRackCount = (int)Math.Ceiling((double)detectInfos.Count / labwareSettings.sourceWells);
            WriteRacksCount(sourceRackCount);
            string sOrgOutPut = sOutput;
            RunResult runResult = new RunResult();


            if(labwareSettings.dstLabwareColumns == 1)
            {
                throw new Exception("Doesn't support dispense to tube carriers");
            }

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
                    AddEachSampleInfo2RunResult(srcRack, startSample, heightsThisTime, runResult);
                    GenerateForBatch(sOutput,srcRack, startSample, heightsThisTime);
                }
            }
            if(GlobalVars.Instance.TrackBarcode)
                barcodeTracker.WriteResult();

            AddCommonInfo2RunResult(runResult);
            SaveRunResult(runResult);
            Thread.Sleep(1000);
         
            Console.WriteLine("All finished!");

            return true;
        }

        private void MockHeights(List<DetectedInfo> detectInfos, int minVol)
        {
            mappingCalculator = new MappingCalculator(Settings.Utility.GetExeFolder() + Settings.stringRes.calibFileName);
            var volumePerMM =  mappingCalculator.GetArea();
            var mm = minVol  / volumePerMM;
            for(int i = 0; i< detectInfos.Count; i++)
            {
                detectInfos[i].ZBuffy = 10;  //10mm
                detectInfos[i].ZLiquid = detectInfos[i].ZBuffy + mm;
            }

        }

        private List<DetectedInfo> GenerateTestInfos()
        {
            string sCountFilePath = Utility.GetOutputFolder() + "SampleCount.txt";
            string sampleCntTxt = File.ReadAllText(sCountFilePath);
            int cnt = int.Parse(sampleCntTxt);
            List<DetectedInfo> detectedInfos = new List<DetectedInfo>();
            for (int i = 0; i < cnt; i++)
                detectedInfos.Add(new DetectedInfo()
                {
                    ZLiquid = 5,
                    ZBuffy = 2,
                    LiquidVol = 4.5,
                    SepVol = 2.0
                }); 
            return detectedInfos;
        }


        string sGetDiti = "";
        string sDropDiti = "";
        private void GenerateForBatch(string sOutput,int rackIndex, int sampleIndexInRack, List<DetectedInfo> heightsThisTime)
        {
            bool bNeedUseLastFour = false;
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
            List<string> sContents = new List<string>();
            sContents.Add("W;");
         
                
            string sNotifierFolder = Utility.GetExeFolder();
          
            //0 get diti
            WriteComment(string.Format("batch id is: {0}", batchID), sContents);
            bool isSBS = bool.Parse(ConfigurationManager.AppSettings["SBS"]);
            if (isSBS)
                sGetDiti = string.Format(breakPrefix + "GetDiti2({0},\"DiTi 1000ul SBS LiHa\",0,0,10,70);", ditiMask);
            else
                sGetDiti = string.Format(breakPrefix + "GetDiti2({0},\"DiTi 1000ul LiHa\",0,0,10,70);", ditiMask);
            sContents.Add(sGetDiti);
            sDropDiti = string.Format(breakPrefix + "DropDiti({0},{1},2,10,70,0);", ditiMask, labwareSettings.wasteGrid);

            //1 aspirate plasmas
            double area = mappingCalculator.GetArea();
         
            List<POINT> ptsAsp = positionGenerator.GetSrcWells(sampleIndexInRack, heightsThisTime.Count);  //.GetSrcWellsForCertainSliceOfOneBatch(batchIndex);
            int plasmaSlice = pipettingSettings.dstPlasmaSlice;
            List<double> plasmaVols = new List<double>();
            string description = pipettingSettings.dstbuffySlice == 0 ? "serum" : "plasma";

            for (int slice = 0; slice < plasmaSlice; slice++)
            {
                string sExe = sNotifierFolder + string.Format("Notifier.exe Pipetting;{0};{1};{2}", rackIndex, batchID - 1, slice);
                if (sNotifierFolder != "")
                    sContents.Add(string.Format(breakPrefix + "Execute(\"{0}\",2,\"\",2);", sExe));

                WriteComment(string.Format("Processing slice: {0}, plasma part", slice + 1), sContents);
                plasmaVols = GenerateForSlice(slice, plasmaSlice, ptsAsp, rackIndex, sampleIndexInRack, heightsThisTime, sContents);

                bool addBuffer = bool.Parse(ConfigurationManager.AppSettings["AddBuffer"]);

                if (addBuffer && slice == 3) //after 4th slice, we mock that we have enough 
                {
                    MockHeights(heightsThisTime, 4000);
                }

                
                if (GlobalVars.Instance.TrackBarcode)
                    barcodeTracker.Track(plasmaVols, slice, description);
            }
            
         
            
            //2 aspirate & dispense buffy
            bool bhasBuffyCoat = pipettingSettings.dstbuffySlice > 0;//ResultReader.Instance.HasBuffyCoat();
            bool inSameColumn = IsDstWellsInSameColumn(rackIndex, sampleIndexInRack, ptsAsp.Count);
            int  globalSampleIndex = GetGlobalSampleIndex(rackIndex, sampleIndexInRack);
         
            if (bhasBuffyCoat)
            {
                string sExe = sNotifierFolder + string.Format("Notifier.exe Pipetting;{0};{1};{2}", rackIndex,batchID-1, pipettingSettings.dstPlasmaSlice);
                if(sNotifierFolder != "")
                sContents.Add(string.Format(breakPrefix + "Execute(\"{0}\",2,\"\",2);", sExe));
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
                    buffyvolumes.Add(10); //为了让sample tracking起作用，加10ul
                }
                int srcGrid = GetSrcGrid(rackIndex);
                string strAspirateBuffy = GenerateAspirateCommand(ptsAsp, buffyvolumes, BB_Buffy, srcGrid, 0, labwareSettings.sourceWells);
                sContents.Add(strAspirateBuffy);
                
                //4 asp buffy using MSD
                WriteMSDCommands(sContents, heightsThisTime, tipOffSet);

                //5 dispense buffy
                WriteDispenseBuffy(rackIndex, sampleIndexInRack,  heightsThisTime.Count,bNeedUseLastFour, sContents);
            }


            int dstRedCellSlice = pipettingSettings.dstRedCellSlice;
            List<double> redCellVols = new List<double>();
            heightsThisTime.ForEach(delegate (DetectedInfo x)
            {
                redCellVols.Add(pipettingSettings.plasmaGreedyVolume);
            });
            int buffyNeedSlice = pipettingSettings.buffyStandalone ? 0:pipettingSettings.dstbuffySlice;
            int plasmaAndBuffyUsedSlice = pipettingSettings.dstPlasmaSlice + buffyNeedSlice;
            for (int i = 0; i < dstRedCellSlice; i++)
            {
                WriteComment($"Processing slice: {i + 1}, red cell part", sContents);
                ProcessSliceOnce(ptsAsp, redCellVols, "BB_RedCell", rackIndex, i + plasmaAndBuffyUsedSlice, sampleIndexInRack, sContents);
                if (GlobalVars.Instance.TrackBarcode)
                {
                    barcodeTracker.Track(redCellVols, i + plasmaAndBuffyUsedSlice,"redCell");
                }
            }

            sContents.Add(sDropDiti);

            int endSampleID = rackIndex * 16 + sampleIndexInRack + heightsThisTime.Count;
            if (endSampleID >= detectInfos.Count)
            {
                string sFinishedCommand = sNotifierFolder + string.Format("Notifier.exe Pipetting;true");
                sContents.Add(string.Format(breakPrefix + "Execute(\"{0}\",2,\"\",2);", sFinishedCommand));
            }
            File.WriteAllLines(sBatchFile, sContents);
            
        }

        private void WriteHeightInfo(List<DetectedInfo> heightsThisTime)
        {
            foreach(var heightInfo in heightsThisTime)
            {
                Console.Write($"liqVol:{heightInfo.LiquidVol},sepVol:{heightInfo.SepVol}");
            }
            Console.WriteLine("=============");
        }

        private void AdjustHeights(List<DetectedInfo> heightsThisTime, double v)
        {
            double usedHeight = mappingCalculator.GetHeightFromVolume(v);
            for (int i = 0; i < heightsThisTime.Count; i++)
                heightsThisTime[i].ZLiquid -= usedHeight;
        }

        private void GetGridSite4OneSlicePerLabware(int plasmaSliceFinished, ref int grid, ref int site)
        {
            grid = labwareSettings.dstLabwareStartGrid + labwareSettings.gridsPerCarrier* (plasmaSliceFinished / labwareSettings.sitesPerCarrier);
            site = plasmaSliceFinished % 3 ;
        }

        private int GetMaxSliceTogether(List<DetectedInfo> heightsThisTime)
        {
            double minVol = detectInfos.Min(x => x.LiquidVol - x.SepVol);
            return Math.Min((int)minVol / (int)pipettingSettings.plasmaGreedyVolume,pipettingSettings.dstPlasmaSlice);

        }

        
       
        private List<double> GetDispenseVolume(List<double> volumes)
        {
            List<double> thisBatchVolumes = new List<double>();
            for(int i = 0; i< volumes.Count; i++)
            {
                var volume = Math.Min(volumes[i], pipettingSettings.plasmaGreedyVolume);
                volumes[i] -= volume;
                thisBatchVolumes.Add(volume);
               
            }
            return thisBatchVolumes;
        }

        private List<int> CalculateSlices4Channels(List<DetectedInfo> detectInfos)
        {
            List<int> fastSlicesEachChannelOnce = new List<int>();
            if (pipettingSettings.plasmaGreedyVolume == 0)
                throw new Exception("multiple dispense doesn't support average");
            int maxVol = (950 / (int)pipettingSettings.plasmaGreedyVolume) * (int)pipettingSettings.plasmaGreedyVolume;
            for(int i = 0; i< detectInfos.Count; i++)
            {
                var detectInfo = detectInfos[i];
                double area = mappingCalculator.GetArea();
                double vol = detectInfo.LiquidVol - detectInfo.SepVol - (pipettingSettings.safeDelta + 10)*area;
                int sliceCnt = pipettingSettings.dstPlasmaSlice;
                int maxSlice = maxVol / (int)pipettingSettings.plasmaGreedyVolume;
                if (vol < maxVol)
                {
                    sliceCnt = (int)vol / (int)pipettingSettings.plasmaGreedyVolume;
                    if(!pipettingSettings.giveUpNotEnough)
                    {
                        throw new Exception("multiple dispense doesn't support different volume");
                        //if( remainVol / )
                    }
                }
                sliceCnt = Math.Min(maxSlice, sliceCnt);
                detectInfo.LiquidVol -= sliceCnt * pipettingSettings.plasmaGreedyVolume;
                fastSlicesEachChannelOnce.Add(sliceCnt);
            }
            return fastSlicesEachChannelOnce;
        }

        private int GetTipOffSet(bool bNeedUseLastFour)
        {
             return bNeedUseLastFour ? 4 : 0;
        }

        private bool NeedUseLastFour(int startSample)
        {
            return false; //never consider this
            //if (labwareSettings.tipCount != 4)
            //    return false;
            //int remCnt = startSample % 16;
            //return 16 - remCnt <= 4;
        }
    
        private List<double> GenerateForSlice(int slice, int totalSlice, List<POINT> ptsAsp, int srcRackIndex,int sampleIndexInRack, List<DetectedInfo> heightsThisTime, List<string> sContents,bool isRedCell = false)
        {

            bool addBuffer = bool.Parse(ConfigurationManager.AppSettings["AddBuffer"]);
            if (addBuffer && slice == 4)  //only 4th slice we need add buffer
            {
                sContents.Add(sDropDiti);
                sContents.Add(sGetDiti);
                int bufferGrid = int.Parse(ConfigurationManager.AppSettings["BufferGrid"]);
                int bufferSite = int.Parse(ConfigurationManager.AppSettings["BufferSite"]) - 1;
                //GenerateAspirateCommand(new List<POINT>() { 1, 2, 3, 4 })
                List<POINT> srcPts = positionGenerator.GetSrcWells(0, heightsThisTime.Count);
                List<double> bufferVol = new List<double>();
                const int bufferWellCnt = 8;
                int srcGrid = GetSrcGrid(srcRackIndex);
                for(int i = 0; i < heightsThisTime.Count; i++)
                {
                    bufferVol.Add(pipettingSettings.plasmaGreedyVolume);
                }
                string liquidName = "BB_FirstBuffer";
                for (int i = 0; i < 2; i++)
                {
                    if (i != 0)
                        liquidName = "BB_GeneralBuffer";
                    string strAspirateBuffer = GenerateAspirateCommand(srcPts, bufferVol, liquidName, bufferGrid, bufferSite, bufferWellCnt);
                    string strDispenseBuffer = GenerateDispenseCommand(ptsAsp, bufferVol, liquidName, srcGrid, 0, labwareSettings.sourceWells);
                    sContents.Add(strAspirateBuffer);
                    sContents.Add(strDispenseBuffer);
                }
            }


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
                eachTimesCommands.Add(new List<List<string>>());
                eachTimesHeights.Add(new List<double>());
            }
                       
            int maxVolPerTip = 850;
            int maxVolmaxVolumePerSlice = pipettingSettings.maxVolumePerSlice;
            //1 设置tipvolume, 
            bool bNeedUseLastFour = NeedUseLastFour(sampleIndexInRack);
            int tipOffset = GetTipOffSet(bNeedUseLastFour);
            for (int tipIndex = 0; tipIndex < heightsThisTime.Count; tipIndex++)
            {
                // set tip_volume 
                double z1 = heightsThisTime[tipIndex].ZLiquid;
                double z2 = heightsThisTime[tipIndex].ZBuffy;
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

                WriteComment(string.Format("aspirate times : {0}", times+1), sContents);
                //set tipvolume for the tip need to do aspiration
                double smalleastDiff =  GetSmallestDiff(volumes, heights, heightsThisTime);
                if (smalleastDiff < 10)
                {
                    sLiquidClass = BBPlasmaMedium;
                    if (smalleastDiff < 5)
                    {
                        sLiquidClass = BBPlasmaSlow;
                    }
                }
                ProcessSliceOnce(ptsAsp, volumes, sLiquidClass, srcRackIndex, slice, sampleIndexInRack, sContents);
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
                    double diff = heights[tipIndex] - heightsThisTime[tipIndex].ZBuffy;
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
            bool plasmaGreed = (pipettingSettings.plasmaGreedyVolume != 0);
          
            double totalPlasmaVolume = mappingCalculator.GetVolumeFromHeight(z1) - mappingCalculator.GetVolumeFromHeight(z2)
                    - pipettingSettings.safeDelta * area;
            if (plasmaGreed)
            {
                double greedyVolume = pipettingSettings.plasmaGreedyVolume;
                double endVolume = (curSlice + 1) * greedyVolume;
                double startVolume = curSlice * greedyVolume;
                if (startVolume >= totalPlasmaVolume)
                {
                    aspirateVol = 0;
                }
                else
                {
                    aspirateVol = endVolume > totalPlasmaVolume ? totalPlasmaVolume - startVolume : greedyVolume;
                    if (pipettingSettings.giveUpNotEnough)
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
                    - pipettingSettings.safeDelta * area;
            double safeHeight = z2 + pipettingSettings.safeDelta;
            double needVolume;
            if (pipettingSettings.plasmaGreedyVolume != 0)// greedy
            {
                //see whether enough
                needVolume = (curSlice + 1) * pipettingSettings.plasmaGreedyVolume;
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
             int srcRackIndex,int sliceIndex, int sampleIndexInRack,List<string> sContents)
        {
            int srcGrid = GetSrcGrid(srcRackIndex);

            bool inSameColumn = IsDstWellsInSameColumn(srcRackIndex, sampleIndexInRack, ptsAspOrg.Count);
            
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
            sContents.Add(strAspirate);

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
                sContents.Add(strDispense);
            }
            else
            {
                //throw new Exception("try to dispense to different regions.");
                int endIndexFirstColumn = GetEndIndexForFirstColumn(srcRackIndex, sampleIndexInRack);
                int firstColumnSampleCount = endIndexFirstColumn - srcRackIndex * labwareSettings.sourceWells - sampleIndexInRack + 1;
                List<POINT> ptsDisp = positionGenerator.GetDestWells(srcRackIndex,sliceIndex, sampleIndexInRack, firstColumnSampleCount);
                int grid = 0, site = 0;
                CalculateDestGridAndSite4OneSlicePerLabware(sliceIndex, ref grid, ref site);
                List<double> volumes1;
                List<double> volumes2;
                SplitVolumes2Region(volumes, out volumes1, out volumes2, firstColumnSampleCount);
                string strDispense = GenerateDispenseCommand(ptsDisp, volumes1, liquidClass, grid, site, labwareSettings.dstLabwareRows);
                sContents.Add(strDispense);
                int secondColumnStartSampleIndex = endIndexFirstColumn + 1;
                ptsDisp = positionGenerator.GetDestWells(srcRackIndex,sliceIndex, sampleIndexInRack + firstColumnSampleCount, ptsAsp.Count - firstColumnSampleCount);
                strDispense = GenerateDispenseCommand(ptsDisp, volumes2, liquidClass, grid, site, labwareSettings.dstLabwareRows);
                sContents.Add(strDispense);
            }
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
            if(pipettingSettings.onlyOneSlicePerLabware)
            {
                GetGridSite4OneSlicePerLabware(slice, ref grid, ref site);
            }
            else
                 CalculateDestPlasmaGridAndSite(sampleIndex,slice, ref grid, ref site);
        }

        //in each region, only 1 slice would be dispensed to
        private void CalculateDestGridAndSite4OneSlicePerLabware(int sliceIndex, ref int grid, ref int site)
        {
            int startGrid = labwareSettings.dstLabwareStartGrid;
            int sitesPerGrid = labwareSettings.sitesPerCarrier;
            int additionalGrids = labwareSettings.gridsPerCarrier * (sliceIndex / sitesPerGrid);
            grid = startGrid + additionalGrids;
            site = sliceIndex % labwareSettings.sitesPerCarrier;
        }

        

        private void CalculateDestPlasmaGridAndSite(int sampleIndex, int slice,ref int grid, ref int site)
        {
            int samplesPerRow = Utility.GetSamplesPerRow(labwareSettings, pipettingSettings,pipettingSettings.buffyStandalone);
            int sampleCountPerLabware = samplesPerRow* labwareSettings.dstLabwareRows;
            int labwareCnt = labwareSettings.dstCarrierCnt * labwareSettings.sitesPerCarrier;
            int sampleCountPerCarrier = sampleCountPerLabware * labwareSettings.sitesPerCarrier;
            if (pipettingSettings.buffyStandalone)
            {
                labwareCnt -= 1;//last use for buffy
            }
            int totalSampleAllowed = labwareCnt * sampleCountPerLabware;
            if(labwareSettings.dstLabwareColumns == 1) //16 eppendorf
            {
                totalSampleAllowed = 16* labwareSettings.dstCarrierCnt / (pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice);
            }

            if (sampleIndex + 1 > totalSampleAllowed)
                throw new Exception("Max  samples allowed is: " + string.Format("{0}!", totalSampleAllowed));

            int startGrid =labwareSettings.dstLabwareStartGrid;
            int maxGrid = GetMaxGrid();
            if (startGrid > maxGrid)
            {
                throw new Exception(string.Format("the destination grid: {0} exceeds the maximum grid： {1}", startGrid, maxGrid));
            }
            int actualGridsPerRegion = labwareSettings.gridsPerCarrier;
            if (actualGridsPerRegion == 1)//如果冻存管载架上只有一列位置，则region的大小决定于plasma和buffy的份数
            {
                int buffyNeedGrids = pipettingSettings.buffyStandalone ? 0 : pipettingSettings.dstbuffySlice;
                actualGridsPerRegion = pipettingSettings.dstPlasmaSlice + buffyNeedGrids;
            }

			int nCarrierIndex = sampleIndex / sampleCountPerCarrier;
            int carrierGridsUsed = nCarrierIndex * actualGridsPerRegion;
            int sliceGridsUsed = labwareSettings.gridsPerCarrier == 1 ? slice : 0;//如果冻存管载架上有多列，则每份封装的Grid位置不变

            int sampleIndexInTheCarrier = sampleIndex % sampleCountPerCarrier;
            site = sampleIndexInTheCarrier / sampleCountPerLabware;
            grid = sliceGridsUsed + carrierGridsUsed + startGrid;
        }

        private void CalculateDestRedCellGridAndSite(int sampleIndex, int slice, ref int grid, ref int site)
        {
            int redCellSliceUsedGrid = labwareSettings.gridsPerCarrier == 1 ? slice : 0;//如果冻存管载架上有多列，则每份封装的Grid位置不变
            CalculateDestPlasmaGridAndSite(sampleIndex, 0, ref grid, ref site);
            grid += pipettingSettings.dstPlasmaSlice + pipettingSettings.dstbuffySlice + redCellSliceUsedGrid;

        }
        private void CalculateDestBuffyGridAndSite(int sampleIndex, ref int grid, ref int site)
        {
            if(pipettingSettings.buffyStandalone)
            {
                grid = labwareSettings.dstLabwareStartGrid + (labwareSettings.dstCarrierCnt - 1) * labwareSettings.gridsPerCarrier;
                site = labwareSettings.sitesPerCarrier - 1;
                return;
            }

            CalculateDestPlasmaGridAndSite(sampleIndex, 0, ref grid, ref site);
            if( labwareSettings.gridsPerCarrier == 1)
                grid += pipettingSettings.dstPlasmaSlice;
        }


        private bool IsDstWellsInSameColumn(int srcRackIndex, int startSample, int sampleCount)
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

     
        private void WriteDispenseBuffyNoCheck(List<POINT> pts, int grid,int site, List<string> sContents, int tipOffset)
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
            WriteDispenseBuffyWithMovingPluger(pts,ditiMask,vols,grid,site,tipOffset, sContents);
            //List<POINT> pts = positionGenerator.GetDestWellsForCertainSliceOfOneBatch(batchIndex,0,false);
            //WriteComment("Move tips up.", sw);
            //sContents.Add(sMoveLiha);
            List<double> volumes = new List<double>();
            //int startTip = 0;
            int buffySlice = pipettingSettings.dstbuffySlice;
            List<List<POINT>> eachSlice_ptsDisp = new List<List<POINT>>();
            if (buffySlice > 1) //need dispense the buffy
            {
                volumes.Clear();
                for (int i = 0; i < tipOffset + sampleCnt; i++)
                {
                    if (i < tipOffset || buffySlice == 0)
                        volumes.Add(0);
                    else
                    {
                        volumes.Add(pipettingSettings.buffyVolume * (buffySlice - 1) / buffySlice);
                    }
                }
                WriteComment("aspirate buffy from slice 1", sContents);
                if(tipOffset == 4) //use last four
                {
                    POINT ptZero = new POINT(0, 0); 
                    pts.InsertRange(0, new List<POINT> { ptZero, ptZero, ptZero, ptZero });
                }
                string strAsp = GenerateAspirateCommand(pts, volumes, BB_Buffy_Mix, grid, site, labwareSettings.dstLabwareRows);
                sContents.Add(strAsp);

                for (int i = 0; i < volumes.Count; i++)
                    volumes[i] = volumes[i] / (buffySlice - 1);
                List<POINT> ptsDisp = new List<POINT>(pts);
                eachSlice_ptsDisp.Add(pts);
                for (int slice = 1; slice < pipettingSettings.dstbuffySlice; slice++)
                {
                    int sliceUsedGrid = 0;
                    if (labwareSettings.dstLabwareColumns == 1)
                        sliceUsedGrid = slice;
                    else
                    {
                        ptsDisp = ChangePositions(pts, slice);
                    }
                    eachSlice_ptsDisp.Add(ptsDisp);
                    string strDispense = GenerateDispenseCommand(ptsDisp, volumes, BB_Buffy_Mix, grid + sliceUsedGrid, site, labwareSettings.dstLabwareRows);
                    WriteComment(string.Format("Dispensing buffy slice: {0}", slice + 1), sContents);
                    sContents.Add(strDispense);
                }

                //add buffer,250ul
                int bufferGrid = int.Parse(ConfigurationManager.AppSettings["BufferGrid"]);
                int bufferSite = int.Parse(ConfigurationManager.AppSettings["BufferSite"]) - 1;
                List<POINT> srcPts = positionGenerator.GetSrcWells(0, ptsDisp.Count);
                List<double> bufferVol = new List<double>();
                const int bufferWellCnt = 8;
                for (int i = 0; i < ptsDisp.Count; i++)
                {
                    bufferVol.Add(250);
                }

                for (int i = 0; i < 3; i++)
                {
                    string strAspirateBuffer = GenerateAspirateCommand(srcPts, bufferVol, "BB_Buffer", bufferGrid, bufferSite, bufferWellCnt);
                    string strDispenseBuffer = GenerateDispenseCommand(eachSlice_ptsDisp[i], bufferVol, "BB_Buffer", grid, 0, labwareSettings.sourceWells);
                    sContents.Add(strAspirateBuffer);
                    sContents.Add(strDispenseBuffer);
                }

            }
        }

        private void WriteDispenseBuffyWithMovingPluger(List<POINT> pts, int ditiMask,List<double> vols, int grid, int site, int tipOffset, List<string> sContents)
        {
            if(pipettingSettings.buffyOneByOne) //75,  dispense first then second.
            {
                WriteMovingPluger(new List<POINT>() { pts[0] }, new List<double>() {vols[0],0}, 1, 0, grid, site, sContents);
                if(pts.Count > 1)
                    WriteMovingPluger(new List<POINT>() { pts[1] }, new List<double>() {0,vols[1]}, 2, 1, grid, site, sContents);
                return;
            }
            WriteMovingPluger(pts, vols, ditiMask, tipOffset, grid, site, sContents);

           
        }

        private void WriteMovingPluger(List<POINT> pts, List<double> vols,int ditiMask,int tipOffset, 
            int grid, int site, List<string> sContents)
        {
            string sVolumes = GetVolumeString(vols);
            string sWellSelection = GetWellSelection(labwareSettings.dstLabwareColumns, labwareSettings.dstLabwareRows, pts);
            string sDispense = string.Format(breakPrefix + "{0}({1},\"{2}\",{3}{4},{5},1,\"{6}\", 0, 0);", "Dispense",
                ditiMask, BB_Buffy, sVolumes, 
                grid, site, sWellSelection);
            WriteComment("Write Dispense for sample tracking", sContents);
            sContents.Add(sDispense);

            int sampleCnt = pts.Count;
            WriteComment("Set end speed for plungers", sContents);
            string sSEP = GetSEPString(sampleCnt, 2400, tipOffset);
            WriteComand(sSEP, sContents);
            WriteComment("Set stop speed for plungers", sContents);
            string sSPP = GetSPPString(sampleCnt, 1500, tipOffset);
            WriteComand(sSPP, sContents);
            WriteComment("Move plunger to absolut position 0 (0ul -> dispense all liquid plus part of airgap)", sContents);
            string sPPA = GetPPAString(sampleCnt, 0, tipOffset);
            WriteComand(sPPA, sContents);

            WriteComment(string.Format("Aspirate air gap: {0}", pipettingSettings.airGap), sContents);
            string sPPR = GetPPRString(sampleCnt, pipettingSettings.airGap, tipOffset);
            WriteComand(sPPR, sContents);
        }

        private string GetVolumeString(List<double> vols)
        {
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
            return sVolumes;
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


        private void WriteDispenseBuffy(List<POINT> pts,int grid, int site, List<string> sContents, int tipOffset = 0)
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
            WriteDispenseBuffyNoCheck(pts, grid,site, sContents, tipOffset);
        }

        private void WriteDispenseBuffy(int rackIndex, 
            int sampleIndexThisRack,
            int samplesCountThisBatch,
            bool bNeedUseLastFour, List<string> sContents)
        {
            log.Info("Write DispenseBuffy");
            int plasmaSliceCnt = pipettingSettings.dstPlasmaSlice;
            List<POINT> ptsDisp = pipettingSettings.buffyStandalone ? 
                  positionGenerator.GetDestWellsBuffyOnly
                  (rackIndex, plasmaSliceCnt, sampleIndexThisRack, samplesCountThisBatch)
                : positionGenerator.GetDestWells(rackIndex, plasmaSliceCnt, sampleIndexThisRack, samplesCountThisBatch);
            int grid = 0, site = 0;
            CalculateDestBuffyGridAndSite(GetGlobalSampleIndex(rackIndex, sampleIndexThisRack), ref grid, ref site);
            int tipShift = bNeedUseLastFour ? 4 : 0;
            WriteDispenseBuffy(ptsDisp, grid, site, sContents, tipShift);

            int buffyVol = pipettingSettings.buffyVolume / pipettingSettings.dstbuffySlice;
            List<double> vols = new List<double>();
            ptsDisp.ForEach(x => vols.Add(buffyVol));
            for(int i = 0; i< pipettingSettings.dstbuffySlice; i++)
            {
                int totalSliceIndex = i + plasmaSliceCnt;
                barcodeTracker.Track(vols, totalSliceIndex, "buffy");
            }

            //List<double> aspVols = new List<double>();
            //vols.ForEach(x => aspVols.Add(pipettingSettings.dstbuffySlice * x));
            //if (vols[0] > 950)
            //    throw new Exception("buffy volume MUST < 950");

            //var ptsAsp = ptsDisp;
            //GenerateAspirateCommand(ptsAsp, vols, "Buffy_Mix", grid, site, labwareSettings.dstLabwareRows);
            ////2nd 3rd... slices from this column
            //for (int i = 1; i< pipettingSettings.dstbuffySlice; i++)
            //{
            //    ptsDisp = positionGenerator.GetDestWells(rackIndex, i+plasmaSliceCnt, sampleIndexThisRack, samplesCountThisBatch);
            //    GenerateDispenseCommand(ptsDisp, vols, "Buffy_Mix", grid, site, labwareSettings.dstLabwareRows);
            //}


        }

        private void WriteMSDCommands(List<string> sContents, List<DetectedInfo> detectedInfos, int tipOffset)
        {
            log.Info("Write MSD");
            int samplesInTheBatch = detectedInfos.Count;
            List<double> heights = detectedInfos.Select(x => x.ZBuffy).ToList();

            WriteComment("Set Move values", sContents);
            WriteComment("Set end speed for plungers", sContents);
            string sSEP = GetSEPString(samplesInTheBatch, 2400, tipOffset);
            WriteComand(sSEP, sContents);

            WriteComment("Set stop speed for plungers", sContents);
            string sSPP = GetSPPString(samplesInTheBatch, 1500, tipOffset);
            WriteComand(sSPP, sContents);

            WriteComment("Leading air gap 70", sContents);
            string sPPA = GetPPAString(samplesInTheBatch, pipettingSettings.airGap, tipOffset);
            WriteComand(sPPA, sContents);

            //move tips to absolute position
            MoveTipsToAbsolutePosition(sContents, heights, tipOffset);

            int buffyVol = pipettingSettings.buffyVolume;
            int aspSpeed = 9;
            double speedFactor = pipettingSettings.buffySpeedFactor;
            int speedXY = (int)(60 * speedFactor);
            double area = mappingCalculator.GetArea();
            //double totalHmm = pipettingSettings.buffyVolume * 10 / area;
            double totalHmm = 0;
           
            double adjustedZPerLayer = (totalHmm + pipettingSettings.msdZDistance * 10) / (2 * pipettingSettings.buffyAspirateLayers);
            WriteComment($"msdZDistance:{pipettingSettings.msdZDistance},adjustedZPerLayer:{adjustedZPerLayer} ", sContents);
            int deltaXY = pipettingSettings.deltaXYForMSD;
            
            int accXY = 2000;
            int numSegments = 5;
           
            double aspVolumePerSpiral = buffyVol / (pipettingSettings.buffyAspirateLayers*2.0);
            int dialutorSteps =(int)(3.1 * aspVolumePerSpiral);
            int aspSpeedSteps = (int)(3.1 * aspSpeed * speedFactor * buffyVol / 300.0);
            //int tipOffset = bNeedUseLastFour ? 4 : 0;
            int tipSel = GetTipSelection(samplesInTheBatch,tipOffset);

         

            sSEP = GetSEPString(samplesInTheBatch, aspSpeedSteps, tipOffset);
            WriteComand(sSEP, sContents);
            int totalZ = 0;
            for (int i = 0; i < pipettingSettings.buffyAspirateLayers; i++)
            {
                WriteComment(string.Format("Move LiHa spiral out -times: {0}",i+1), sContents);
                //WriteComment("MSD deltaDistance, NrOfHalfSpirals, TipSelect, DilutorDistance, ZTrackingDistance, XYSpeed,", sw);
                string sMSDCommand = GetMSDCommand(deltaXY, numSegments, tipSel, dialutorSteps, speedXY, accXY);
                WriteComand(sMSDCommand, sContents);
                WriteComment(string.Format("Move LiHa deltaZ down -times: {0} distance:{1}", i + 1, adjustedZPerLayer), sContents);
                double adjustedZ = (i + 1) * adjustedZPerLayer;
                int thisLayerDeltaZ = (int)(Math.Round(adjustedZ - totalZ));
                totalZ += thisLayerDeltaZ;
                string sMoveLihaDown = GetMoveLihaDown(samplesInTheBatch, -thisLayerDeltaZ, tipOffset);
                WriteComand(sMoveLihaDown, sContents);
                WriteComment(string.Format("Move LiHa spiral in -times: {0}",i+1), sContents);
                deltaXY = -deltaXY;
                sMSDCommand = GetMSDCommand(deltaXY, numSegments, tipSel, dialutorSteps, speedXY, accXY);
                WriteComand(sMSDCommand, sContents);
                deltaXY = -deltaXY;
                if (i == pipettingSettings.buffyAspirateLayers - 1)
                    break;
                WriteComment(string.Format("Move LiHa deltaZ down -times: {0}",i+1), sContents);
                WriteComand(sMoveLihaDown, sContents);
            }

            WriteComment(string.Format("Move LiHa up to {0}cm", pipettingSettings.retractHeightcm), sContents);
            var sMoveAbsoluteZ = GetMoveLihaAbsoluteZSlow(samplesInTheBatch, pipettingSettings.retractHeightcm, tipOffset);
            WriteComand(sMoveAbsoluteZ, sContents);

            WriteComment("Trailing air gap 70", sContents);
            string sPPR = GetPPRString(samplesInTheBatch, pipettingSettings.airGap, tipOffset);
            WriteComand(sPPR, sContents);

        }

        #region 
        private void WriteRacksCount(int n)
        {
            var sOutput = Utility.GetOutputFolder();
            Utility.Write2File(sOutput + "RacksCount.txt", n.ToString());
        }
        private void SaveRunResult(RunResult runResult)
        {
            string sRunResultPath = Utility.GetOutputFolder() + "runResult.xml";
            if (File.Exists(sRunResultPath))
                File.Delete(sRunResultPath);

            string sContent = Utility.Serialize(runResult);
            File.WriteAllText(sRunResultPath, sContent);
        }

        private void AddCommonInfo2RunResult(RunResult runResult)
        {
            runResult.buffySlice = pipettingSettings.dstbuffySlice;//ResultReader.Instance.HasBuffyCoat() ? 1 : 0;
            runResult.buffyVolume = pipettingSettings.buffyVolume;
            runResult.plasmaVolume = pipettingSettings.plasmaGreedyVolume;
            runResult.plasmaTotalSlice = pipettingSettings.dstPlasmaSlice;
        }

        private void AddEachSampleInfo2RunResult(int srcRackIndex, int startSampleIndex, List<DetectedInfo> heightsThisTime, RunResult runResult)
        {
            double area = mappingCalculator.GetArea();
            for (int i = 0; i < heightsThisTime.Count; i++)
            {
                double z1 = heightsThisTime[i].ZLiquid;
                double z2 = heightsThisTime[i].ZBuffy;
                
                double totalPlasmaVolume = mappingCalculator.GetVolumeFromHeight(z1) -
                    mappingCalculator.GetVolumeFromHeight(z2) - pipettingSettings.safeDelta *area;
            
                int plasmaSlice = pipettingSettings.dstPlasmaSlice;
                if (pipettingSettings.plasmaGreedyVolume != 0)
                {
                    int maxPlasmaSlice = (int)Math.Ceiling(totalPlasmaVolume / pipettingSettings.plasmaGreedyVolume);
                    plasmaSlice = Math.Min(plasmaSlice, maxPlasmaSlice);
                }
                int startSampleID = srcRackIndex * labwareSettings.sourceWells + i + startSampleIndex + 1;

                runResult.plasmaRealSlices.Add(plasmaSlice);
                //sContents.Add(string.Format("{0};{1};{2};{3}{4}", startSampleID, plasmaSlice, pipettingSetting.dstPlasmaSlice, buffySlice, pipettingSetting));
            }
        }

        private void WriteRunResult(int srcRackIndex, int startSampleIndex, List<DetectedInfo> heightsThisTime, List<string> sContents)
        {
            double area = mappingCalculator.GetArea();
            for (int i = 0; i < heightsThisTime.Count; i++)
            {
                double z1 = heightsThisTime[i].ZLiquid;
                double z2 = heightsThisTime[i].ZBuffy;
                double totalPlasmaVolume = (z1 - z2 - pipettingSettings.safeDelta) * area;
                int plasmaSlice = pipettingSettings.dstPlasmaSlice;
                if (pipettingSettings.plasmaGreedyVolume != 0)
                {
                    int maxPlasmaSlice = (int)Math.Ceiling(totalPlasmaVolume / pipettingSettings.plasmaGreedyVolume);
                    plasmaSlice = Math.Min(plasmaSlice, maxPlasmaSlice);
                }
                int startSampleID = srcRackIndex * labwareSettings.sourceWells + i + startSampleIndex + 1;
                int buffySlice = pipettingSettings.dstbuffySlice;//ResultReader.Instance.HasBuffyCoat() ? 1:0;
                sContents.Add(string.Format("{0};{1};{2};{3}{4}", startSampleID, plasmaSlice, pipettingSettings.dstPlasmaSlice, buffySlice, pipettingSettings));
            }

        }
        #endregion

    }
}
