using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.ComponentModel;
using System.Data;
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
        private const int sceneRows = 18;
        private const int sceneCols = 34;
        private List<SuperChar> problems = null;
 
        private bool IsEditing = false;
        private int selectedCharIdx = -1;
        private int curStroke = -1;
        private int curVerse = -1;
        private int curWord = -1;
        private List<Point> ptsEdt = null;
        private DataTable table1 = null;
        private DataTable table2 = null;

        public MainWindow()
        {
            InitializeComponent();
            backend = MainLogic.GetInstance();
            // Data Init.
            backend.LoadData(".\\THUOCL_poem.txt", ".\\LUT.txt");
            backend.LoadBank(".\\BANK.txt");
            problems = backend.problemBank;
        }

        private void Backbone_Loaded(object sender, RoutedEventArgs e)
        {
            /*
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

            // UI Init.
            CreateScene();
            lbFunctions.SelectedIndex = 1;
        }

        private void PrepareEditProblems()
        {
            // Do initialization work to perform edit.

            // Setting UI Visibility.
            CardBottom2.Visibility = Visibility.Visible;
            CardLeft2.Visibility = Visibility.Visible;

            // Clear scene and clear binded points.
            selectedCharIdx = 0;
            ResetScene();
            UnsetEdit();

            // Set problems.
            LoadOneSuperChar();
            UpdateCurrentChar();
        }

        private void LoadOneSuperChar()
        {
            selectedCharIdx = 0;
            table1 = new DataTable();
            table2 = new DataTable();
            PrepareTables(table1, table2);
            dgStrokes.ItemsSource = table1.DefaultView;
            dgAnchor.ItemsSource = table2.DefaultView;
        }

        private void UpdateCurrentChar()
        {
            SuperChar selected = problems[selectedCharIdx];
            tbCurrentName.Text = selected.Name;
            for (int t = 0; t < selected.descriptors.Count; ++t)
            {
                TagStroke descriptor = selected.descriptors[t];
                for(int i = 0; i < descriptor.positions.Count; ++i)
                {
                    for(int j = 0; j < descriptor.positions[i].Length; ++j)
                    {
                        Point pt = descriptor.positions[i][j];
                        int sceneIdx = (int)pt.Y * sceneCols + (int)pt.X;
                        scene[sceneIdx].SelfType = GridPoint.GridPointType.Assigned;
                        scene[sceneIdx].AddVersePos(selectedCharIdx, t, i, j);
                    }
                }
            }
            table1 = new DataTable();
            table2 = new DataTable();
            PrepareTables(table1, table2);
            dgStrokes.ItemsSource = table1.DefaultView;
            dgAnchor.ItemsSource = table2.DefaultView;
        }

        private void SetEdit()
        {
            // Disable
            btnPrevChar.IsEnabled = false;
            btnNextChar.IsEnabled = false;
            pbEdit.IsEnabled = false;
            lbiAdjustment.IsEnabled = false;
            lbiBenchmark.IsEnabled = false;
            lbiSolveOther.IsEnabled = false;
            lbiEditPuzzles.IsEnabled = false;

            // Enable
            btnNextVerse.IsEnabled = true;
            btnNextStroke.IsEnabled = true;
            btnCommit.IsEnabled = true;

            // Flag
            IsEditing = true;
        }

        private void UnsetEdit()
        {
            // Disable
            btnNextVerse.IsEnabled = false;
            btnNextStroke.IsEnabled = false;
            btnCommit.IsEnabled = false;

            // Enable
            btnPrevChar.IsEnabled = selectedCharIdx > 0;
            btnNextChar.IsEnabled = selectedCharIdx < problems.Count - 1;
            btnDelSuperchar.IsEnabled = selectedCharIdx > 0;
            pbEdit.IsEnabled = true;
            lbiAdjustment.IsEnabled = true;
            lbiBenchmark.IsEnabled = true;
            lbiSolveOther.IsEnabled = true;
            lbiEditPuzzles.IsEnabled = true;

            // Flag
            IsEditing = false;
        }

        private void PrepareTables(DataTable tb1, DataTable tb2)
        {
            tb1.Columns.Add("StrokeIdx", typeof(int));
            tb1.Columns.Add("VerseIdx", typeof(int));
            tb1.Columns.Add("VerseLen", typeof(int));
            tb2.Columns.Add("StrokeIdx", typeof(int));
            tb2.Columns.Add("VSIdx", typeof(int));
            tb2.Columns.Add("CSIdx", typeof(int));
            tb2.Columns.Add("VEIdx", typeof(int));
            tb2.Columns.Add("CEIdx", typeof(int));
            for (int si = 0; si < problems[selectedCharIdx].descriptors.Count; ++si)
            {
                for (int vi = 0; vi < problems[selectedCharIdx].descriptors[si].nbVerses; ++vi)
                {
                    DataRow row = tb1.NewRow();
                    row["StrokeIdx"] = si;
                    row["VerseIdx"] = vi;
                    row["VerseLen"] = problems[selectedCharIdx].descriptors[si].LenVerses[vi];
                    tb1.Rows.Add(row);
                }
                for (int ai = 0; ai < problems[selectedCharIdx].descriptors[si].nbAnchors; ++ai)
                {
                    DataRow row = tb2.NewRow();
                    row["StrokeIdx"] = si;
                    row["VSIdx"] = problems[selectedCharIdx].descriptors[si].Anchors[ai][0];
                    row["CSIdx"] = problems[selectedCharIdx].descriptors[si].Anchors[ai][1];
                    row["VEIdx"] = problems[selectedCharIdx].descriptors[si].Anchors[ai][2];
                    row["CEIdx"] = problems[selectedCharIdx].descriptors[si].Anchors[ai][3];
                    tb2.Rows.Add(row);
                }
            }
        }

        private void LbiQuit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Set a timer to make visual effects.
            delayExit = new DispatcherTimer();
            delayExit.Tick += new EventHandler(DelayShutdown);
            delayExit.Interval = TimeSpan.FromMilliseconds(250);
            delayExit.Start();
        }

        private void DelayShutdown(object sender, EventArgs e)
        {
            // Call Close().
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

        private void CreateScene()
        {
            scene = new ObservableCollection<GridPoint>();
            icContainer.ItemsSource = scene;
            for (int i = 0; i < sceneCols * sceneRows; ++i)
            {
                GridPoint gpt = new GridPoint((i % sceneCols).ToString(), i % sceneCols, i / sceneCols);
                scene.Add(gpt);
            }   
        }

        private void ResetScene()
        {
            for(int i = 0; i < sceneRows * sceneCols; ++i)
            {
                scene[i].SelfContent = "";
                scene[i].SelfType = GridPoint.GridPointType.Unset;
                scene[i].ClearVersePos();
            }
        }

        private void SetBenchmarkScene()
        {
            for(int tsc = 0; tsc < 2; ++tsc)
            {
                SuperChar sc = problems[tsc];
                for (int t = 0; t < sc.descriptors.Count; ++t)
                {
                    TagStroke descriptor = sc.descriptors[t];
                    for (int k = 0; k < descriptor.positions.Count; ++k)
                    {
                        Point[] applicants = descriptor.positions[k];
                        for (int l = 0; l < applicants.Length; ++l)
                        {
                            Point p = applicants[l];
                            int idx = (int)p.Y * sceneCols + (int)p.X;
                            scene[idx].SelfType = GridPoint.GridPointType.Assigned;
                            scene[idx].AddVersePos(tsc, t, k, l);
                        }
                    }
                }
            }
        }

        private void LbiBenchmark_Selected(object sender, RoutedEventArgs e)
        {
            ResetScene();
            SetBenchmarkScene();
        }

        private void Backbone_Closing(object sender, CancelEventArgs e)
        {
            backend.SaveBank(".\\BANK.txt");
        }

        private void LbiEditPuzzles_Selected(object sender, RoutedEventArgs e)
        {
            PrepareEditProblems();
        }

        private void TbCurrentName_LostFocus(object sender, RoutedEventArgs e)
        {
            problems[selectedCharIdx].Name = tbCurrentName.Text;
        }

        private void BtnAddSuperchar_Click(object sender, RoutedEventArgs e)
        {
            problems.Add(new SuperChar());
            selectedCharIdx = problems.Count - 1;
            problems[selectedCharIdx].descriptors.Add(new TagStroke());
            table1 = new DataTable();
            table2 = new DataTable();
            PrepareTables(table1, table2);
            dgStrokes.ItemsSource = table1.DefaultView;
            dgAnchor.ItemsSource = table2.DefaultView;
            tbCurrentName.Text = problems[selectedCharIdx].Name;
            ResetScene();
            UpdateCurrentChar();
            SetEdit();
            curStroke = 0;
            curVerse = 0;
            curWord = 0;
            ptsEdt = new List<Point>();
        }

        private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsEditing) return;
            ContentPresenter cp = VisualTreeHelper.GetParent((Card)sender) as ContentPresenter;
            GridPoint gp = cp.Content as GridPoint;
            int sceneIdx = (int)gp.SelfPos.Y * sceneCols + (int)gp.SelfPos.X;
            Point thePoint = gp.SelfPos;
            if(!ptsEdt.Contains(thePoint))
            {
                ptsEdt.Add(thePoint);
                scene[sceneIdx].AddVersePos(selectedCharIdx, curStroke, curVerse, curWord);
                ++curWord;
                scene[sceneIdx].SelfType = GridPoint.GridPointType.Assigned;

                bool foundCross = false;
                for(int k = 0; k < curVerse; ++k)
                {
                    List<Point[]> ptsPrev = problems[selectedCharIdx].descriptors[curStroke].positions;
                    for(int i = 0; i < ptsPrev.Count; ++i)
                    {
                        for(int j = 0; j < ptsPrev[i].Length; ++j)
                        {
                            if((int)ptsPrev[i][j].X == (int)thePoint.X && (int)ptsPrev[i][j].Y == (int)thePoint.Y)
                            {
                                DataRow rowA = table2.NewRow();
                                rowA["StrokeIdx"] = curStroke;
                                rowA["VSIdx"] = i;
                                rowA["CSIdx"] = j;
                                rowA["VEIdx"] = curVerse;
                                rowA["CEIdx"] = curWord - 1;
                                table2.Rows.Add(rowA);
                                foundCross = true;
                            }
                            if (foundCross) break;
                        }
                        if (foundCross) break;
                    }
                    if (foundCross) break;
                }
            }
            else
            {
                lblCurrentTips.Content = "选点重复。";
            }
            
        }

        private void BtnCommit_Click(object sender, RoutedEventArgs e)
        {
            DataRow rowV = table1.NewRow();
            rowV["StrokeIdx"] = curStroke;
            rowV["VerseIdx"] = curVerse;
            rowV["VerseLen"] = ptsEdt.Count;
            table1.Rows.Add(rowV);
            problems[selectedCharIdx].descriptors[curStroke].positions.Add(ptsEdt.ToArray());
            curVerse = 0;
            curWord = 0;
            curStroke = 0;
            ptsEdt = null;
            CommitTables();
            ResetScene();
            UpdateCurrentChar();
            UnsetEdit();
        }

        private void CommitTables()
        {
            foreach(DataRow row in table1.Rows)
            {
                int stkIdx = (int)row["StrokeIdx"], lenV = (int)row["VerseLen"];
                if(lenV == 0)
                {
                    lblCurrentTips.Content = "题目描述非法。";
                    problems.RemoveAt(selectedCharIdx);
                    selectedCharIdx -= 1;
                    return;
                }
                problems[selectedCharIdx].descriptors[stkIdx].LenVerses.Add(lenV);
                problems[selectedCharIdx].descriptors[stkIdx].nbVerses += 1;
            }
            foreach(DataRow row in table2.Rows)
            {
                int stkIdx = (int)row["StrokeIdx"];
                int vs = (int)row["VSIdx"], cs = (int)row["CSIdx"], ve = (int)row["VEIdx"], ce = (int)row["CEIdx"];
                problems[selectedCharIdx].descriptors[stkIdx].Anchors.Add(new int[] { vs, cs, ve, ce });
                problems[selectedCharIdx].descriptors[stkIdx].nbAnchors += 1;
            }
        }

        private void BtnNextStroke_Click(object sender, RoutedEventArgs e)
        {
            DataRow rowV = table1.NewRow();
            rowV["StrokeIdx"] = curStroke;
            rowV["VerseIdx"] = curVerse;
            rowV["VerseLen"] = ptsEdt.Count;
            table1.Rows.Add(rowV);
            problems[selectedCharIdx].descriptors[curStroke].positions.Add(ptsEdt.ToArray());
            curVerse = 0;
            curWord = 0;
            ++curStroke;
            ptsEdt = new List<Point>();
            problems[selectedCharIdx].descriptors.Add(new TagStroke());
        }

        private void BtnNextVerse_Click(object sender, RoutedEventArgs e)
        {
            DataRow rowV = table1.NewRow();
            rowV["StrokeIdx"] = curStroke;
            rowV["VerseIdx"] = curVerse;
            rowV["VerseLen"] = ptsEdt.Count;
            table1.Rows.Add(rowV);
            problems[selectedCharIdx].descriptors[curStroke].positions.Add(ptsEdt.ToArray());
            ++curVerse;
            curWord = 0;
            ptsEdt = new List<Point>();
        }

        private void BtnDelSuperchar_Click(object sender, RoutedEventArgs e)
        {
            int delCharIdx = selectedCharIdx;
            selectedCharIdx -= 1;
            ResetScene();
            UpdateCurrentChar();
            problems.RemoveAt(delCharIdx);
            UnsetEdit();
        }

        private void BtnNextChar_Click(object sender, RoutedEventArgs e)
        {
            selectedCharIdx += 1;
            ResetScene();
            UpdateCurrentChar();
            UnsetEdit();
        }

        private void BtnPrevChar_Click(object sender, RoutedEventArgs e)
        {
            selectedCharIdx -= 1;
            ResetScene();
            UpdateCurrentChar();
            UnsetEdit();
        }
    }

    public class GridPoint : INotifyPropertyChanged
    {
        public enum GridPointType
        {
            Unset = 0,
            Assigned = 1
        }
        private string _content;
        public string SelfContent
        {
            get { return _content; }
            set
            {
                if (_content == value) return;
                _content = value;
                OnPropertyChanged("SelfContent");
            }
        }
        private GridPointType _type;
        public GridPointType SelfType
        {
            get { return _type; }
            set
            {
                if (_type == value) return;
                _type = value;
                OnPropertyChanged("SelfType");
            }
        }
        public Point SelfPos { get; }
        public List<int[]> versePos = null;
        public GridPoint(string s, int x, int y, GridPointType gpt = GridPointType.Unset)
        {
            SelfContent = s;
            SelfPos = new Point(x, y);
            SelfType = gpt;
            versePos = new List<int[]>();
        }
        public void AddVersePos(int scIdx, int strokeIdx, int verseIdx, int wordIdx)
        {
            versePos.Add(new int[] { scIdx, strokeIdx, verseIdx, wordIdx });
        }
        public void ClearVersePos()
        {
            versePos.Clear();
        }
        #region implement property changed 
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
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
            GridPoint.GridPointType tp = (GridPoint.GridPointType)value;
            SolidColorBrush brushResult = new SolidColorBrush();
            switch(tp)
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
