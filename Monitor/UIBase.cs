using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Monitor
{
    public class UIBase : UIElement
    {
        protected double width;
        protected double height;
        public Size BoundingSize
        {
            set
            {
                this.width = value.Width;
                this.height = value.Height;
                InvalidateVisual();
            }
        }
        public UIBase(double w, double h)
        {
            width = w;
            height = h;
        }

        protected virtual void CalcuUsable(out double usableWidth, out double usableHeight)
        {
            usableWidth = width;
            usableHeight = height;
        }
    }
}
