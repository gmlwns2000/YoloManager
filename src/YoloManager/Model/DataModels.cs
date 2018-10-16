using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace YoloManager
{
    public class ObjectAnnotation : NotifyPropertyBase
    {
        int cls = -1;
        public int Class { get => cls; set { cls = Math.Max(0, value); OnPropertyChanged(); OnPropertyChanged(nameof(ClassColor)); } }
        public Color ClassColor
        {
            get
            {
                switch (Class)
                {
                    case 0:
                        return Colors.Lime;
                    case 1:
                        return Colors.Magenta;
                    case 2:
                        return Colors.Cyan;
                    case 3:
                        return Colors.Yellow;
                    case 4:
                        return Colors.DarkCyan;
                    case 5:
                        return Colors.Tomato;
                    case 6:
                        return Colors.SlateBlue;
                    case 7:
                        return Colors.DodgerBlue;
                    case 8:
                        return Colors.Orange;
                    case 9:
                        return Colors.Indigo;
                    default:
                        return RandColor(Class);
                }
            }
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public bool IsTrackedResult { get; set; }

        double prob;
        public double Prob { get => prob; set { prob = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProbColor)); } }
        public Color ProbColor
        {
            get
            {
                var thresh = Setting.Current.YoloThresh;
                var p = (prob - thresh) / (1 - thresh);
                return Color.FromRgb((byte)((1 - p) * 255), 0, (byte)(p * 255));
            }
        }

        public DataFrame Parent { get; private set; }

        public ICommand DeleteCommand => new CommandHandler(() => Delete(), true);

        public ObjectAnnotation(DataFrame parent)
        {
            Parent = parent;
        }

        public ObjectAnnotation(DataFrame parent, string note) : this(parent)
        {
            var noteSpl = note.Split(' ');
            var noteInt = ToDoubleArray(noteSpl);
            Class = (int)noteInt[0];
            X = noteInt[1];
            Y = noteInt[2];
            Width = noteInt[3];
            Height = noteInt[4];
        }

        double[] ToDoubleArray(string[] data)
        {
            var ret = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                ret[i] = Convert.ToDouble(data[i]);
            }
            return ret;
        }

        public override string ToString()
        {
            return $"{Class} {X} {Y} {Width} {Height}";
        }

        public void Delete()
        {
            Parent.Annotations.Remove(this);
        }

        Color RandColor(int seed)
        {
            var rnd = new Random(seed);
            return Color.FromRgb((byte)rnd.Next(255), (byte)rnd.Next(255), (byte)rnd.Next(255));
        }
    }

    public class DataFrame : NotifyPropertyBase
    {
        BitmapImage imageSource;
        public BitmapImage ImageSource { get => imageSource; set { imageSource = value; OnPropertyChanged(); } }

        public FileInfo File { get; set; }

        public ObservableCollection<ObjectAnnotation> Annotations { get; set; } = new ObservableCollection<ObjectAnnotation>();

        public double Width { get; set; }
        public double Height { get; set; }

        public DataModel Parent { get; set; }

        public DataFrame(DataModel parent, FileInfo file)
        {
            Parent = parent;

            File = file;

            ImageSource = new BitmapImage();
            ImageSource.BeginInit();
            ImageSource.UriSource = new Uri(file.FullName, UriKind.Absolute);
            ImageSource.DecodePixelWidth = 80;
            ImageSource.CreateOptions = BitmapCreateOptions.None;
            ImageSource.CacheOption = BitmapCacheOption.OnLoad;
            ImageSource.EndInit();
            ImageSource.Freeze();

            var decoder = BitmapDecoder.Create(new Uri(file.FullName, UriKind.Absolute), BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default);
            Width = decoder.Frames[0].PixelWidth;
            Height = decoder.Frames[0].PixelHeight;

            var note = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file.FullName), System.IO.Path.GetFileNameWithoutExtension(file.Name) + ".txt");
            var noteInfo = new FileInfo(note);
            if (noteInfo.Exists)
            {
                var lines = System.IO.File.ReadAllLines(noteInfo.FullName);
                foreach (var line in lines)
                {
                    Annotations.Add(new ObjectAnnotation(this, line));
                }
            }
        }

        public ObjectAnnotation AddAnnotation(int Class, Point pt, double W, double H, bool isAbs = true)
        {
            var anno = new ObjectAnnotation(this)
            {
                Class = Class,
                X = (pt.X + W / 2),
                Y = (pt.Y + H / 2),
                Width = W,
                Height = H,
            };

            if (isAbs)
            {
                anno.X /= Width;
                anno.Y /= Height;
                anno.Width /= Width;
                anno.Height /= Height;
            }

            return AddAnnotation(anno);
        }

        public ObjectAnnotation AddAnnotation(ObjectAnnotation anno)
        {
            Annotations.Add(anno);
            return anno;
        }

        public void Save()
        {
            var note = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(File.FullName), System.IO.Path.GetFileNameWithoutExtension(File.Name) + ".txt");
            var noteInfo = new FileInfo(note);
            if (!noteInfo.Exists)
                noteInfo.Create().Dispose();
            var builder = new StringBuilder();
            foreach (var item in Annotations)
            {
                builder.AppendLine(item.ToString());
            }
            System.IO.File.WriteAllText(note, builder.ToString());
        }
    }

    public class DataModel : NotifyPropertyBase
    {
        bool useForTrain = true;
        public bool UseForTrain { get => useForTrain; set { useForTrain = value; OnPropertyChanged(); } }

        public DirectoryInfo TargetDir { get; set; }

        public ObservableCollection<DataFrame> Datas { get; set; }

        int currentIndex = 0;
        public int CurrentIndex { get => currentIndex; set { currentIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentFrame)); } }
        public DataFrame CurrentFrame => (currentIndex > -1 && currentIndex < Datas.Count) ? Datas[currentIndex] : null;

        int notesCount = 0;
        public int NotesCount { get => notesCount; set { notesCount = value; OnPropertyChanged(); } }

        public CommandHandler NextFrameCommand => new CommandHandler(() => NextFrame(), true);
        public CommandHandler PrevFrameCommand => new CommandHandler(() => PrevFrame(), true);

        public DataModel(DirectoryInfo info)
        {
            TargetDir = info;
            var list = new List<DataFrame>();
            var files = info.GetFiles();
            var cores = Environment.ProcessorCount * 4;
            Parallel.For(0, cores, (id) =>
            {
                for (int i = id; i < files.Length; i += cores)
                {
                    var item = files[i];
                    if (item.Extension == ".jpg")
                    {
                        var d = new DataFrame(this, item);
                        lock (list)
                            list.Add(d);
                    }
                }
            });
            Datas = new ObservableCollection<DataFrame>(list.OrderBy(k => k.File.FullName));

            DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Normal, Application.Current.Dispatcher);
            timer.Interval = TimeSpan.FromMilliseconds(250);
            timer.Tick += delegate
            {
                int c = 0;
                foreach (var item in Datas)
                {
                    c += item.Annotations.Count;
                }
                NotesCount = c;
            };
            timer.Start();
        }

        public void NextFrame()
        {
            CurrentIndex = Math.Max(0, Math.Min(Datas.Count - 1, CurrentIndex + 1));
        }

        public void PrevFrame()
        {
            CurrentIndex = Math.Max(0, Math.Min(Datas.Count - 1, CurrentIndex - 1));
        }

        public void Save()
        {
            foreach (var item in Datas)
            {
                item.Save();
            }
        }

        public override string ToString()
        {
            return TargetDir.ToString();
        }
    }

    public class DataBaseTrainerModel : NotifyPropertyBase
    {
        public static void StartTrainer(DataBaseModel db)
        {
            var model = new DataBaseTrainerModel(db);
            var wnd = new TrainerWindow(model);
            wnd.Show();
        }

        public DataBaseModel Parent { get; set; }

        public ICommand TrainCommand => new CommandHandler(() => Train(), true);
        public ICommand KillCommand => new CommandHandler(() => Kill(), true);

        //Settings
        string modelConfigPath = Setting.Current.TrainConfig;
        public string ModelConfigPath { get => modelConfigPath; set { Setting.Current.TrainConfig = modelConfigPath = value; OnPropertyChanged(); } }

        string preTrainedWeightPath = Setting.Current.TrainPreTrained;
        public string PreTrainedWeightPath { get => preTrainedWeightPath; set { Setting.Current.TrainPreTrained = preTrainedWeightPath = value; OnPropertyChanged(); } }

        ProcessPriorityClass processPriority = ProcessPriorityClass.AboveNormal;
        public ProcessPriorityClass ProcessPriority { get => processPriority; set { processPriority = value; if (process != null) process.PriorityClass = value; OnPropertyChanged(); } }

        //Outputs
        bool isTraining = false;
        public bool IsTraining { get => isTraining; set { isTraining = value; OnPropertyChanged(); } }

        DateTime startTime;
        public DateTime StartTime { get => startTime; set { startTime = value; OnPropertyChanged(); } }
        DateTime endTime;
        public DateTime EndTime { get => endTime; set { endTime = value; OnPropertyChanged(); } }
        TimeSpan trainingTime;
        public TimeSpan TrainingTime { get => trainingTime; set { trainingTime = value; OnPropertyChanged(); } }

        ObservableCollection<string> stdOutput = new ObservableCollection<string>();
        public ObservableCollection<string> StdOutput { get => stdOutput; set { stdOutput = value; OnPropertyChanged(); } }

        Process process;
        Dispatcher dispatcher;

        public DataBaseTrainerModel(DataBaseModel Parent)
        {
            this.Parent = Parent;
            dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void Train()
        {
            if (process != null)
                return;

            if (Parent.Datas.Count < 1)
            { MessageBox.Show("Not found : Datas"); return; }

            if (!Directory.Exists(Parent.BackupPath))
                Directory.CreateDirectory(Parent.BackupPath);

            if (!File.Exists(Parent.DataDescPath))
            { MessageBox.Show("Not found : DataDescPath"); return; }

            if (!File.Exists(Parent.TrainDataPath))
            { MessageBox.Show("Not found : TrainDataPath"); return; }

            if (!File.Exists(ModelConfigPath))
            { MessageBox.Show("Not found : ModelConfigPath"); return; }

            if (!File.Exists(PreTrainedWeightPath))
            { MessageBox.Show("Not found : PreTrainedWeightPath"); return; }

            IsTraining = true;
            StartTime = DateTime.Now;

            StdOutput.Clear();
            process = Process.Start(
                new ProcessStartInfo("darknet.exe", $"detector train \"{Parent.DataDescPath}\" \"{ModelConfigPath}\" \"{PreTrainedWeightPath}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                }
            );
            process.PriorityClass = ProcessPriority;
            process.EnableRaisingEvents = true;
            process.Exited += Process_Exited;

            var dispatcher = Dispatcher.CurrentDispatcher;
            Task.Factory.StartNew(() =>
            {
                var proc = process;
                while (!proc.StandardOutput.EndOfStream && IsTraining)
                {
                    string line = proc.StandardOutput.ReadLine();
                    dispatcher.Invoke(() =>
                    {
                        StdOutput.Insert(0, line);
                        if (StdOutput.Count > 2500)
                            StdOutput.RemoveAt(StdOutput.Count - 1);
                        TrainingTime = DateTime.Now - startTime;
                    });
                }
            });

            var timer = new DispatcherTimer();
            timer.Tick += delegate
            {
                TrainingTime = DateTime.Now - startTime;
            };
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Start();
        }

        void Process_Exited(object sender, EventArgs e)
        {
            IsTraining = false;

            EndTime = DateTime.Now;
            TrainingTime = EndTime - StartTime;
            dispatcher.Invoke(() => StdOutput.Insert(0, "Program Exited --"));
            process = null;
        }

        public void Kill()
        {
            if (process == null)
                return;

            if (process.HasExited)
                return;
            process.Kill();
            process = null;
        }
    }

    public class DataBaseModel : NotifyPropertyBase
    {
        ObservableCollection<DataModel> datas = new ObservableCollection<DataModel>();
        public ObservableCollection<DataModel> Datas { get => datas; set { datas = value; OnPropertyChanged(); } }

        ObservableCollection<string> names = new ObservableCollection<string>();
        public ObservableCollection<string> Names { get => names; set { names = value; OnPropertyChanged(); } }

        int classCount = 1;
        public int ClassCount { get => classCount; set { classCount = value; OnPropertyChanged(); } }

        int currentIndex = 0;
        public int CurrentIndex { get => currentIndex; set { currentIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentModel)); } }
        public DataModel CurrentModel => (currentIndex > -1 && currentIndex < datas.Count) ? datas[currentIndex] : null;

        public ICommand OpenCommand => new CommandHandler(() => Load(), true);
        public ICommand SaveCommand => new CommandHandler(() => Save(), true);
        public ICommand SettingCommand => new CommandHandler(() => SettingWindow.ShowSettings(), true);
        public ICommand DatasetCommand => new CommandHandler(() => new DatasetOptionWindow(this).ShowDialog(), true);
        public ICommand TrainerCommand => new CommandHandler(() => DataBaseTrainerModel.StartTrainer(this), true);

        public string TrainDataPath => Path.Combine(TargetDir.FullName, "train.txt");
        public string TestDataPath => Path.Combine(TargetDir.FullName, "test.txt");
        public string NamesPath => Path.Combine(TargetDir.FullName, "obj.names");
        public string BackupPath => Path.Combine(TargetDir.FullName, "backups");
        public string DataDescPath => Path.Combine(TargetDir.FullName, "obj.data");

        public DirectoryInfo TargetDir { get; set; }

        System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog()
        {
            SelectedPath = Setting.Current.LastLoadDir,
        };
        Dispatcher Dispatcher;

        public DataBaseModel()
        {
            Dispatcher = Dispatcher.CurrentDispatcher;
        }

        Task loadTask;
        public void Load()
        {
            if (loadTask != null && (!loadTask.IsCanceled && !loadTask.IsCompleted && !loadTask.IsFaulted))
                return;

            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            TargetDir = new DirectoryInfo(fbd.SelectedPath);
            var dirs = TargetDir.GetDirectories();
            var prg = new ProgressDialog(dirs.Length);

            datas.Clear();
            loadTask = Task.Factory.StartNew(() =>
            {
                Setting.Current.LastLoadDir = fbd.SelectedPath;
                Setting.Save();

                int ind = 0;
                foreach (var item in dirs)
                {
                    var m = new DataModel(item);
                    Dispatcher.Invoke(() => { datas.Add(m); prg.SetValue(ind); });
                    ind++;
                }
                loadTask = null;
                Dispatcher.Invoke(() => { CurrentIndex = CurrentIndex; prg.Close(); });
            });

            if (File.Exists(DataDescPath))
            {
                foreach (var line in File.ReadAllLines(DataDescPath))
                {
                    if (line.Trim().ToLower().StartsWith("classes"))
                    {
                        var spl = line.Trim().Split('=');
                        if (spl.Length > 1)
                        {
                            ClassCount = Convert.ToInt32(spl[1]);
                        }
                    }
                }
            }

            prg.ShowDialog();
        }

        public void Save()
        {
            if (datas.Count <= 0)
                return;

            var prg = new ProgressDialog(Datas.Count);

            Task.Factory.StartNew(() =>
            {
                var i = 0;
                var files = new List<FileInfo>();
                foreach (var model in Datas)
                {
                    model.Save();
                    if (model.UseForTrain)
                    {
                        foreach (var frame in model.Datas)
                        {
                            files.Add(frame.File);
                        }
                    }
                    prg.SetValue(i);
                    i++;
                }
                prg.SetValue(i);
                i++;

                var trainBuilder = new StringBuilder();
                var testBuilder = new StringBuilder();
                var rand = new Random((int)(DateTime.Now.TimeOfDay.TotalMilliseconds * 1000));
                var testRatio = 0.02;
                foreach (var item in files)
                {
                    var r = rand.Next() % 10000;
                    if (r > testRatio * 10000)
                        trainBuilder.AppendLine(item.FullName);
                    else
                        testBuilder.AppendLine(item.FullName);
                }
                WriteFile(TrainDataPath, trainBuilder.ToString());
                WriteFile(TestDataPath, testBuilder.ToString());

                var namesBuilder = new StringBuilder();
                foreach (var item in names)
                {
                    namesBuilder.AppendLine(item);
                }
                WriteFile(NamesPath, namesBuilder.ToString());

                var dataBuilder = new StringBuilder();
                dataBuilder.AppendLine($"classes = {ClassCount}");
                dataBuilder.AppendLine($"train   = {TrainDataPath}");
                dataBuilder.AppendLine($"valid   = {TestDataPath}");
                dataBuilder.AppendLine($"names   = {NamesPath}");
                dataBuilder.AppendLine($"backup  = {BackupPath}");
                WriteFile(DataDescPath, dataBuilder.ToString());

                prg.Close();
            });

            prg.ShowDialog();
        }

        void WriteFile(string path, string content)
        {
            var saveFile = new FileInfo(path);
            if (!saveFile.Exists)
                saveFile.Create().Dispose();
            File.WriteAllText(saveFile.FullName, content);
        }
    }
}
