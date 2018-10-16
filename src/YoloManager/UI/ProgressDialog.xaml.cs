using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace YoloManager
{
    /// <summary>
    /// ProgressDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ProgressDialog : Window
    {
        bool fin = false;
        Stopwatch sw;

        public ProgressDialog(double max)
        {
            InitializeComponent();

            prg.Maximum = max;
            Owner = App.Current.MainWindow;
            sw = new Stopwatch();
            sw.Start();

            Closing += ProgressDialog_Closing;
        }

        void ProgressDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (fin)
                return;
            e.Cancel = true;
        }

        public new void Close()
        {
            if (System.Windows.Threading.Dispatcher.CurrentDispatcher != Dispatcher)
            {
                Dispatcher.Invoke(() =>
                {
                    close();
                });
            }
            else
            {
                close();
            }
        }

        void close()
        {
            fin = true;
            base.Close();
        }

        double lastMs = 0;
        double lastFrameMs = -1;
        public void SetValue(double value)
        {
            if (System.Windows.Threading.Dispatcher.CurrentDispatcher != Dispatcher)
            {
                Dispatcher.Invoke(() =>
                {
                    setValue(value);
                });
            }
            else
            {
                setValue(value);
            }
        }

        void setValue(double value)
        {
            if (value == prg.Value)
                return;

            var frameMs = (sw.ElapsedMilliseconds - lastMs) / (value - prg.Value);
            if (lastFrameMs == -1)
                lastFrameMs = frameMs;
            else
                lastFrameMs = lastFrameMs * 0.7 + frameMs * 0.3;
            var eta = TimeSpan.FromMilliseconds(lastFrameMs * (prg.Maximum - value));
            lastMs = sw.ElapsedMilliseconds;

            prg.Value = value;
            Title = $"{prg.Value}/{prg.Maximum} ({prg.Value / prg.Maximum * 100:F3}% ETA:{eta.ToString(@"hh\:mm\:ss")})";
        }
    }
}
