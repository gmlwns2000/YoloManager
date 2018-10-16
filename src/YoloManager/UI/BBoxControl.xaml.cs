using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace YoloManager
{
    /// <summary>
    /// BBoxControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BBoxControl : UserControl
    {
        public enum DirectionCase
        {
            LeftTop,
            LeftCenter,
            LeftBottom,
            CenterTop,
            CenterCenter,
            CenterBottom,
            RightTop,
            RightCenter,
            RightBottom,
        }

        public ObjectAnnotation Annotation { get; set; }
        DataFrameEditor parent;

        public BBoxControl(DataFrameEditor parent, ObjectAnnotation annotation)
        {
            InitializeComponent();

            DataContext = annotation;
            Annotation = annotation;

            this.parent = parent;

            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            UpdatePos();
        }

        void UpdatePos()
        {
            var parent = Annotation.Parent;
            Grid_Background.Width = Annotation.Width * parent.Width;
            Grid_Background.Height = Annotation.Height * parent.Height;
            Margin = new Thickness(Annotation.X * parent.Width, Annotation.Y * parent.Height, 0, 0);
            UpdateLayout();
        }

        void rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var dirCase = GetDirectionCase(e);
            if (dirCase == DirectionCase.CenterCenter)
                DragMove(e);
            else
                ResizeMe(e, dirCase);
        }

        private void rect_MouseMove(object sender, MouseEventArgs e)
        {
            var dirCase = GetDirectionCase(e);
            switch (dirCase)
            {
                case DirectionCase.LeftTop:
                    Cursor = Cursors.SizeNWSE;
                    break;
                case DirectionCase.LeftCenter:
                    Cursor = Cursors.SizeWE;
                    break;
                case DirectionCase.LeftBottom:
                    Cursor = Cursors.SizeNESW;
                    break;
                case DirectionCase.CenterTop:
                    Cursor = Cursors.SizeNS;
                    break;
                case DirectionCase.CenterCenter:
                    Cursor = Cursors.SizeAll;
                    break;
                case DirectionCase.CenterBottom:
                    Cursor = Cursors.SizeNS;
                    break;
                case DirectionCase.RightTop:
                    Cursor = Cursors.SizeNESW;
                    break;
                case DirectionCase.RightCenter:
                    Cursor = Cursors.SizeWE;
                    break;
                case DirectionCase.RightBottom:
                    Cursor = Cursors.SizeNWSE;
                    break;
            }
        }

        DirectionCase GetDirectionCase(MouseEventArgs e)
        {
            var pt = e.GetPosition(brd);
            if (pt.X > brd.ActualWidth)
            {
                if (pt.Y > brd.ActualHeight)
                    return DirectionCase.RightBottom;
                else if (pt.Y < 0)
                    return DirectionCase.RightTop;
                else
                    return DirectionCase.RightCenter;
            }
            else if (pt.X < 0)
            {
                if (pt.Y > brd.ActualHeight)
                    return DirectionCase.LeftBottom;
                else if (pt.Y < 0)
                    return DirectionCase.LeftTop;
                else
                    return DirectionCase.LeftCenter;
            }
            else
            {
                if (pt.Y > brd.ActualHeight)
                    return DirectionCase.CenterBottom;
                else if (pt.Y < 0)
                    return DirectionCase.CenterTop;
                else
                    return DirectionCase.CenterCenter;
            }
        }

        public void DragMove(MouseEventArgs e)
        {
            var startPt = e.GetPosition(parent.img);
            var startX = Annotation.X;
            var startY = Annotation.Y;
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(11);
            timer.Tick += delegate
            {
                if (Mouse.LeftButton == MouseButtonState.Released)
                    timer.Stop();
                var nowPt = Mouse.GetPosition(parent.img);
                Annotation.X = startX + (nowPt.X - startPt.X) / parent.img.ActualWidth;
                Annotation.Y = startY + (nowPt.Y - startPt.Y) / parent.img.ActualHeight;
                Annotation.X = Math.Min(1 - Annotation.Width / 2, Math.Max(Annotation.Width / 2, Annotation.X));
                Annotation.Y = Math.Min(1 - Annotation.Height / 2, Math.Max(Annotation.Height / 2, Annotation.Y));
                UpdatePos();
            };
            timer.Start();
        }

        public void ResizeMe(MouseEventArgs e, DirectionCase directionCase)
        {
            var startx1 = (Annotation.X - Annotation.Width / 2) * parent.img.ActualWidth;
            var starty1 = (Annotation.Y - Annotation.Height / 2) * parent.img.ActualHeight;
            var startx2 = (Annotation.X + Annotation.Width / 2) * parent.img.ActualWidth;
            var starty2 = (Annotation.Y + Annotation.Height / 2) * parent.img.ActualHeight;
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(11);
            timer.Tick += delegate
            {
                if (Mouse.LeftButton == MouseButtonState.Released)
                    timer.Stop();
                var nowPt = Mouse.GetPosition(parent.img);
                var nowx = nowPt.X;
                var nowy = nowPt.Y;

                double newx1 = 0, newx2 = 0, newy1 = 0, newy2 = 0;
                switch (directionCase)
                {
                    case DirectionCase.LeftTop:
                        newx1 = Math.Min(startx2, nowx);
                        newx2 = Math.Max(startx2, nowx);
                        newy1 = Math.Min(starty2, nowy);
                        newy2 = Math.Max(starty2, nowy);
                        break;
                    case DirectionCase.LeftCenter:
                        newx1 = Math.Min(startx2, nowx);
                        newx2 = Math.Max(startx2, nowx);
                        newy1 = starty1;
                        newy2 = starty2;
                        break;
                    case DirectionCase.LeftBottom:
                        newx1 = Math.Min(startx2, nowx);
                        newx2 = Math.Max(startx2, nowx);
                        newy1 = Math.Min(starty1, nowy);
                        newy2 = Math.Max(starty1, nowy);
                        break;
                    case DirectionCase.CenterTop:
                        newx1 = startx1;
                        newx2 = startx2;
                        newy1 = Math.Min(starty2, nowy);
                        newy2 = Math.Max(starty2, nowy);
                        break;
                    case DirectionCase.CenterBottom:
                        newx1 = startx1;
                        newx2 = startx2;
                        newy1 = Math.Min(starty1, nowy);
                        newy2 = Math.Max(starty1, nowy);
                        break;
                    case DirectionCase.RightTop:
                        newx1 = Math.Min(startx1, nowx);
                        newx2 = Math.Max(startx1, nowx);
                        newy1 = Math.Min(starty2, nowy);
                        newy2 = Math.Max(starty2, nowy);
                        break;
                    case DirectionCase.RightCenter:
                        newx1 = Math.Min(startx1, nowx);
                        newx2 = Math.Max(startx1, nowx);
                        newy1 = starty1;
                        newy2 = starty2;
                        break;
                    case DirectionCase.RightBottom:
                        newx1 = Math.Min(startx1, nowx);
                        newx2 = Math.Max(startx1, nowx);
                        newy1 = Math.Min(starty1, nowy);
                        newy2 = Math.Max(starty1, nowy);
                        break;
                }

                newx1 = Math.Min(parent.img.ActualWidth, Math.Max(0, newx1));
                newx2 = Math.Min(parent.img.ActualWidth, Math.Max(0, newx2));
                newy1 = Math.Min(parent.img.ActualHeight, Math.Max(0, newy1));
                newy2 = Math.Min(parent.img.ActualHeight, Math.Max(0, newy2));

                Annotation.X = newx1 / parent.img.ActualWidth + Annotation.Width / 2;
                Annotation.Y = newy1 / parent.img.ActualHeight + Annotation.Height / 2;
                Annotation.Width = Math.Max(3, newx2 - newx1) / parent.img.ActualWidth;
                Annotation.Height = Math.Max(3, newy2 - newy1) / parent.img.ActualHeight;

                UpdatePos();
            };
            timer.Start();
        }

        void rect_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Annotation.Class += e.Delta > 0 ? 1 : -1;
        }
    }
}
