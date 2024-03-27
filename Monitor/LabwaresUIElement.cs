using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Configuration;

namespace Monitor
{
    enum XYFULL
    {
        XFull = 0,
        YFull
    };

    enum Stage
    {
        Measure = 0,
        Pipetting
    };

    class EVOMeasureInfo
    {
        public int curSampleID;
        public bool go2MeasurePos;
        public bool bFinished;
        public EVOMeasureInfo(int curSmpID, bool bGo2MeasurePos)
        {
            this.curSampleID = curSmpID;
            this.go2MeasurePos = bGo2MeasurePos;
            bFinished = false;
        }
    }
    class EVOPipettingInfo
    {
        public int rackIndex;
        public int batchIndex;
        public int sliceIndex;
        public int tipsCount;
        public bool bFinished;
        public EVOPipettingInfo()
        {
            rackIndex = 0;
            sliceIndex = 0;
            batchIndex = 0;
            bFinished = false;
            tipsCount =  MonitorWindow.labwareSettings.tipCount;
        }
    }


    class ArrowHelper
    {

        public static void DrawArrow( Point ptStart,Point ptEnd,DrawingContext drawingContext)
        {
            Matrix matx = new Matrix();
            Point ptControl = new Point(Math.Max(ptStart.X, ptEnd.X), Math.Min(ptStart.Y, ptEnd.Y));

            //arrow
            Vector vectAll = ptStart - ptEnd;
            Vector vect = ptControl - ptEnd;
            if (vect.Length == 0)
                vect = vectAll;

            vect.Normalize();
            double ArrowLength = 20;
            vect *= ArrowLength;
            double ArrowAngle = 35;
            matx.Rotate(ArrowAngle / 2);
            Point ptArrowLeft = ptEnd + vect * matx;
            matx.Rotate(-ArrowAngle);
            Point ptArrowRight = ptEnd + vect * matx;

            Pen bluePen = new Pen(Brushes.Blue, 3);
            drawingContext.DrawLine(bluePen, ptArrowLeft, ptEnd);
            drawingContext.DrawLine(bluePen, ptArrowRight, ptEnd);
            string sData = string.Format("M {0},{1} C {2},{3} {4},{5} {6},{7}",
                ptStart.X, ptStart.Y,
                ptControl.X, ptControl.Y,
                ptControl.X, ptControl.Y,
                ptEnd.X, ptEnd.Y);
            Geometry geometry = Geometry.Parse(sData);
            Brush blueBrush = Brushes.Transparent;
            
            drawingContext.DrawGeometry(blueBrush, bluePen, geometry);

        }
    }

 
    struct LayoutInfo
    {
        public double xLeftMargin;
        public double xRightMargin;
        public double yTopMargin;
        public double yBottomMargin;
        public double cellX;
        public double cellY;
        public double cellXGap;
        public double cellYGap;
        public double xTotalUnits;
        public double yTotalUnits;
        public LayoutInfo(PipettingSettings pipettingSettings)
        {
            cellX = 3;
            cellY = 0.8;
            cellXGap = 0.6;
            cellYGap = 0.2;
            yTopMargin = 1;
            yBottomMargin = 2.6;
            xLeftMargin = 2;
            xRightMargin = xLeftMargin;
            xTotalUnits = xLeftMargin + xRightMargin;
            int tipCount = EVOPipettingInfoGlobal.Value.tipsCount;
            xTotalUnits += tipCount * cellX;
            xTotalUnits += (tipCount - 1) * cellXGap;

            yTotalUnits = yTopMargin + yBottomMargin;
            int totalSlice = pipettingSettings.dstbuffySlice + pipettingSettings.dstPlasmaSlice;

            yTotalUnits += totalSlice * cellY;
            yTotalUnits += (totalSlice - 1) * cellYGap;
        }
    }
    class PipettingUI : UIBase
    {
        int wellsPerLabware;
        //List<SampleInfo> smpInfos = new List<SampleInfo>();
        RunResult runResult = null;
        PipettingSettings pipettingSettings = new PipettingSettings();
        LayoutInfo layoutInfo;
        ProgressController prgController;
        int frame = 0;
        bool bFinished = false;
        public PipettingUI(double w, double h, int wellsPerLabware,ProgressController prgController)
            : base(w, h)
        {
            this.prgController = prgController;
            this.wellsPerLabware = wellsPerLabware;
            runResult = RunResultReader.Read();
            //smpInfos = RunResultReader.Read();
            pipettingSettings = Utility.LoadPipettingSettings();
            layoutInfo = new LayoutInfo(pipettingSettings);
        }

        

