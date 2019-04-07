using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;

namespace WordPuzzle
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainLogic backend = null;
        private DispatcherTimer delayExit = null;
        private ObservableCollection<GridPoint> scene = null;

        public MainWindow()
        {
            InitializeComponent();
            backend = MainLogic.GetInstance();
        }

        private void Backbone_Loaded(object sender, RoutedEventArgs e)
        {
            /*backend.LoadData(".\\THUOCL_poem.txt", ".\\LUT.txt");
            TagStroke tag = new TagStroke()
            {
                nbVerses = 3,
                nbAnchors = 2,
                lenVerses = new int[]{ 5, 5, 7 },
                anchors = new int[,] { {0,2,1,0}, { 1,4,2,3}}
            };
            List<TagStroke> tags = new List<TagStroke>(new TagStroke[] { tag });
            backend.MakePuzzleData(tags);
            bool b = backend.Solve(MainLogic.Solver.NaiveSearch);
            Stroke[] result = backend.GetResult();
            string[] resultInChar = backend.Translate(result[0]);
            Console.WriteLine(b);*/

            lbFunctions.SelectedIndex = 0;
        }

        private void LbiQuit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            delayExit = new DispatcherTimer();
            delayExit.Tick += new EventHandler(DelayShutdown);
            delayExit.Interval = TimeSpan.FromMilliseconds(250);
            delayExit.Start();
        }

        private void DelayShutdown(object sender, EventArgs e)
        {
            delayExit.Stop();
            Close();
        }

        private void CzTitle_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetScene();
        }

        private void ResetScene()
        {
            scene = new ObservableCollection<GridPoint>();
            for(int i = 0; i < 18 * 35; ++i)
            {
                GridPoint gpt = new GridPoint("", i % 35, i / 35);
                scene.Add(gpt);
            }
            icContainer.ItemsSource = scene;
        }

        private void Benchmark_Selected(object sender, RoutedEventArgs e)
        {
            ResetScene();
        }
    }

    public class GridPoint
    {
        public enum GridPointType
        {
            Unset = 0,
            Assigned = 1
        }

        public string SelfContent { get; set; }
        public GridPointType SelfType { get; set; }
        public Point SelfPos { get; }

        public GridPoint(string s, int x, int y, GridPointType gpt = GridPointType.Assigned)
        {
            SelfContent = s;
            SelfPos = new Point(x, y);
            SelfType = gpt;
        }
    }

    public class GridPointConverter : IValueConverter
    {
        private MaterialDesignColors.Hue hueAcc;

        public GridPointConverter()
        {
            var palette = new PaletteHelper().QueryPalette();
            hueAcc = palette.AccentSwatch.AccentHues.ToArray()[palette.AccentHueIndex];
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            GridPoint gp = value as GridPoint;
            SolidColorBrush brushResult = new SolidColorBrush();
            
            switch(gp.SelfType)
            {
                case GridPoint.GridPointType.Unset:
                    brushResult.Color = Colors.White;
                    break;
                case GridPoint.GridPointType.Assigned:
                    brushResult.Color = hueAcc.Color;
                    break;
            }
            return brushResult;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
