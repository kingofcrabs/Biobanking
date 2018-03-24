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
    class worklistGenerator:worklistCommand
    {
        
        List<int> destPlasmaPos = new List<int>();
        PositionGenerator positionGenerator;
        MappingCalculator mappingCalculator;
        BarcodeTracker barcodeTracker;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        const int maxSourceCountOneRack = 10;
        List<DetectedInfo> detectInfos = null;
        int dstPlateStartWellID = 1; //for test
        
        public bool DoJob()
        {
            detectInfos = ResultReader.Instance.Read();
            List<string> srcBarcodes = detectInfos.Select(x => x.sBarcode).ToList();
            Console.WriteLine(string.Format("{0} samples", srcBarcodes.Count));
            string startWellIDFile =  Utility.GetOutputFolder() + "startWellID.txt";
            if(File.Exists(startWellIDFile))
            {
                dstPlateStartWellID = int.Parse(File.ReadAllText(startWellIDFile));
            }
            if(GlobalVars.Instance.TrackBarcode)
                barcodeTracker = new BarcodeTracker(pipettingSettings, labwareSettings, srcBarcodes, dstPlateStartWellID);
            log.Info("read heights");
            mappingCalculator = new MappingCalculator(Settings.Utility.GetExeFolder() + Settings.stringRes.calibFileName);
            DeleteRackFolders();
            positionGenerator = new PositionGenerator(pipettingSettings, labwareSettings,detectInfos.Count);
            int maxSampleAllowed = positionGenerator.AllowedSamples();
            if (maxSampleAllowed < detectInfos.Count)
                throw new Exception(string.Format("max allowed sample is: {0}",maxSampleAllowed));
            string sOutput = Utility.GetOutputFolder();
            int sourceRackCount = (int)Math.Ceiling((double)detectInfos.Count / labwareSettings.sourceWells);
            WriteRacksCount(sourceRackCount);
            string sOrgOutPut = sOutput;
            RunResult runResult = new RunResult();
            int curGrid, curSite;
            curGrid = curSite = 0;
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
                    //CalculateDestGridAndSite(globalSampleIndex, sliceIndex, ref grid, ref site);
                    //WriteNeedDecapper(sOutput,ref curGrid, ref curSite, srcRack,startSample);
                    GenerateForBatch(sOutput,srcRack, startSample, heightsThisTime);
                    AddEachSampleInfo2RunResult(srcRack, startSample, heightsThisTime, runResult);
                }
            }
            if(GlobalVars.Instance.TrackBarcode)
                barcodeTracker.WriteResult();

            AddCommonInfo2RunResult(runResult);
            SaveRunResult(runResult);
            Thread.Sleep(1000);
            return true;
        }

        private void WriteNeedDecapper(string sOutput, ref int lastGrid, ref int lastSite, int rackIndex, int sampleIndexInRack)
        {
            int batchID = 1 + sampleIndexInRack / labwareSettings.tipCount;
            int globalSampleIndex = GetGlobalSampleIndex(rackIndex, sampleIndexInRack);
            int curGrid, curSite;
            curSite = curGrid = 0;
            CalculateDestGridAndSite(globalSampleIndex, 0, ref curGrid, ref curSite);
          
            string sDecaperFile = sOutput + string.Format("\\decap_Plate{0}.txt", batchID);
            bool bNeed = curGrid != lastGrid || curSite != lastSite;
            lastGrid = curGrid;
            lastSite = curSite;
            File.WriteAllText(sDecaperFile, bNeed.ToString());
        }

      

        private void DeleteRackFolders()
        {
            var directorys = Directory.EnumerateDirectories(Utility.GetOutputFolder());
            foreach(var dir in directorys)
            {
                if(dir.Contains("srcRack"))
                    Directory.Delete(dir,true);
            }
        }

        private void GenerateForBatch(string sOutput,int rackIndex, int sampleIndexInRack, List<DetectedInfo> heightsThisTime)
        {
            bool bNeedUseLastFour = NeedUseLastFour(sampleIndexInRack);
            log.InfoFormat("rack index : {0}, start sample : {1}", rackIndex, sampleIndexInRack);
             //batch index
            int batchID = 1+ sampleIndexInRack / labwareSettings.tipCount;

            int ditiMask = 0;
            for (int i = 0; i < heightsThisTime.Count; i++)
            {
                ditiMask += (int)Math.Pow(2, i);
            }
                

            //create batch file
            
            string sBatchFile = sOutput + string.Format("\\worklist{0}.gwl",batchID);
           
           
            

            string sBatchFileTransfer2Final = sOutput + string.Format("\\transfer2Final{0}.gwl",batchID);
            if (File.Exists(sBatchFile))
                File.Delete(sBatchFile);
              if (File.Exists(sBatchFileTransfer2Final))
                File.Delete(sBatchFileTransfer2Final);
            FileStream fs = new FileStream(sBatchFile, FileMode.CreateNew);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            sw.WriteLine("W;");
         
                
            string sNotifierFolder = Utility.GetExeFolder();
          
            //0 get diti
            WriteComment(string.Format("batch id is: {0}", batchID), sw);
            string getDiti = string.Format(breakPrefix + "GetDiti2({0},\"DiTi 1000ul LiHa\",0,0,10,70);", ditiMask);
            sw.WriteLine(getDiti);

            //1 aspirate plasmas
            double area = mappingCalculator.GetArea();
            
            List<POINT> ptsAsp = positionGenerator.GetSrcWells(sampleIndexInRack, heightsThisTime.Count);  //.GetSrcWellsForCertainSliceOfOneBatch(batchIndex);
            int plasmaSlice = pipettingSettings.dstPlasmaSlice;
            List<double> plasmaVols = new List<double>();
            for (int slice = 0; slice < plasmaSlice; slice++)
            {
                string sExe = sNotifierFolder + string.Format("Notifier.exe Pipetting;{0};{1};{2}", rackIndex,batchID-1, slice);
                List<string> hintInfos = new List<string>();
                if(sNotifierFolder != "")
                    hintInfos.Add(string.Format(breakPrefix + "Execute(\"{0}\",2,\"\",2);", sExe));
                hintInfos.Add(GetComment(string.Format("Processing slice: {0}, plasma part", slice + 1)));
                List<string> thisSliceTransfer2FinalCmds = new List<string>();
               
                plasmaVols = GenerateForSlice(slice, plasmaSlice, ptsAsp, rackIndex, sampleIndexInRack, heightsThisTime, sw, thisSliceTransfer2FinalCmds);
                
                hintInfos.ForEach(x=>sw.WriteLine(x));
                if(GlobalVars.Instance.TrackBarcode)
                    barcodeTracker.Track(plasmaVols,slice,heightsThisTime);
            }
            
            //2 aspirate & dispense buffy
            bool bhasBuffyCoat = pipettingSettings.dstbuffySlice > 0;//ResultReader.Instance.HasBuffyCoat();
            bool inSameColumn = IsDstWellsInSameColumn(rackIndex, sampleIndexInRack, ptsAsp.Count);
            int  globalSampleIndex = GetGlobalSampleIndex(rackIndex, sampleIndexInRack);
            //int secondRegionStartSampleIndex = GetEndIndexForFirstRegion(rackIndex, startSample) + 1;

            if (bhasBuffyCoat)
            {
                string sExe = sNotifierFolder + string.Format("Notifier.exe Pipetting;{0};{1};{2}", rackIndex,batchID-1, pipettingSettings.dstPlasmaSlice);
                if(sNotifierFolder != "")
                sw.WriteLine(string.Format(breakPrefix + "Execute(\"{0}\",2,\"\",2);", sExe));
                //3 为buffy设置tipVolume
                List<double> buffyvolumes = new List<double>();
               
                
                for (int tipIndex = 0; tipIndex < heightsThisTime.Count; tipIndex++)
                {
                    int vol = heightsThisTime[tipIndex].Z2 == 100 ? 0 : 10;
                    buffyvolumes.Add(vol); //为了让sample tracking起作用，加10ul
                }
                int srcGrid = GetSrcGrid(rackIndex);
                string strAspirateBuffy = GenerateAspirateCommand(ptsAsp, buffyvolumes, BB_Buffy, srcGrid, 0, labwareSettings.sourceWells);
                sw.WriteLine(strAspirateBuffy);
                
                //4 asp buffy using MSD
                WriteMSDCommands(sw, heightsThisTime);

                //5 dispense buffy
                if (inSameColumn)
                {
                    int dstWellStartID = GetDstWellStartID(rackIndex, sampleIndexInRack);
                    WriteDispenseBuffy(rackIndex, sampleIndexInRack, pipettingSettings.dstPlasmaSlice, dstWellStartID-1, heightsThisTime, sw);
                }
                else//need to dispense to different column
                {
                    int dstWellStartIndex = 0;
                    int firstColumnSampleCnt = 0;
                    int secondColumnStartSampleIndex = 0;
                    GetTwoColumnInfo(rackIndex, sampleIndexInRack, ref dstWellStartIndex, ref firstColumnSampleCnt, ref secondColumnStartSampleIndex);
                    WriteDispenseBuffy(rackIndex, sampleIndexInRack, pipettingSettings.dstPlasmaSlice,dstWellStartIndex, heightsThisTime.Take(firstColumnSampleCnt).ToList(), sw);
                    WriteDispenseBuffy(rackIndex, secondColumnStartSampleIndex, pipettingSettings.dstPlasmaSlice, secondColumnStartSampleIndex, heightsThisTime.Skip(firstColumnSampleCnt).ToList(), sw, firstColumnSampleCnt);
                }
            }
            string dropDiti = string.Format(breakPrefix + "DropDiti({0},{1},2,10,70,0);", ditiMask,labwareSettings.wasteGrid);
            sw.WriteLine(dropDiti);
            
            int endSampleID = rackIndex * 16 + sampleIndexInRack + heightsThisTime.Count;
            if (endSampleID >= detectInfos.Count)
            {
                string sFinishedCommand = sNotifierFolder + string.Format("Notifier.exe Pipetting;true");
                sw.WriteLine(string.Format(breakPrefix + "Execute(\"{0}\",2,\"\",2);", sFinishedCommand));
            }
            sw.Close();
            
        }

     

      
        private void SetAspVolumes(int slice, List<double> toAspVolumes)
        {
            if (slice == 0)
                toAspVolumes.Add(0);
            else
                toAspVolumes.Add(slice * pipettingSettings.plasmaGreedyVolume + 50);
        }

        private List<int> CalculateSlices(List<double> volumes)
        {
            List<int> eachTipSlices = new List<int>();
            foreach(var vol in volumes)
            {
                int maxSliceCnt = ((int)vol - 50) / ((int)(pipettingSettings.plasmaGreedyVolume));
                eachTipSlices.Add(Math.Min(maxSliceCnt,pipettingSettings.dstPlasmaSlice));
            }
            return eachTipSlices;
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

        private List<double> GenerateForSlice(int slice, int totalSlice, List<POINT> ptsAsp,
            int srcRackIndex,int sampleIndexInRack, List<DetectedInfo> heightsThisTime, StreamWriter sw,List<string> transfer2Final)
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
                eachTimesCommands.Add(new List<List<string>>());
                eachTimesHeights.Add(new List<double>());
            }
                       
            int maxVolPerTip = 850;
            int maxVolmaxVolumePerSlice = pipettingSettings.maxVolumePerSlice;
            bool bNeedUseLastFour = NeedUseLastFour(sampleIndexInRack);
            int tipOffset = GetTipOffSet(bNeedUseLastFour);
            for (int tipIndex = 0; tipIndex < heightsThisTime.Count; tipIndex++)
            {
                double z1 = heightsThisTime[tipIndex].Z1;
                double z2 = heightsThisTime[tipIndex].Z2;
                double aspPositionVolume = Math.Round( CalcuAspiratePositionVolume(slice, totalSlice, z1, z2));
                double volumeTheTip = Math.Round(CalculateAspirateVolume(slice, totalSlice, z1, z2));
                volumeTheTip = Math.Max(volumeTheTip, 0);
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
                transfer2Final.Add(GetComment(string.Format("aspirate times : {0}", times+1)));
                //WriteComment(string.Format("aspirate times : {0}", times+1), sw);
                
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
                ProcessSliceOnce(ptsAsp, volumes, sLiquidClass, srcRackIndex, slice, sampleIndexInRack, sw, transfer2Final);
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
            {
                aspirateVol = totalPlasmaVolume / totalSlice;//平均吸取
                aspirateVol = Math.Min(aspirateVol, pipettingSettings.maxVolumePerSlice);
            }
                

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

        private int GetDstWellStartID(int rackIndex, int sampleIndexInRack)
        {
            int srcWellStartID = rackIndex * labwareSettings.dstLabwareRows + sampleIndexInRack + 1;
            return srcWellStartID - 1 + dstPlateStartWellID;
        }

        private void ProcessSliceOnce(List<POINT> ptsAspOrg, List<double> volumes, string liquidClass ,
             int srcRackIndex,int sliceIndex, int sampleIndexInRack,StreamWriter sw,List<string> transfer2Final)
        {
            //有时候，液体需要被喷到不同列，比如目标载架为4*6时
            bool inSameColumn = IsDstWellsInSameColumn(srcRackIndex, sampleIndexInRack, ptsAspOrg.Count);
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
            string strAspirate = GenerateAspirateCommand(ptsAsp, volumes, liquidClass, srcGrid, 0, labwareSettings.sourceWells);
			sw.WriteLine(strAspirate);
            if (inSameColumn)
            {
                int srcWellStartID = srcRackIndex * labwareSettings.dstLabwareRows +sampleIndexInRack+  1;
                int dstWellStartIndex = srcWellStartID -1 + dstPlateStartWellID - 1;
                List<POINT> ptsDisp = positionGenerator.GetDstWells(dstWellStartIndex, volumes.Count);
                int grid = 0, site = 0;
                CalculateDestGridAndSite4OneSlicePerLabware(sliceIndex, ref grid, ref site);
                if (bNeedUseLastFour)
                {
                    POINT ptZero = new POINT(0, 0);
                    ptsDisp.InsertRange(0, new List<POINT>() { ptZero, ptZero, ptZero, ptZero });
                }
                string strDispense = GenerateDispenseCommand(ptsDisp, volumes, liquidClass, grid, site, labwareSettings.dstLabwareRows);
                sw.WriteLine(strDispense);
            }
            else
            {
                //throw new Exception("try to dispense to different regions.");
                int dstWellStartIndex = 0;
                int firstColumnSampleCnt = 0;
                int secondColumnStartSampleIndex = 0;
                GetTwoColumnInfo(srcRackIndex,sampleIndexInRack,ref dstWellStartIndex,ref firstColumnSampleCnt, ref secondColumnStartSampleIndex);
                List<POINT> ptsDisp = positionGenerator.GetDstWells(dstWellStartIndex, firstColumnSampleCnt);
                int grid = 0, site = 0;
                CalculateDestGridAndSite4OneSlicePerLabware(sliceIndex, ref grid, ref site);
                List<double> volumes1;
                List<double> volumes2;
                SplitVolumes2DifferentColumns(volumes, out volumes1, out volumes2, firstColumnSampleCnt);
                string strDispense = GenerateDispenseCommand(ptsDisp, volumes1, liquidClass, grid, site, labwareSettings.dstLabwareRows);
                sw.WriteLine(strDispense);
                ptsDisp = positionGenerator.GetDstWells(dstWellStartIndex, volumes.Count);
                strDispense = GenerateDispenseCommand(ptsDisp, volumes2, liquidClass, grid, site, labwareSettings.dstLabwareRows);
                sw.WriteLine(strDispense);
            }
        }

        private void GetTwoColumnInfo(int srcRackIndex, int sampleIndexInRack, ref int dstWellStartIndex, ref int firstColumnSampleCnt, ref int secondColumnStartSampleIndex)
        {
            dstWellStartIndex = GetDstWellStartID(srcRackIndex, sampleIndexInRack);
            int columnIndex = dstWellStartIndex / labwareSettings.dstLabwareRows;
            int firstColumnEndIndex = (columnIndex + 1) * labwareSettings.dstLabwareRows - 1;
            firstColumnSampleCnt = firstColumnEndIndex - dstWellStartIndex + 1;
            secondColumnStartSampleIndex = firstColumnEndIndex + 1;
        }

        private void SplitVolumes2DifferentColumns(List<double> volumes, out List<double> volumes1, out List<double> volumes2, int firstColumnSampleCount)
        {
            volumes1 = new List<double>();
            volumes2 = new List<double>();
            for (int i = 0; i < volumes.Count; i++)
            {
                if (i < firstColumnSampleCount)
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
        private void CalculateDestGridAndSite4OneSlicePerLabware(int plateIndex, ref int grid, ref int site)
        {
            int startGrid = labwareSettings.dstLabwareStartGrid;
            int sitesPerGrid = labwareSettings.sitesPerCarrier;
            int additionalGrids = labwareSettings.gridsPerCarrier * (plateIndex / sitesPerGrid);
            grid = startGrid + additionalGrids;
            site = plateIndex % labwareSettings.sitesPerCarrier;
        }

        

        private void CalculateDestPlasmaGridAndSite(int sampleIndex, int slice,ref int grid, ref int site)
        {
            int samplesPerRow = Utility.GetSamplesPerRow4Plasma(labwareSettings, pipettingSettings,false);
            int sampleCountPerLabware = samplesPerRow* labwareSettings.dstLabwareRows;
            int labwareCnt = labwareSettings.dstCarrierCnt * labwareSettings.sitesPerCarrier;
            int sampleCountPerCarrier = sampleCountPerLabware * labwareSettings.sitesPerCarrier;
         
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
                int buffyNeedGrids =  pipettingSettings.dstbuffySlice;
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
            CalculateDestPlasmaGridAndSite(sampleIndex, 0, ref grid, ref site);
            if( labwareSettings.gridsPerCarrier == 1)
                grid += pipettingSettings.dstPlasmaSlice;
        }


        private bool IsDstWellsInSameColumn(int srcRackIndex, int startSample, int sampleCount)
        {
            int nStartSampleIndex = srcRackIndex * labwareSettings.sourceWells + startSample + dstPlateStartWellID - 1;
            int nEndSampleIndex = nStartSampleIndex + sampleCount - 1;
            int wellsCount = labwareSettings.dstLabwareRows;
            int nCol1 = nStartSampleIndex / wellsCount;
            int nCol2 = nEndSampleIndex / wellsCount;
            return nCol1 == nCol2;
        }

     


        private void WriteDispenseBuffyNoCheck(List<POINT> pts, int grid,int site, StreamWriter sw, int startTipIndex = 0)
        {
            log.Info("WriteDispenseBuffy for certain region");
            int sampleCnt = pts.Count;
            int ditiMask = GetTipSelection(sampleCnt,startTipIndex);
            List<double> vols = new List<double>();
            for(int i = 0; i < 8; i++)
            {
                vols.Add(0);
            }
            for (int i = startTipIndex; i < startTipIndex+sampleCnt; i++)
            {
                vols[i] = 10;
            }

            WriteDispenseBuffyWithMovingPluger(pts, ditiMask, vols, grid, site, sw, startTipIndex);
        }

        private void WriteDispenseBuffyWithMovingPluger(List<POINT> pts, int ditiMask, List<double> vols, int grid, int site, StreamWriter sw, int startTipIndex = 0)
        {
            
            WriteDispenseByMovingPluger(pts, vols, ditiMask, grid, site, sw,startTipIndex);

            int sampleCnt = pts.Count;
            WriteComment(string.Format("Move LiHa up to {0}cm", pipettingSettings.retractHeightcm), sw);
            var sMoveAbsoluteZ = GetMoveLihaAbsoluteZSlow(sampleCnt, pipettingSettings.retractHeightcm, startTipIndex);
            WriteComand(sMoveAbsoluteZ, sw);

            WriteComment("Set end speed for plungers", sw);
            string sSEP = GetSEPString(sampleCnt, 2400,startTipIndex);
            WriteComand(sSEP, sw);
            WriteComment("Set stop speed for plungers", sw);
            string sSPP = GetSPPString(sampleCnt, 1500, startTipIndex);
            WriteComand(sSPP, sw);
            WriteComment(string.Format("Aspirate air gap: {0}", pipettingSettings.airGap), sw);
            string sPPA = GetPPAString(sampleCnt, pipettingSettings.airGap, startTipIndex);
            WriteComand(sPPA, sw);
        }

        private void WriteDispenseByMovingPluger(List<POINT> pts, List<double> vols,int ditiMask, 
            int grid, int site, StreamWriter sw,int startTipIndex =0)
        {
            string sVolumes = GetVolumeString(vols);
            string sWellSelection = GetWellSelection(labwareSettings.dstLabwareColumns, labwareSettings.dstLabwareRows, pts);
            //if(pipettingSettings.dstbuffySlice == 2)
            //{
            //    sWellSelection = GetWellSelection(1, 16, pts);
            //}
            string sDispense = string.Format(breakPrefix + "{0}({1},\"{2}\",{3}{4},{5},1,\"{6}\", 0, 0);", "Dispense",
                ditiMask, BB_Buffy, sVolumes, 
                grid, site, sWellSelection);
            WriteComment("Write Dispense for sample tracking", sw);
            sw.WriteLine(sDispense);

            int sampleCnt = pts.Count;
            WriteComment("Set end speed for plungers", sw);
            string sSEP = GetSEPString(sampleCnt, 2400,startTipIndex);
            WriteComand(sSEP, sw);
            WriteComment("Set stop speed for plungers", sw);
            string sSPP = GetSPPString(sampleCnt, 1500, startTipIndex);
            WriteComand(sSPP, sw);
            WriteComment("Move plunger to absolut position 0 (0ul -> dispense all liquid plus part of airgap)", sw);
            string sPPA = GetPPAString(sampleCnt, 0, startTipIndex);
            WriteComand(sPPA, sw);
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


        private void WriteDispenseBuffy(List<POINT> pts,int grid, int site, StreamWriter sw,int startTipIndex = 0)
        {
            //for safe liha movement.
            double maxWell = pts.Select(x => x.y).Max();
            int endTip = pts.Count;
         
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
            WriteDispenseBuffyNoCheck(pts, grid, site, sw, startTipIndex);
        }

        private void WriteDispenseBuffy(int rackIndex, 
            int sampleIndexThisRack,int plateIndex,int dstWellStartIndex,
            List<DetectedInfo> detectInfos,
            StreamWriter sw, int startTipIndex = 0)
        {
            log.Info("Write DispenseBuffy");
            List<POINT> ptsDisp = positionGenerator.GetDstWells(dstWellStartIndex, detectInfos.Count);
            int grid = 0, site = 0;
            CalculateDestGridAndSite4OneSlicePerLabware(plateIndex, ref grid, ref site);
            double buffyVol = pipettingSettings.buffyVolume / pipettingSettings.dstbuffySlice;
            WriteDispenseBuffy(ptsDisp, grid, site, sw, startTipIndex);
            if(pipettingSettings.dstbuffySlice == 2)
            {
                List<double> volumes = new List<double>();
                for(int i = 0; i< detectInfos.Count; i++)
                {
                    volumes.Add(buffyVol);
                }
                sw.WriteLine(GetComment("Aspirate buffy to 2nd slice."));
                sw.WriteLine(GenerateAspirateBuffy(ptsDisp, volumes, BB_Buffy, grid, site, labwareSettings.dstLabwareRows));
                CalculateDestGridAndSite4OneSlicePerLabware(plateIndex + 1, ref grid, ref site);
                sw.WriteLine(GenerateDispenseCommand(ptsDisp, volumes, BB_Buffy, grid, site, labwareSettings.dstLabwareRows));
            }
            
        }

        private List<POINT> GetTransferDispPts(int sampleIndexThisRack, List<DetectedInfo> detectInfos)
        {
            List<POINT> pts = new List<POINT>();
            for (int row = 0; row < detectInfos.Count; row++)
            {
                if(detectInfos[row].Z2 != 100)
                    pts.Add(new POINT(1, sampleIndexThisRack+row+1));
            }
            return pts;
        }

        private void WriteMSDCommands(StreamWriter sw, List<DetectedInfo> detectedInfos)
        {
            log.Info("Write MSD");
            int samplesInTheBatch = detectedInfos.Count;
            List<double> heights = detectedInfos.Select(x => x.Z2).ToList();
           
            //move tips to absolute position
            MoveTipsToAbsolutePosition(sw, heights);

            int buffyVol = pipettingSettings.buffyVolume + 20;
            int aspSpeed = 9;
            double speedFactor = pipettingSettings.buffySpeedFactor;
            int speedXY = (int)(60 * speedFactor);
            double area = mappingCalculator.GetArea(); 
            double totalHmm = pipettingSettings.buffyVolume * 10 / area;
            double adjustedZPerLayer = (totalHmm + pipettingSettings.msdZDistance * 10) / (2 * pipettingSettings.buffyAspirateLayers);
            
            int deltaXY = pipettingSettings.deltaXYForMSD;
            
            int accXY = 2000;
            int numSegments = 5;
           
            double aspVolumePerSpiral = buffyVol / (pipettingSettings.buffyAspirateLayers*2.0);
            int dialutorSteps =(int)(3.1 * aspVolumePerSpiral);
            int aspSpeedSteps = (int)(3.1 * aspSpeed * speedFactor * buffyVol / 300.0);
            //int tipOffset = bNeedUseLastFour ? 4 : 0;
            int tipSel = GetTipSelection(samplesInTheBatch);

            WriteComment("Set Move values",sw);
            string sSEP = GetSEPString(samplesInTheBatch, aspSpeedSteps);
            WriteComand(sSEP, sw);
            int totalZ = 0;
            for (int i = 0; i < pipettingSettings.buffyAspirateLayers; i++)
            {
                WriteComment(string.Format("Move LiHa spiral out -times: {0}",i+1), sw);
                //WriteComment("MSD deltaDistance, NrOfHalfSpirals, TipSelect, DilutorDistance, ZTrackingDistance, XYSpeed,", sw);
                string sMSDCommand = GetMSDCommand(deltaXY, numSegments, tipSel, dialutorSteps, speedXY, accXY);
                WriteComand(sMSDCommand, sw);
                WriteComment(string.Format("Move LiHa deltaZ down -times: {0} distance:{1}", i + 1, adjustedZPerLayer), sw);
                double adjustedZ = (i + 1) * adjustedZPerLayer;
                int thisLayerDeltaZ = (int)(adjustedZ - totalZ + 0.5);
                totalZ += thisLayerDeltaZ;
                string sMoveLihaDown = GetMoveLihaDown(samplesInTheBatch, -thisLayerDeltaZ);
                WriteComand(sMoveLihaDown, sw);
                WriteComment(string.Format("Move LiHa spiral in -times: {0}",i+1),sw);
                deltaXY = -deltaXY;
                sMSDCommand = GetMSDCommand(deltaXY, numSegments, tipSel, dialutorSteps, speedXY, accXY);
                WriteComand(sMSDCommand, sw);
                deltaXY = -deltaXY;
                if (i == pipettingSettings.buffyAspirateLayers - 1)
                    break;
                WriteComment(string.Format("Move LiHa deltaZ down -times: {0}",i+1), sw);
                WriteComand(sMoveLihaDown, sw);
            }
            WriteComment("Set stop speed for plungers", sw);
            string sSPP = GetSPPString(samplesInTheBatch, 1500);
            WriteComand(sSPP, sw);
            WriteComment(string.Format("Aspirate air gap: {0}", pipettingSettings.airGap), sw);
            string sPPA = GetPPAString(samplesInTheBatch, pipettingSettings.airGap);
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
                double z1 = heightsThisTime[i].Z1;
                double z2 = heightsThisTime[i].Z2;
                
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
                //sw.WriteLine(string.Format("{0};{1};{2};{3}{4}", startSampleID, plasmaSlice, pipettingSetting.dstPlasmaSlice, buffySlice, pipettingSetting));
            }
        }

        private void WriteRunResult(int srcRackIndex, int startSampleIndex, List<DetectedInfo> heightsThisTime, StreamWriter sw)
        {
            double area = mappingCalculator.GetArea();
            for (int i = 0; i < heightsThisTime.Count; i++)
            {
                double z1 = heightsThisTime[i].Z1;
                double z2 = heightsThisTime[i].Z2;
                double totalPlasmaVolume = (z1 - z2 - pipettingSettings.safeDelta) * area;
                int plasmaSlice = pipettingSettings.dstPlasmaSlice;
                if (pipettingSettings.plasmaGreedyVolume != 0)
                {
                    int maxPlasmaSlice = (int)Math.Ceiling(totalPlasmaVolume / pipettingSettings.plasmaGreedyVolume);
                    plasmaSlice = Math.Min(plasmaSlice, maxPlasmaSlice);
                }
                int startSampleID = srcRackIndex * labwareSettings.sourceWells + i + startSampleIndex + 1;
                int buffySlice = pipettingSettings.dstbuffySlice;//ResultReader.Instance.HasBuffyCoat() ? 1:0;
                sw.WriteLine(string.Format("{0};{1};{2};{3}{4}", startSampleID, plasmaSlice, pipettingSettings.dstPlasmaSlice, buffySlice, pipettingSettings));
            }

        }
        #endregion

    }
}