        public void UpdatePipettingProgress(int rackIndex, int batchIndex,int sliceIndex)
        {
            EVOPipettingInfoGlobal.Value.rackIndex = rackIndex;
            EVOPipettingInfoGlobal.Value.batchIndex = batchIndex;
            EVOPipettingInfoGlobal.Value.sliceIndex = sliceIndex;
            prgController.UpdatePipettingProgress();
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            frame++;
            base.OnRender(drawingContext);
            double usableWidth, usableHeight;
            CalcuUsable(out usableWidth, out usableHeight);
            int startID = EVOPipettingInfoGlobal.GetStartID(wellsPerLabware);
            int endID = startID + EVOPipettingInfoGlobal.Value.tipsCount - 1;
            List<int> thisBatchPlasmaSlices = GetThisBatchInfo(startID, endID);
            double unit = usableWidth / layoutInfo.xTotalUnits;
            
            double xStart = unit * layoutInfo.xLeftMargin;
            double yStart = unit * layoutInfo.yTopMargin;
            for (int i = 0; i < thisBatchPlasmaSlices.Count; i++)
            {
                double xPos = xStart + i * ( layoutInfo.cellX + layoutInfo.cellXGap)*unit;
                DrawSample(thisBatchPlasmaSlices[i], unit, xPos, yStart, drawingContext);
            }
        }

        private void DrawSample(int actualPlasmaSlices,double unit, double xPos, double yStart, DrawingContext drawingContext)
        {
            
            double boundYTopPos = unit * layoutInfo.yTopMargin * 0.3;
            double boundYBottomPos = 0;
            for (int i = 0; i < actualPlasmaSlices; i++)
            {
                double yPos = yStart + unit* i * (layoutInfo.cellY + layoutInfo.cellYGap);
                DrawSlice(xPos, yPos,unit,i, true, drawingContext);    
            }
            double yPlasmaBottomPos = yStart + unit * pipettingSettings.dstPlasmaSlice * 
                (layoutInfo.cellY + layoutInfo.cellYGap);
            boundYBottomPos = yPlasmaBottomPos + pipettingSettings.dstbuffySlice * unit * (layoutInfo.cellY + layoutInfo.cellYGap);
            for (int j = 0; j < runResult.buffySlice; j++)
            {
                double yPos = yPlasmaBottomPos + unit * j * (layoutInfo.cellY + layoutInfo.cellYGap);
                DrawSlice(xPos, yPos, unit, pipettingSettings.dstPlasmaSlice + j, false, drawingContext);    
            }
            Rect rcBound = new Rect(new Point(xPos, boundYTopPos),
                new Point(xPos + layoutInfo.cellX * unit,
                boundYBottomPos + unit * layoutInfo.cellY));
            drawingContext.DrawRectangle(Brushes.Transparent, new Pen(Brushes.Blue, 1), rcBound);

            DrawTubeBottom(xPos, boundYBottomPos + unit * layoutInfo.cellY, unit, drawingContext);
            
        }

        private void DrawTubeBottom(double xPos,double yPos,double unit, DrawingContext drawingContext)
        {
            Point ptBottomMid = new Point(xPos + unit * layoutInfo.cellX / 2,yPos);
            Point ptStart = new Point(xPos, yPos);
            Point ptEnd = new Point(xPos + unit * layoutInfo.cellX, yPos);
            Size sz = new Size(layoutInfo.cellX * unit / 2, layoutInfo.cellY * unit );
            //Data="M 10,100 A 100,50 45 1 0 200,100" />

            string sData = string.Format("M {0},{1} A {2},{3} 90 1 0 {4},{5}",
                ptStart.X, ptStart.Y,
                sz.Width,sz.Height,
                ptEnd.X, ptEnd.Y);
            Geometry geometry = Geometry.Parse(sData);

            drawingContext.DrawGeometry(Brushes.Red, new Pen(Brushes.Red, 1), geometry);
        }

