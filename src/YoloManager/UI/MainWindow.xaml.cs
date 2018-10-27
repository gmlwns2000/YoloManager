using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
using System.Xml.Serialization;

namespace YoloManager
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        DataBase dataViewModel;

        public MainWindow()
        {
            Setting.Load();

            InitializeComponent();

            dataViewModel = new DataBase();
            DataContext = dataViewModel;

            Closed += delegate
            {
                Setting.Save();
            };
        }

        private void Lst_Frames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Lst_Frames.ScrollIntoView(Lst_Frames.SelectedItem);
            editor.DataFrame = (DataFrame)Lst_Frames.SelectedItem;
        }

        private void window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    dataViewModel.CurrentModel?.PrevFrame();
                    e.Handled = true;
                    break;
                case Key.Right:
                    dataViewModel.CurrentModel?.NextFrame();
                    e.Handled = true;
                    break;
                case Key.Space:
                    dataViewModel.CurrentModel?.TrackFrame();
                    e.Handled = true;
                    break;
            }
        }
    }
}
