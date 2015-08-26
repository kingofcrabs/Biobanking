using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

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
     
         internal int AllowedSamples()
         {
             int columnsPerLabware = labwareSettings.dstLabwareColumns;
             

             int totalSlice = pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice + pipettingSettings.dstRedCellSlice;
             int samplesPerRow = columnsPerLabware / totalSlice;
             log.InfoFormat("samples per row is: {0}", samplesPerRow);

             int dispenseAllowed = samplesPerRow*labwareSettings.dstLabwareRows * labwareSettings.regions * labwareSettings.sitesPerRegion;
             

             if (columnsPerLabware == 1)
             {
                 dispenseAllowed = labwareSettings.dstLabwareRows * (labwareSettings.regions / totalSlice) * labwareSettings.sitesPerRegion;
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
        internal List<POINT> GetDestWells(int srcRackIndex, int sliceIndex, int startSample, int sampleCount)
        {
            int nStartSampleIndex = srcRackIndex * labwareSettings.sourceWells + startSample;
            int nEndSampleIndex = nStartSampleIndex + sampleCount - 1;
            int totalRow = labwareSettings.dstLabwareRows;
            int totalSlicePerSample = pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice + pipettingSettings.dstRedCellSlice;
         
            int samplesPerRow = Utility.GetSamplesPerRow(labwareSettings, pipettingSettings);// labwareSettings.dstLabwareColumns / totalSlicePerSample;
            int samplesPerLabware = samplesPerRow * labwareSettings.dstLabwareRows;

            int sampleIndexInLabware = nStartSampleIndex;
            while (sampleIndexInLabware >= samplesPerLabware)
                sampleIndexInLabware -= samplesPerLabware;

            int subRegion = sampleIndexInLabware / labwareSettings.dstLabwareRows;
            int sliceUsedSubRegion = totalSlicePerSample * subRegion;

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

            int col = sliceIndex + sliceUsedSubRegion + 1;
            //if (isBuffy && pipettingSettings.dstbuffySlice == 1)
            //{
            //    col = labwareSettings.dstLabwareColumns;
            //}

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