        private void DrawSlice(double xPos, double yPos,double unit,int curSliceIndex, bool isPlasma, DrawingContext drawingContext)
        {
            double width = layoutInfo.cellX * unit;
            double height = layoutInfo.cellY * unit;
            int sliceIndex = EVOPipettingInfoGlobal.Value.sliceIndex;
            Brush brush = GetBrush(isPlasma,curSliceIndex, sliceIndex);
            
            //Brush brush = isPlasma ? Brushes.Orange : Brushes.LightBlue;
            Rect rc = new Rect(new Point(xPos,yPos),new Size(width,height));
            drawingContext.DrawRectangle(brush, new Pen(brush, 1), rc);
        }

        private Brush GetBrush(bool isPlasma, int curSliceIndex,int sliceIndex)
        {
            Brush brush = Brushes.Green;
            if (bFinished)
                return brush;
            if (!isPlasma) //buffy
            {
                if (curSliceIndex == sliceIndex)
                    brush = frame % 2 == 0 ? Brushes.Purple : Brushes.Green;
                else if( curSliceIndex > sliceIndex)
                    brush = Brushes.Purple;
            }
            else
            {
                if (curSliceIndex == sliceIndex)
                    brush = frame % 2 == 0 ? Brushes.Orange : Brushes.Green;
                else if (curSliceIndex > sliceIndex)
                    brush = Brushes.Orange;
            }
            return brush;
        }

        private List<int> GetThisBatchInfo(int startID, int endID)
        {
            List<int> tmpResult = new List<int>(runResult.plasmaRealSlices);
            tmpResult = tmpResult.Skip(startID - 1).ToList();
            return tmpResult.Take(endID - startID + 1).ToList();
        }

        protected override void CalcuUsable(out double usableWidth, out double usableHeight)
        {
            usableWidth = width;
            usableHeight = height;
            double screenRatio = this.width / this.height;
            double realRatio = layoutInfo.xTotalUnits / layoutInfo.yTotalUnits;
            if (realRatio > screenRatio)//x方向占满
            {
                usableHeight = height / (realRatio / screenRatio);
            }
            else //y方向占满
            {
                usableWidth = width / (screenRatio / realRatio);
            }
        }

        internal void FinishedPipetting()
        {
            bFinished = true;
            prgController.FinishedPipetting();
            InvalidateVisual();
        }
    }
    class EVOPipettingInfoGlobal
    {
        static EVOPipettingInfo pipettingInfo;
        public static EVOPipettingInfo  Value
        {
            get
            {
                if( pipettingInfo == null)
                    pipettingInfo = new EVOPipettingInfo();
                return pipettingInfo;
            }
            set
            {
                pipettingInfo = value;
            }
        }

        public static int GetStartID(int wellsPerLabware)
        {
            int startID = pipettingInfo.rackIndex * wellsPerLabware + pipettingInfo.batchIndex * pipettingInfo.tipsCount + 1;    
            return startID;
        }

        
        
    }
    class WorkTableUI : UIBase
    {
        int srcGrids;
        int wellsPerLabware;
        EVOMeasureInfo EVOMeasureInfo;
        ProgressController prgController;
        Stage curStage;
        int frame;
        int smpCount;
        
        List<List<Point>> srcWellPos = new List<List<Point>>();
        public WorkTableUI(double w, double h, int srcGrids , int wellsPerLabware,ProgressController prgController)
            :base(w,h)
        {
            width = w;
            height = h;
            this.prgController = prgController;
            this.srcGrids = srcGrids;
            this.wellsPerLabware = wellsPerLabware;
            frame = 0;
            curStage = Stage.Measure;
            smpCount = Helper.ReadSampleCount();
            EVOMeasureInfo = new EVOMeasureInfo(0,true);
            
        }
        public Stage CurStage
        {
            get
            {
                return curStage;
            }
            set
            {
                curStage = value;
                InvalidateVisual();
            }
        }
  
        public void MoveTube(int curSampleID, bool go2MeasurePos)
        {
            EVOMeasureInfo.curSampleID = curSampleID;
            EVOMeasureInfo.go2MeasurePos = go2MeasurePos;
            prgController.Start();
            prgController.SetMeasureProgress(curSampleID - 1, go2MeasurePos);
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            double usableWidth, usableHeight;
            CalcuUsable(out usableWidth, out usableHeight);
            frame++;
            //if (curStage == Stage.Measure)
            //{
            DrawSourceGridsMeasure(drawingContext, usableWidth, usableHeight);
            //}
        }

