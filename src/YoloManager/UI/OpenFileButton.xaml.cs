using Microsoft.Win32;
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

namespace YoloManager
{
    /// <summary>
    /// OpenFileButton.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OpenFileButton : UserControl
    {
        OpenFileDialog ofd = new OpenFileDialog();
        
        public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register("FileName", typeof(string), typeof(OpenFileButton),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public string FileName { get => (string)GetValue(FileNameProperty); set => SetValue(FileNameProperty, value); }

        public OpenFileButton()
        {
            InitializeComponent();
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            ofd.FileName = FileName;
            if(ofd.ShowDialog() != true)
                return;
            FileName = ofd.FileName;
        }
    }
}
