using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace SampleInfo
{
    internal class PlateViewer : FrameworkElement
    {
        Size _size;
        int _col = 12;
        int _row = 8;
        Size _szMargin;
        Pen defaultPen;
        string lastSelWell = "A1";
        Point ptCurrentWell;
        Point ptSelectedWell = new Point(-1, -1);
        List<int> notUsedWellIDs = new List<int>();
        public delegate void selectedWellChangedHandler(string sNewWell);
        public event selectedWellChangedHandler OnSelectedWellChanged;
        public PlateViewer(Size sz, Size szMargin, int col = 12, int row = 8)
        {
            _size = sz;
            _szMargin = szMargin;
            _col = col;
            _row = row;
            defaultPen = new Pen(Brushes.Black, 1);
            //this.MouseDown += new System.Windows.Input.MouseButtonEventHandler(MyFrameWorkElement_MouseDown);
            this.MouseUp += new System.Windows.Input.MouseButtonEventHandler(MyFrameWorkElement_MouseUp);
            this.MouseMove += new System.Windows.Input.MouseEventHandler(MyFrameWorkElement_MouseMove);

        }

        internal void SetNotUsed(List<int> wellIDs)
        {
            notUsedWellIDs = wellIDs;
            InvalidateVisual();
        }

        void MyFrameWorkElement_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point ptTmp = e.GetPosition(this);
            ptCurrentWell = GetPos(ptTmp);
            //int colStart = (int)((ptTmp.X - _szMargin.Width) / GetWellWidth());
            //int rowStart = (int)((ptTmp.Y - _szMargin.Height) / GetWellHeight());
            //ptEnd = e.GetPosition(this);
            //sWellDesc = GetDescription(ptEnd);
            this.InvalidateVisual(); // cause a render
        }


        internal string GetDescription(Point curPt)
        {
            curPt.X += GetWellWidth() / 2;
            curPt.Y += GetWellHeight() / 2;
            Point pos = GetPos(curPt);
            if (pos.Y >= 8)
                return "";
            string sWellDesc = string.Format("{0}{1} ", (char)(pos.Y + 'A'), (pos.X + 1));
            return sWellDesc;
        }

        void MyFrameWorkElement_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //AdjustStartEndPosition();
            var pt = e.GetPosition(this);
            ptSelectedWell = GetPos(pt);
            if (OnSelectedWellChanged != null)
            {
                string sWellDesc = string.Format("{0}{1} ", (char)(ptSelectedWell.Y + 'A'), (ptSelectedWell.X + 1));
                OnSelectedWellChanged(sWellDesc);
            }
              

            this.InvalidateVisual();
        }

        void MyFrameWorkElement_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //ptStart = e.GetPosition(this);
        }

        private Pen GetDefaultPen()
        {
            return new Pen(Brushes.DarkGray, 1);
        }

        private Pen CreateBorderPen()
        {
            return new Pen(Brushes.Blue, 4);
        }

        private double GetWellWidth()
        {
            return (_size.Width - _szMargin.Width) / _col;
        }

        private double GetWellHeight()
        {
            return (_size.Height - _szMargin.Height) / _row;
        }

        public Point AdjustPosition(Point pt)
        {
            Point ptNew = new Point();
            int col = (int)((pt.X - _szMargin.Width) / GetWellWidth());
            ptNew.X = col * GetWellWidth() + _szMargin.Width;

            int row = (int)((pt.Y - _szMargin.Height) / GetWellHeight());
            ptNew.Y = (row + 1) * GetWellHeight() + _szMargin.Height;
            return ptNew;
        }

        public List<int> GetWellsInRegion(Point ptStart, Point ptEnd)
        {
            List<int> wells = new List<int>();
            int xStart = (int)ptStart.X;
            int yStart = (int)ptStart.Y;

            int xEnd = (int)ptEnd.X;
            int yEnd = (int)ptEnd.Y;

            for (int y = yStart; y < yEnd; y++)
            {
                for (int x = xStart; x < xEnd; x++)
                {
                    wells.Add(x * _row + y + 1);
                }
            }
            return wells;
        }




        private void DrawWells(DrawingContext drawingContext)
        {
            for (int i = 0; i < 96; i++)
            {
                DrawWell(i + 1, drawingContext);
            }
        }

        public static void Convert(int wellID, out int col, out int row)
        {
            int _row = 8;
            col = (wellID - 1) / _row;
            row = wellID - col * _row - 1;
        }

        private void DrawWell(int wellID, DrawingContext drawingContext)
        {
            int col, row;
            Convert(wellID, out col, out row);
            int xStart = (int)(col * GetWellWidth() + _szMargin.Width);
            int yStart = (int)(row * GetWellHeight() + _szMargin.Height);
            Brush tmpBrush = Brushes.LightGreen;
            Pen pen = defaultPen;
            //bool hangOver = IsSamePosition(col, row, ptCurrentWell);
            bool empty = notUsedWellIDs.Contains(wellID);
            if (empty)
                tmpBrush = Brushes.Pink;
            //if (hangOver)
            //    tmpBrush = Brushes.Green;
            bool selected = IsSamePosition(col, row, ptSelectedWell);
            if (selected)
            {
                tmpBrush = Brushes.Orange;
               
                   
            }


            double height = GetWellHeight();
            drawingContext.DrawRectangle(tmpBrush, pen,
                    new Rect(new Point(xStart + 1, yStart + 1), new Size(GetWellWidth() - 1, height - 1)));
            if (selected  || empty)
            {
                string sDesc = GetDescription(new Point(xStart, yStart));
                var txt = new FormattedText(
                 sDesc,
                 System.Globalization.CultureInfo.CurrentCulture,
                 FlowDirection.LeftToRight,
                 new Typeface("Courier new"),
                 16,
                 Brushes.Black);
                drawingContext.DrawText(txt, new Point(xStart + GetWellWidth() / 3, yStart + GetWellHeight() / 3));
            }

        }

        private bool IsSamePosition(int col, int row, Point ptWell)
        {
            return (col == ptWell.X && row == ptWell.Y);
        }


       
        private void DrawLabels(DrawingContext drawingContext)
        {
            for (int x = 1; x < _col + 1; x++)
            {
                var txt = new FormattedText(
                x.ToString(),
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Courier new"),
                20,
                Brushes.Black);

                int xPos = (int)((x - 0.6) * GetWellWidth()) + (int)_szMargin.Width;

                drawingContext.DrawText(txt,
                new Point(xPos, _szMargin.Height - 20)
                );
                //drawingContext.DrawLine(new Pen(defaultLineBrush, 1), new Point(xPos, 0), new Point(xPos, _size.Height));
            }


            for (int y = 1; y < _row + 1; y++)
            {
                var txt = new FormattedText(
                ((char)('A' + y - 1)).ToString(),
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Courier new"),
                20,
                Brushes.Black);

                int yPos = (int)((y - 0.7) * GetWellHeight()) + (int)_szMargin.Height;

                drawingContext.DrawText(txt,
                new Point(0, yPos)
                );

                //drawingContext.DrawLine(new Pen(defaultLineBrush, 1), new Point(xPos, 0), new Point(xPos, _size.Height));
            }
        }

        private void DrawGrids(DrawingContext drawingContext)
        {

            for (int x = 1; x < _col; x++)
            {
                int xPos = (int)(x * GetWellWidth()) + (int)_szMargin.Width;
                drawingContext.DrawLine(defaultPen, new Point(xPos, _szMargin.Height), new Point(xPos, _size.Height));
            }

            for (int y = 1; y < _row; y++)
            {
                int yPos = (int)(y * GetWellHeight()) + (int)_szMargin.Height;
                drawingContext.DrawLine(defaultPen, new Point(_szMargin.Width, yPos), new Point(_size.Width, yPos));
            }
        }


        private void DrawBorder(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Transparent, defaultPen,
                new Rect(_szMargin.Width, _szMargin.Height, _size.Width - _szMargin.Width, _size.Height - _szMargin.Height));
        }

      

        private Point GetPos(Point pt)
        {
            int xStart = (int)((pt.X - _szMargin.Width) / GetWellWidth());
            int yStart = (int)((pt.Y - _szMargin.Height) / GetWellHeight());
            return new Point(xStart, yStart);
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            //draw border
            DrawBorder(drawingContext);
            //grids
            DrawGrids(drawingContext);
            //label
            DrawLabels(drawingContext);
            DrawWells(drawingContext);
        }

        private int GetWellID(int x, int y)
        {
            return x * _row + y + 1;
        }



        internal void SetSelectedWellID(int val)
        {
            int col, row;
            Convert(val, out col, out row);
            ptSelectedWell = new Point(col, row);
            InvalidateVisual();
        }
    }
}