        private void DrawSourceGridsMeasure(DrawingContext drawingContext, double usableWidth, double usableHeight)
        {
            //throw new NotImplementedException();
            double carrierW = usableWidth/(5.0 + srcGrids);
            double carrierH = usableHeight*0.9;
            double xCarrierStart = 5 * carrierW;
            double yMargin = usableHeight * 0.05;
            double yCarrierStart = yMargin;

            //draw rects
            SolidColorBrush grayBrush = new SolidColorBrush(Colors.LightBlue);
            Pen bluePen = new Pen(new SolidColorBrush(Colors.Blue), 0.5);
            
            for (int i = 0; i < srcGrids; i++)
            {
                double carrierX = xCarrierStart + i * carrierW;
                double carrierY = yMargin;
                Rect rc = new Rect(new Point(carrierX,carrierY),new Size(carrierW*0.9,carrierH));
                drawingContext.DrawRectangle(
                    grayBrush,
                    bluePen, rc);
                DrawCircles(drawingContext, rc,i);
            }
        }

        private void DrawCircles(DrawingContext drawingContext, Rect rc, int carrierIndex)
        {
            srcWellPos.Clear();
            double margin = rc.Height * 0.05;
            double r =  (rc.Height-margin) / (2.5*wellsPerLabware+0.5);
            double yPos = margin/2.0 + rc.Top + 1.5 * r;
            double xPos = rc.Left + rc.Width / 2.0;
            Point ptOrg = new Point(yPos, yPos);

            int startID = carrierIndex * wellsPerLabware;
            for (int i = 0; i < wellsPerLabware; i++)
            {
                Point ptWell = new Point(xPos, yPos);
                int thisWellID = startID + i + 1;
                if (thisWellID > smpCount)
                    return;
                Color wellColor = GetWellColor(thisWellID);
                DrawCircle(drawingContext, ptWell, r,wellColor);
                yPos += 2.5 * r ;
            }
        }

     

        private Color GetWellColor(int thisID)
        {
            if (curStage == Stage.Measure)
                return GetWellColorMeasure(thisID);
            else
                return GetWellColorPipetting(thisID);
        }

        private Color GetWellColorPipetting(int thisID)
        {
            EVOPipettingInfo pipettingInfo = EVOPipettingInfoGlobal.Value;
            if (pipettingInfo.bFinished)
                return Colors.Green;

            int startID = EVOPipettingInfoGlobal.GetStartID(wellsPerLabware);
            int endID = startID + EVOPipettingInfoGlobal.Value.tipsCount - 1;
            if (thisID < startID)
            {
                return Colors.Green;
            }
            else if (thisID > endID)
                return Colors.Gray;
            else
                return frame % 2 == 0 ? Colors.Green : Colors.Yellow;

        }

        private Color GetWellColorMeasure(int thisID)
        {
            if (EVOMeasureInfo.bFinished)
                return Colors.Green;

            if (thisID < EVOMeasureInfo.curSampleID)
            {
                return Colors.Green;
            }
            else if (thisID == EVOMeasureInfo.curSampleID)
            {
                return frame % 2 == 0 ? Colors.Green : Colors.Yellow;
            }
            else
            {
                return Colors.Gray;
            }
        }

        private void DrawCircle(DrawingContext drawingContext, Point point, double r,Color color)
        {
            SolidColorBrush renderBrush = new SolidColorBrush(color);
            renderBrush.Opacity = 0.8;
            Pen thinPen = new Pen(new SolidColorBrush(Colors.Black), 1);
            drawingContext.DrawEllipse(renderBrush, thinPen, point, r, r);
        }

        protected override void CalcuUsable(out double usableWidth, out double usableHeight)
        {
            usableWidth = width;
            usableHeight = height;
            double screenRatio = this.width / this.height;
            double carrierHeight = 305 * wellsPerLabware / 16.0;
            double realRatio = (5 + srcGrids) * 24 / carrierHeight; //24 是一个Grid的宽度
            if (realRatio > screenRatio)//x方向占满
            {
                usableHeight = height/(realRatio / screenRatio);
                //xyFull = XYFULL.XFull;
            }
            else //y方向占满
            {
                usableWidth = width/(screenRatio / realRatio);
                //xyFull = XYFULL.YFull;
            }
        }

        internal void FinishedMeasure()
        {
            EVOMeasureInfo.bFinished = true;
            prgController.FinishedMeasure();
            InvalidateVisual();
        }

        internal void FinishedPipetting()
        {
            EVOPipettingInfoGlobal.Value.bFinished = true;
            InvalidateVisual();
        }
    }
}
