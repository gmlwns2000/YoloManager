using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
    /// ImageDataEditor.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DataFrameEditor : UserControl
    {
        public static readonly DependencyProperty DataFrameProperty = DependencyProperty.Register("DataFrame", typeof(DataFrame), typeof(DataFrameEditor),
            new FrameworkPropertyMetadata(new PropertyChangedCallback(OnImageDataPropertyChanged)));
        public DataFrame DataFrame { get => (DataFrame)GetValue(DataFrameProperty); set => SetValue(DataFrameProperty, value); }

        YoloWrapper wrapper;

        public DataFrameEditor()
        {
            InitializeComponent();
            DataContext = DataFrame;
        }

        static void OnImageDataPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var editor = (DataFrameEditor)obj;
            var frame = (DataFrame)e.NewValue;

            if (e.OldValue != null)
                ((DataFrame)e.OldValue).Annotations.CollectionChanged -= editor.Annotations_CollectionChanged;

            editor.DataContext = frame;
            editor.annoGrid.Children.Clear();

            if (frame == null)
                return;

            frame.Annotations.CollectionChanged += editor.Annotations_CollectionChanged;
            Task.Factory.StartNew(() =>
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CreateOptions = BitmapCreateOptions.None;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(frame.File.FullName, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                editor.Dispatcher.Invoke(() =>
                {
                    editor.img.Source = bitmap;
                });
            }, TaskCreationOptions.PreferFairness);
            editor.imgGrid.Width = frame.Width;
            editor.imgGrid.Height = frame.Height;

            foreach (var item in frame.Annotations)
            {
                editor.annoGrid.Children.Add(new BBoxControl(editor, item));
            }
        }

        void Annotations_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            annoGrid.Children.Clear();
            foreach (var item in DataFrame.Annotations)
            {
                annoGrid.Children.Add(new BBoxControl(this, item));
            }
        }

        void img_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                return;

            var pt = e.GetPosition(img);
            var timer = new DispatcherTimer();
            timer.Tick += delegate
            {
                if (Mouse.LeftButton == MouseButtonState.Released)
                {
                    timer.Stop(); return;
                }

                var nowpt = Mouse.GetPosition(img);
                if (Math.Pow(nowpt.X - pt.X, 2) + Math.Pow(nowpt.Y - pt.Y, 2) > 10)
                {
                    pt.X = pt.X / img.ActualWidth * DataFrame.Width;
                    pt.Y = pt.Y / img.ActualHeight * DataFrame.Height;
                    var anno = DataFrame.AddAnnotation(0, pt, 3, 3);
                    var control = GetBBox(anno);
                    control.ResizeMe(e, BBoxControl.DirectionCase.RightBottom);
                    timer.Stop();
                }
            };
            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Start();
        }

        BBoxControl GetBBox(ObjectAnnotation anno)
        {
            foreach (var item in annoGrid.Children)
            {
                if (item is BBoxControl bBox)
                {
                    if (bBox.Annotation == anno)
                        return bBox;
                }
            }
            return null;
        }

        void UpdateYoloModel()
        {
            Dispatcher.Invoke(() =>
            {
                wrapper?.Dispose();

                if (File.Exists(Setting.Current.YoloConfigPath) && File.Exists(Setting.Current.YoloWeightPath))
                {
                    var prg = new ProgressDialog(0);

                    Task.Factory.StartNew(() =>
                    {
                        wrapper = new YoloWrapper(Setting.Current.YoloConfigPath, Setting.Current.YoloWeightPath, 0);
                        prg.Close();
                    });
                    prg.ShowDialog();
                }
            });
        }

        void Bt_Detect_Click(object sender, RoutedEventArgs e)
        {
            Detect(DataFrame);
        }

        void Detect(DataFrame frame)
        {
            if (wrapper == null)
                UpdateYoloModel();

            if (wrapper == null || frame == null)
                return;

            var result = wrapper.Detect(frame.File.FullName);
            var thresh = Setting.Current.YoloThresh;
            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < frame.Annotations.Count; i++)
                {
                    var item = frame.Annotations[i];
                    if (item.IsTrackedResult)
                    {
                        frame.Annotations.RemoveAt(i);
                        i--;
                    }
                }
            });
            foreach (var item in result)
            {
                if (item.prob > thresh)
                {
                    var anno = new ObjectAnnotation(frame)
                    {
                        Class = (int)item.obj_id,
                        //TODO: WHAT IS ANSWER I HAVE NO IDEA.
                        X = (item.x + item.w / 2) / frame.Width,
                        Y = (item.y + item.h / 2) / frame.Height,
                        Width = item.w / frame.Width,
                        Height = item.h / frame.Height,
                        Prob = item.prob,
                        IsTrackedResult = true,
                    };
                    Dispatcher.Invoke(() =>
                    {
                        frame.AddAnnotation(anno);
                    });
                }
            }
        }

        private void Bt_DetectAll_Click(object sender, RoutedEventArgs e)
        {
            if (DataFrame == null)
                return;

            var datas = DataFrame.Parent.Datas;
            var prg = new ProgressDialog(datas.Count);

            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < datas.Count; i++)
                {
                    Detect(datas[i]);
                    prg.Dispatcher.Invoke(() => { prg.SetValue(i + 1); });
                }
                prg.Dispatcher.Invoke(() => prg.Close());
            });

            prg.ShowDialog();
        }
    }
}
