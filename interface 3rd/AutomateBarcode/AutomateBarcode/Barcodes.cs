using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using AutomateBarcode.TissueManagement;

namespace AutomateBarcode
{
    class Barcodes
    {
        public List<Dictionary<Point, string>> allPackageScannedInfos = new List<Dictionary<Point, string>>();
        public List<Dictionary<Point, string>> allPackageInfos;
        public Dictionary<Point, string> thisRackScannedPackageInfos;
        public List<Dictionary<Point, string>> allPackageExpectedInfos = new List<Dictionary<Point, string>>();
        public Dictionary<Point, string> thisRackPackageExpectedInfos;

        public List<Dictionary<int, int>> allRackSmpPlasmaSlices = new List<Dictionary<int, int>>();
        public Dictionary<int, int> curRackPlasmaSlices;
        public Dictionary<int, int> smpPlasmaSlices;
        int curIndex = 0;
        public Barcodes()
        {
            smpPlasmaSlices = Global.Instance.smpPlasmaSlices;
            SplitByRack(smpPlasmaSlices);
        }
        public void ChangeRackIndex(int index)
        {
            curIndex = index;
            thisRackScannedPackageInfos = allPackageScannedInfos[index];
            curRackPlasmaSlices = allRackSmpPlasmaSlices[index];
            thisRackPackageExpectedInfos = allPackageExpectedInfos[index];
        }

        private void SplitByRack(Dictionary<int, int> smpPlasmaSlices)
        {
            int totalSliceCnt = smpPlasmaSlices.Count;
            while (smpPlasmaSlices.Any())
            {
                int remaining = smpPlasmaSlices.Count;
                int splitted = totalSliceCnt - remaining;
                var tmpPlasmaSlices = smpPlasmaSlices.Take(Math.Min(remaining, 16)).ToDictionary(x => x.Key, x => x.Value);
                allRackSmpPlasmaSlices.Add(tmpPlasmaSlices);
                var tmpPackageInfos = CreatePackInfo(tmpPlasmaSlices);
                allPackageExpectedInfos.Add(new Dictionary<Point, string>(tmpPackageInfos));
                allPackageScannedInfos.Add(new Dictionary<Point, string>());
                smpPlasmaSlices = smpPlasmaSlices.Except(tmpPlasmaSlices).ToDictionary(x => x.Key, x => x.Value);
            }
            ChangeRackIndex(0);
        }

        public bool IsTubeExists(int rowIndex, int colIndex)
        {
            int plasmaSliceCnt = curRackPlasmaSlices.ElementAt(rowIndex).Value;
            if (Global.Instance.buffySlice != 0 && colIndex >= Global.Instance.plasmaSlice)
            {
                return true;
            }
            return colIndex < plasmaSliceCnt;
        }

        private Dictionary<Point, string> CreatePackInfo(Dictionary<int, int> tmpPlasmaSlices)
        {
            int colCnt = GetColumnCnt();
            int rowCnt = tmpPlasmaSlices.Count;
            curRackPlasmaSlices = tmpPlasmaSlices;
            Dictionary<Point, string> tmpRackBarcodes = new Dictionary<Point, string>();
            for (int col = 0; col < colCnt; col++)
            {
                for (int row = 0; row < rowCnt; row++)
                {
                    bool bExist = IsTubeExists(row, col);
                    if (!bExist)
                        continue;
                    
                    Point pt = new Point(col + 1, row + 1);
                    int indexInAll = tmpRackBarcodes.Count + allPackageExpectedInfos.Sum(x => x.Count);//allPackageInfos.Sum(x=>x.Count);
                    tmpRackBarcodes.Add(pt, Global.Instance.expectedBarcodes[indexInAll]);
                }
            }
            return tmpRackBarcodes;
        }
       
        public int GetColumnCnt()
        {
            return Global.Instance.plasmaSlice + Global.Instance.buffySlice;
        }

        internal bool IsConsist(int columnIndex, int rowIndex, ref string errMsg)
        {
            Point pt = new Point(columnIndex + 1, rowIndex + 1);
            string expectedBarcode = thisRackPackageExpectedInfos[pt];
            string curBarcode = thisRackScannedPackageInfos.ContainsKey(pt)
                ? thisRackScannedPackageInfos[pt] : "";
            if (curBarcode == "")
                return true;
            bool isConsist = curBarcode == expectedBarcode;
            if (!isConsist)
                errMsg = GetInconsistWarning(expectedBarcode, curBarcode);
            return isConsist;
        }

        

        public string GetInconsistWarning(string expected, string actual)
        {
            return string.Format("期望条码为:{0}， 实际条码为:{1}", expected, actual);
        }
        private SampleType GetSampleType(int colIndex)
        {
            return colIndex < Global.Instance.plasmaSlice ? SampleType.plasma : SampleType.buffy;
        }


