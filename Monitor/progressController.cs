using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Timers;
using System.Windows.Threading;
using System.Configuration;

namespace Monitor
{
    public struct PackageInfo
    {
        public int buffySlice;
        public int plasmaSlice;
        public int sampleCount;
   

        public PackageInfo(int smpCount, int plasmaSlice, int buffyCount)
        {
            // TODO: Complete member initialization
            sampleCount = smpCount;
            this.plasmaSlice = plasmaSlice;
            this.buffySlice = buffyCount;
        }
    }
    class ProgressController
    {
        public TextBlock txtThisStageUsed;
        public TextBlock txtThisStageRemaining;
        public ProgressBar prgThisStage;
        public TextBlock txtTotalUsed;
        public TextBlock txtTotalRemaining;
        public ProgressBar prgTotal;

        DateTime veryBegining = DateTime.Now;
        DateTime stageBegining = DateTime.Now;
        PackageInfo packageInfo;
        Timer timer = new Timer(1000);
        int totalSlice = 0;
        readonly string sInit = "--:--:--";

        bool stopCurStageTimer = false;

        public ProgressController(
            TextBlock txtThisStageUsed,
            TextBlock txtThisStageRemaining,
            TextBlock txtTotalUsed,
            TextBlock txtTotalRemaining,
            ProgressBar prgThisStage,
            ProgressBar prgTotal,
            PackageInfo packageInfo
            )
        {
            this.txtThisStageUsed = txtThisStageUsed;
            this.txtThisStageRemaining = txtThisStageRemaining;
            this.txtTotalRemaining = txtTotalRemaining;
            this.txtTotalUsed = txtTotalUsed;
            this.prgThisStage = prgThisStage;
            this.prgTotal = prgTotal;

            this.packageInfo = packageInfo;
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
        }

        public void Start()
        {
            if( !timer.Enabled)
                timer.Start();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ChangeTimeUsed(txtTotalUsed, veryBegining);
            ChangeTimeRemaining(txtTotalRemaining);
            if (stopCurStageTimer)
                return;
            ChangeTimeUsed(txtThisStageUsed, stageBegining);
            ChangeTimeRemaining(txtThisStageRemaining);
        }

        private void ChangeTimeRemaining(TextBlock txtBlock)
        {
            string sText = "";
            Action getText = () => sText = txtBlock.Text;
            txtBlock.Dispatcher.Invoke(getText);
            if (sText == sInit)
                return;
            if (sText == "")
                return;
            TimeSpan timeRemaing = TimeSpan.Parse(sText);
            int seconds = (int)timeRemaing.TotalSeconds;
            if (seconds == 0)
                return;

            seconds--;
            string s = TimeSpan.FromSeconds(seconds).ToString("c");
            SetText(txtBlock, s);
        }

        private void SetText(TextBlock txtBlock, string s)
        {
            Action changeText = ()=>txtBlock.Text = s;
            txtBlock.Dispatcher.Invoke(changeText);
        }

        private void SetProgress(ProgressBar prgBar, int val)
        {
            Action changeProgress = () => prgBar.Value = val;
            prgBar.Dispatcher.Invoke(changeProgress);
        }

        private void ChangeTimeUsed(TextBlock txtBlock, DateTime begining)
        {
            TimeSpan used = DateTime.Now - begining;
            int seconds = (int)used.TotalSeconds;
            string s = TimeSpan.FromSeconds(seconds).ToString("c");
            SetText(txtBlock, s);
        }

        public void FinishedMeasure()
        {
            SetProgress(prgThisStage, 100);
            stopCurStageTimer = true;
            stageBegining = DateTime.Now;
        }

        public void FinishedPipetting()
        {
            timer.Stop();
            SetText(txtTotalRemaining, "00:00:00");
            SetText(txtThisStageRemaining, "00:00:00");
            SetProgress(prgThisStage, 100);
            SetProgress(prgTotal, 100);
        }

