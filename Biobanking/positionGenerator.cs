using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using Settings;

namespace Biobanking
{
    class PositionGenerator
    {
         PipettingSettings pipettingSettings = null;
         LabwareSettings labwareSettings = null;
         int totalSample = 0;
         private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
         public PositionGenerator(PipettingSettings pSettings, LabwareSettings lSettings, int nSample)
         {
             pipettingSettings = pSettings;
             labwareSettings = lSettings;
             totalSample = nSample;
         }

         public static  List<POINT> GetWells(int startWellIndex, int wellsCount, int x, int y)
         {
             List<POINT> pts = new List<POINT>();
             int endWellIndex = startWellIndex + wellsCount - 1;
             for (int i = startWellIndex; i <= endWellIndex; i++)
             {
                 int column = i / y;
                 int rowIndex = i - column * y;
                 pts.Add(new POINT((double)(1 + column), (double)(rowIndex + 1)));
             }
             return pts;
         }

         private static int GetWellID(int colIndex, int rowIndex)
         {
             int _row = 8;
             return colIndex * _row + rowIndex + 1;
         }
         public static int ParseWellID(string sWellID)
         {
             sWellID = sWellID.Trim();
             if (sWellID.Length > 3)
                 throw new Exception("WellID length must <=3!");

             if (sWellID.All(x => Char.IsDigit(x)))
             {
                 return int.Parse(sWellID);
             }
             sWellID = sWellID.ToUpper();
             char first = sWellID.First();
             if (char.IsLetter(first))
             {
                 string remain = sWellID.Substring(1);
                 int rowIndex = (int)(first - 'A');
                 int colIndex = int.Parse(remain) - 1;
                 return GetWellID(colIndex, rowIndex);
             }
             else
                 throw new Exception("Invalid WellID, must be digital or string likes A01!");
         }
     
         internal int AllowedSamples()
         {
             int columnsPerLabware = labwareSettings.dstLabwareColumns;


            int totalSlice = pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice;// + pipettingSettings.dstRedCellSlice;
             int samplesPerRow = columnsPerLabware / totalSlice;
             log.InfoFormat("samples per row is: {0}", samplesPerRow);

             int dispenseAllowed = samplesPerRow*labwareSettings.dstLabwareRows * labwareSettings.dstCarrierCnt * labwareSettings.sitesPerCarrier;
             

             if (columnsPerLabware == 1)
             {
                 dispenseAllowed = labwareSettings.dstLabwareRows * (labwareSettings.dstCarrierCnt / totalSlice) * labwareSettings.sitesPerCarrier;
             }
             log.InfoFormat("dispenseAllowed is: {0}", dispenseAllowed);

             int srcSampleAllowed = labwareSettings.sourceLabwareGrids * labwareSettings.sourceWells;
             log.InfoFormat("srcSampleAllowed is: {0}", srcSampleAllowed);

             return Math.Min(dispenseAllowed,srcSampleAllowed);

         }
   
        internal List<POINT> GetSrcWells(int startSample, int wellsCount)
        {
            List<POINT> pts = new List<POINT>();
            for (int i = startSample + 1; i < startSample + wellsCount + 1; i++)
            {
                pts.Add(new POINT(1, i));
            }
            return pts;
        }

        internal List<POINT> GetDestWellsOneSlicePerRegion(int srcRackIndex, int startSample, int sampleCount)
        {
            int nStartSampleIndex = srcRackIndex * labwareSettings.sourceWells + startSample;
            List<POINT> pts = new List<POINT>();
            for(int i = 0; i< sampleCount ; i++)
            {
                int nSampleIndex = nStartSampleIndex + i;
                int col = nSampleIndex / labwareSettings.dstLabwareRows + 1;
                int row = nSampleIndex - (col - 1) * labwareSettings.dstLabwareRows + 1;
                pts.Add(new POINT(col, row));
            }
            return pts;
        }