        private void AddTubeInfo4TheRack(int colCnt, List<Dictionary<Point, string>> allInfos,
            int rackIndex, List<TubeInfo> tubeInfos)
        {
            curRackPlasmaSlices = allRackSmpPlasmaSlices[rackIndex];
            var curRackInfos = allInfos[rackIndex];
            for (int c = 0; c < colCnt; c++)
            {
                for (int r = 0; r < curRackPlasmaSlices.Count; r++)
                {
                    if (!IsTubeExists(r, c))
                        continue;
                    int index = rackIndex * 16 + r;
                    Point pt = new Point(c + 1, r + 1);
                    SampleType smpType = GetSampleType(c);
                    double vol = Global.Instance.plasmaVolume;
                    if (smpType == SampleType.buffy)
                    {
                        vol = Global.Instance.buffyVolume;
                    }
                    TubeInfo tmpTubeInfo = new TubeInfo();
                    tmpTubeInfo.barcode = curRackInfos[pt];
                    tmpTubeInfo.sampleType = smpType.ToString();
                    tmpTubeInfo.sliceID = (c + 1).ToString();
                    tmpTubeInfo.srcBarcode = Global.Instance.srcBarcodes[index];
                    tmpTubeInfo.volumeUL = ((int)vol).ToString();
                    tubeInfos.Add(tmpTubeInfo);
                    //tubeInfos.Add(new TubeInfo(curRackInfos[pt], smpType,
                    //    c + 1, Global.Instance.expectedBarcodes[index], (int)vol));
                }
            }
        }
        public bool AllbarcodesAreExpected(ref string errMsg)
        {
            //List<Dictionary<Point, string>> allInfos = new List<Dictionary<Point, string>>(allPackageExpectedInfos);
            for (int rackIndex = 0; rackIndex < allPackageExpectedInfos.Count; rackIndex++)
            {
                foreach (var pair in allPackageExpectedInfos[rackIndex])
                {
                    if (allPackageScannedInfos[rackIndex].ContainsKey(pair.Key))
                    {
                        if (allPackageScannedInfos[rackIndex][pair.Key] != pair.Value)
                        {
                            errMsg = string.Format("样品载架：{0}上有不一致的条码，期望条码为{1}，实际条码为{2}"
                                , rackIndex + 1, pair.Value, allPackageScannedInfos[rackIndex][pair.Key]);
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private string FindMissedBarcode(List<string> barcodes)
        {
            string firstMissedBarcode = Global.Instance.expectedBarcodes.Where(x => !barcodes.Contains(x)).First();
            return firstMissedBarcode;
        }

        private string FindShouldNotExist(List<string> barcodes)
        {
            string shouldNotExistBarcode = "";
            foreach (string sBarcode in barcodes)
            {
                if (!Global.Instance.expectedBarcodes.Contains(sBarcode))
                {
                    shouldNotExistBarcode = sBarcode;
                    break;
                }
            }
            return shouldNotExistBarcode;
        }

        private List<string> GetAllBarcodes(int colCnt, int rowCnt, int rackIndex)
        {
            List<string> barcodes = new List<string>();
            allPackageInfos = GetCurrentInfos();
            Dictionary<Point, string> thisRackPackageInfos = allPackageInfos[rackIndex];
            for (int c = 0; c < colCnt; c++)
            {
                for (int r = 0; r < rowCnt; r++)
                {
                    if (IsTubeExists(r, c))
                    {
                        Point pt = new Point(c + 1, r + 1);
                        if (thisRackPackageInfos.ContainsKey(pt))
                        {
                            barcodes.Add(thisRackPackageInfos[pt]);
                        }
                    }
                }
            }
            return barcodes;
        }

        private List<Dictionary<Point, string>> GetCurrentInfos()
        {
            List<Dictionary<Point, string>> allInfos = new List<Dictionary<Point, string>>();
            foreach( var dict in allPackageExpectedInfos)
                allInfos.Add(dict.ToDictionary(x=>x.Key,x=>x.Value));
            for (int rackIndex = 0; rackIndex < allInfos.Count; rackIndex++ )
            {
                foreach (var pair in allPackageScannedInfos[rackIndex])
                {
                    allInfos[rackIndex][pair.Key] = pair.Value;
                }
            }
            return allInfos;
        }

        internal List<TubeInfo> GetTubeInfos()
        {
            List<TubeInfo> tubeInfos = new List<TubeInfo>();
            var allInfos = GetCurrentInfos();
            int colCnt = GetColumnCnt();
            for (int rackIndex = 0; rackIndex < allInfos.Count; rackIndex++)
            {
                AddTubeInfo4TheRack(colCnt, allInfos, rackIndex, tubeInfos);
            }
            ChangeRackIndex(curIndex);//recover
            return tubeInfos;
        }
    }
}