        public void SetMeasureProgress(int tubeIndex, bool bGo2MeasurePos)
        {
            Start();
            int totalMoves = packageInfo.sampleCount * 2;
            double finishedMoves = tubeIndex * 2;
            if (finishedMoves == 0)
                return;

            if(!bGo2MeasurePos)
                finishedMoves += 1;
            TimeSpan timeUsed = DateTime.Now - stageBegining;
            int secondsUsed = (int)timeUsed.TotalSeconds;
            int timeRemaining = (int)(secondsUsed / finishedMoves * totalMoves);
            SetText(txtThisStageRemaining, TimeSpan.FromSeconds(timeRemaining).ToString("c"));
            int pipettingTime = (int)Math.Ceiling(packageInfo.sampleCount / 8.0)
                * (packageInfo.plasmaSlice) * 30;
            SetText(txtTotalRemaining, TimeSpan.FromSeconds(pipettingTime+ timeRemaining).ToString("c"));
            SetProgress(prgThisStage, (int)(100 * finishedMoves / totalMoves));
            SetProgress(prgTotal, (int)(50 * finishedMoves / totalMoves));
        }

        public void UpdatePipettingProgress()
        {
            Start();
            stopCurStageTimer = false;
            if (totalSlice == 0)
                totalSlice = GetTotalSlices();
            double finishedSlice = GetFinihsedSlice(EVOPipettingInfoGlobal.Value.rackIndex,
                EVOPipettingInfoGlobal.Value.batchIndex,
                EVOPipettingInfoGlobal.Value.sliceIndex);
            TimeSpan timeUsedThisStage = DateTime.Now - stageBegining;
            int secondsUsed = (int)timeUsedThisStage.TotalSeconds;
            TimeSpan timeUsedAll = DateTime.Now - veryBegining;
            if (finishedSlice == 0)
            {
                SetProgress(prgThisStage, 0);
                return;
            }

            int timeRemaining = (int)(secondsUsed / finishedSlice * totalSlice);
            string sTimeRemaining = TimeSpan.FromSeconds(timeRemaining).ToString("c");
            SetText(txtThisStageRemaining, sTimeRemaining);
            SetText(txtTotalRemaining, sTimeRemaining);
            int percent = (int)(100 * finishedSlice / totalSlice);
            SetProgress(prgThisStage, (percent));
            SetProgress(prgTotal, (percent));
        }

        private int GetFinihsedSlice(int curRackIndex, int curBatchIndex, int curSliceIndex)
        {
            int finishedSlices = 0;
            for (int i = 0; i < curRackIndex; i++)
            {
                int sliceCount = GetSlicesCountOfTheRack(i);
                finishedSlices += sliceCount;
            }

            finishedSlices += curBatchIndex * GetOneBatchSlice();
            finishedSlices += curSliceIndex;
            return finishedSlices;
        }

      

        private int GetTotalSlices()
        {

            int racks = (int)Math.Ceiling(packageInfo.sampleCount / 16.0);
            int totalSliceCount = 0;
            for (int i = 0; i < racks; i++)
            {
                int sliceCount = GetSlicesCountOfTheRack(i);
                totalSliceCount += sliceCount;
            }
            return totalSliceCount;
        }

        private int GetSlicesCountOfTheRack(int rackIndex)
        {
            int thisRackSamples = 16;
            int finishedSamples = rackIndex * 16;
            int remainingSamples = packageInfo.sampleCount - finishedSamples;
            if (remainingSamples < 16)
                thisRackSamples = remainingSamples;

            double tipsCount = EVOPipettingInfoGlobal.Value.tipsCount;
            int batches = (int)Math.Ceiling(thisRackSamples / tipsCount);
            return GetOneBatchSlice() * batches;
        }

        private int GetOneBatchSlice()
        {
            return packageInfo.plasmaSlice + packageInfo.buffySlice;
        }
    }
}