        internal List<POINT> GetDestWellsBuffyStandalone(int srcRackIndex, int sliceIndex, int startSample, int sampleCount)
        {
            return GetDestWells(srcRackIndex, sliceIndex - pipettingSettings.dstPlasmaSlice, startSample, sampleCount, true);
        }
        internal List<POINT> GetDestWells(int srcRackIndex, int sliceIndex, int startSample, int sampleCount,bool onlyBuffy = false)
        {
            if (pipettingSettings.onlyOneSlicePerLabware)
                return GetDestWellsOneSlicePerRegion(srcRackIndex, startSample, sampleCount);
            int nStartSampleIndex = srcRackIndex * labwareSettings.sourceWells + startSample;
            int nEndSampleIndex = nStartSampleIndex + sampleCount - 1;
            int totalRow = labwareSettings.dstLabwareRows;
            int plasmaSlice = onlyBuffy ? 0 : pipettingSettings.dstPlasmaSlice;
            
            //for pipetting plasma, when Buffy should be pipeted to standalone plate, the buffy slice should be 0
            bool plasmaWhenBuffyStandalone = GlobalVars.Instance.BuffyStandalone && !onlyBuffy;
            int buffySlice = plasmaWhenBuffyStandalone ? 0 : pipettingSettings.dstbuffySlice;
            int totalSlicePerSample = buffySlice + plasmaSlice;
            int samplesPerRow = onlyBuffy ? Utility.GetSamplesPerRow4Buffy(labwareSettings,pipettingSettings) :
                Utility.GetSamplesPerRow4Plasma(labwareSettings, pipettingSettings,GlobalVars.Instance.BuffyStandalone);
            int samplesPerLabware = samplesPerRow * labwareSettings.dstLabwareRows;

            int sampleIndexInLabware = nStartSampleIndex;
            while (sampleIndexInLabware >= samplesPerLabware)
                sampleIndexInLabware -= samplesPerLabware;

            int subRegion = sampleIndexInLabware / labwareSettings.dstLabwareRows;
            int subRegionUsedSlices = totalSlicePerSample * subRegion;

            while (nStartSampleIndex >= totalRow)
                nStartSampleIndex -= totalRow;

            while (nEndSampleIndex >= totalRow)
                nEndSampleIndex -= totalRow;

            int nStartSampleRow = nStartSampleIndex + 1;
            int nEndSampleRow = nEndSampleIndex + 1;
            List<POINT> pts = new List<POINT>();
            if ( labwareSettings.dstLabwareColumns == 1)
            {
                sliceIndex = 0;
            }

            int col = sliceIndex + subRegionUsedSlices + 1;

            for (int row = nStartSampleRow; row <= nEndSampleRow; row++)
            {
                pts.Add(new POINT(col, row));
            }
            return pts;
        }

        //internal List<POINT> GetDestWells(int srcRackIndex, int startSample, int sampleCount)
        //{
        //    int nStartSampleIndex = srcRackIndex * labwareSettings.sourceWells + startSample;
        //    int nEndSampleIndex = nStartSampleIndex + sampleCount -1;
        //    int totalRow = labwareSettings.dstRowCount;
        //    while (nStartSampleIndex >= totalRow)
        //        nStartSampleIndex -= totalRow;

        //    while (nEndSampleIndex >= totalRow)
        //        nEndSampleIndex -= totalRow;

        //    int nStartSampleRow = nStartSampleIndex + 1;
        //    int nEndSampleRow = nEndSampleIndex + 1;
        //    List<POINT> pts = new List<POINT>();
        //    for (int row = nStartSampleRow; row <= nEndSampleRow; row++)
        //    {
        //        pts.Add(new POINT(1, row));
        //    }
        //    return pts;
        //}


        //internal List<POINT> GetDestPlasmaWells(int srcRackIndex, int startSample, int sampleCount)
        //{
        //    return GetDestWells(srcRackIndex, startSample, sampleCount);
        //}

        //internal List<POINT> GetDestBuffyWells(int srcRackIndex, int startSample, int sampleCount)
        //{
        //    return GetDestWells(srcRackIndex, startSample, sampleCount);
        //}

       
    }
}
