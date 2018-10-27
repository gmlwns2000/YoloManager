using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace YoloManager
{
    public class Setting : NotifyPropertyBase
    {
        public static Setting Current { get; private set; }

        public static void Load()
        {
            string path = Path.Combine(Environment.CurrentDirectory, "Settings.xml");
            if (File.Exists(path))
            {
                try
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(Setting));
                    using (StreamReader rdr = new StreamReader(path))
                    {
                        var decoded = (Setting)xmlSerializer.Deserialize(rdr);
                        Current = decoded;
                    }
                }
                catch (Exception e)
                {
                    Current = new Setting();
                }
            }
            else
            {
                Current = new Setting();
            }
        }

        public static void Save()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Setting));
            using (StreamWriter wr = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "Settings.xml")))
            {
                xmlSerializer.Serialize(wr, Current);
            }
        }

        string lastLoadDir = "";
        public string LastLoadDir { get => lastLoadDir; set { lastLoadDir = value; OnPropertyChanged(); } }

        string yoloConfigPath = "";
        public string YoloConfigPath { get => yoloConfigPath; set { yoloConfigPath = value; OnPropertyChanged(); } }

        string yoloWeightPath = "";
        public string YoloWeightPath { get => yoloWeightPath; set { yoloWeightPath = value; OnPropertyChanged(); } }

        double yoloThresh = 0.2;
        public double YoloThresh { get => yoloThresh; set { yoloThresh = value; OnPropertyChanged(); } }

        string trainConfig = "";
        public string TrainConfig { get => trainConfig; set { trainConfig = value; OnPropertyChanged(); } }

        string trainPreTrained = "darknet53.conv.74";
        public string TrainPreTrained { get => trainPreTrained; set { trainPreTrained = value; OnPropertyChanged(); } }

        int trackingModel = 0;
        public int TrackingModel { get => trackingModel; set { trackingModel = value; OnPropertyChanged(); } }

        public string[] TrackingModelTypes => new[] {
            "TLD",
            "MIL",
            "MOSSE",
            "MedianFlow",
            "GOTURN",
        };
    }
}
