using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace YoloManager
{
    /// <summary>
    /// DatasetOptionWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DatasetOptionWindow : Window
    {
        DataBase db;
        public DatasetOptionWindow(DataBase model)
        {
            InitializeComponent();
            DataContext = model;
            foreach (var item in model.Names)
            {
                Tb_Names.Text += item + "\n";
            }
            db = model;
        }

        void Tb_Names_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(db == null)
                return;

            db.Names.Clear();
            using(var reader = new StringReader(Tb_Names.Text))
            {
                var line = "";
                while ((line = reader.ReadLine()) != null)
                {
                    if(!string.IsNullOrEmpty(line))
                        db.Names.Add(line);
                }
            }
        }
    }
}
