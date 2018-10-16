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
using System.Windows.Shapes;

namespace YoloManager
{
    /// <summary>
    /// Settings.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingWindow : Window
    {
        static SettingWindow current;
        public static void ShowSettings()
        {
            if (current == null)
            {
                current = new SettingWindow();
                current.Show();
            }
            else
            {
                current.Activate();
            }
        }

        public SettingWindow()
        {
            InitializeComponent();

            DataContext = Setting.Current;

            Closed += delegate { Setting.Save(); current = null; };
        }
    }
}
