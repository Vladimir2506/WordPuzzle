using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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
        private int[] stepLimits = { -1, 1000000, 2000000, 3000000, 4000000 };
        private BackgroundWorker worker = null;
        private bool IsCollating = false;
        private bool IsEditing = false;
        private int selectedCharIdx = -1;
        private int curStroke = -1;
        private int curVerse = -1;
        private int curWord = -1;
        private List<Point> ptsEdt = null;
        private DataTable table1 = null;
        private DataTable table2 = null;
        private TagStroke extraDesc = null;
        private int selectedStrokeIdx = -1;
        private List<int[]> StrokeTable = null;
        private List<string> ExNameTable = null;
        private DataTable table3 = null;
        private DataTable table4 = null;
        private ExtraConstraint extraConstraint = null;
        private bool IsExtraCollating = false;
        private bool IsDoingOther = false;

        public MainWindow()
        {
            InitializeComponent();
            backend = MainLogic.GetInstance();
            StrokeTable = new List<int[]>()
            {
                new int[]{0, 0},
                new int[]{1, 0},
                new int[]{2, 0},
                new int[]{2, 1},
                new int[]{2, 2},
                new int[]{3, 0},
                new int[]{3, 1},
                new int[]{3, 2},
                new int[]{3, 3}
            };
            ExNameTable = new List<string>()
            {
                "连接0：“人”字",
                "连接1：“工”字",
                "连接2：“智”字左上部分",
                "连接3：“智”字右上部分",
                "连接4：“智”字下部分",
                "连接5：“能”字左上部分",
                "连接6：“能”字左下部分",
                "连接7：“能”字右上部分",
                "连接8：“能”字右下部分"
            };
        }

        private void Backbone_Loaded(object sender, RoutedEventArgs e)
        {
            // Data Init.
            backend.LoadData(".\\THUOCL_poem.txt", ".\\LUT.txt");
            backend.LoadBank(".\\BANK.txt");
            problems = backend.problemBank;

            // UI Init.
            CreateScene();
            lbFunctions.SelectedIndex = 0;
        }

        private void SetContainerVisibility()
        {
            IsDoingOther = false;
            switch(lbFunctions.SelectedIndex)
            {
                case 0:
                    CardBottom0.Visibility = Visibility.Visible;
                    CardLeft0.Visibility = Visibility.Visible;
                    CardLeft1.Visibility = Visibility.Collapsed;
                    CardBottom1.Visibility = Visibility.Collapsed;
                    CardBottom2.Visibility = Visibility.Collapsed;
                    CardLeft2.Visibility = Visibility.Collapsed;
                    icContainer.Visibility = Visibility.Visible;
                    break;
                case 1:
                    CardBottom0.Visibility = Visibility.Visible;
                    CardLeft0.Visibility = Visibility.Collapsed;
                    CardLeft1.Visibility = Visibility.Visible;
                    CardBottom1.Visibility = Visibility.Visible;
                    CardBottom2.Visibility = Visibility.Collapsed;
                    CardLeft2.Visibility = Visibility.Collapsed;
                    icContainer.Visibility = Visibility.Collapsed;
                    break;
                case 2:
                    CardBottom0.Visibility = Visibility.Collapsed;
                    CardLeft0.Visibility = Visibility.Collapsed;
                    CardLeft1.Visibility = Visibility.Collapsed;
                    CardBottom1.Visibility = Visibility.Collapsed;
                    CardBottom2.Visibility = Visibility.Visible;
                    CardLeft2.Visibility = Visibility.Visible;
                    icContainer.Visibility = Visibility.Visible;
                    break;
                case 3:
                    CardBottom0.Visibility = Visibility.Visible;
                    CardLeft0.Visibility = Visibility.Visible;
                    CardLeft1.Visibility = Visibility.Collapsed;
                    CardBottom1.Visibility = Visibility.Collapsed;
                    CardBottom2.Visibility = Visibility.Collapsed;
                    CardLeft2.Visibility = Visibility.Collapsed;
                    icContainer.Visibility = Visibility.Visible;
                    IsDoingOther = true;
                    break;
            }
        }

        private void PrepareEditProblems()
        {
            // Do initialization work to perform edit.

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

        private void SetCollating()
        {
            lbiAdjustment.IsEnabled = false;
            lbiBenchmark.IsEnabled = false;
            lbiSolveOther.IsEnabled = false;
            lbiEditPuzzles.IsEnabled = false;
        }

        private void UnsetCollating()
        {
            lbiAdjustment.IsEnabled = true;
            lbiBenchmark.IsEnabled = true;
            lbiSolveOther.IsEnabled = true;
            lbiEditPuzzles.IsEnabled = true;
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
            btnDelSuperchar.IsEnabled = selectedCharIdx > 3;
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
            for(int tsc = 0; tsc < 4; ++tsc)
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
            lblInfo.Content = "";
            tblStepInfo.Text = "";
        }

        private void LbiBenchmark_Selected(object sender, RoutedEventArgs e)
        { 
            ResetScene();
            lblBenchmarkTitle.Content = "测试问题——人工智能";
            btnPrevSuperChar.Visibility = Visibility.Collapsed;
            btnNextSuperChar.Visibility = Visibility.Collapsed;
            lblInfo.Content = "";
            tblStepInfo.Text = "";
            SetBenchmarkScene();
        }

        private void Backbone_Closing(object sender, CancelEventArgs e)
        {
            backend.SaveBank(".\\BANK.txt");
        }

        private void LbiEditPuzzles_Selected(object sender, RoutedEventArgs e)
        {
            lblCurrentTips.Content = "";
            PrepareEditProblems();
        }

        private void TbCurrentName_LostFocus(object sender, RoutedEventArgs e)
        {
            problems[selectedCharIdx].Name = tbCurrentName.Text;
        }

        private void BtnAddSuperchar_Click(object sender, RoutedEventArgs e)
        {
            lblCurrentTips.Content = "";
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
            lblCurrentTips.Content = "";
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
            lblCurrentTips.Content = "";
            ResetScene();
            UpdateCurrentChar();
            UnsetEdit();
        }

        private void BtnPrevChar_Click(object sender, RoutedEventArgs e)
        {
            selectedCharIdx -= 1;
            lblCurrentTips.Content = "";
            ResetScene();
            UpdateCurrentChar();
            UnsetEdit();
        }

        private void LbFunctions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetContainerVisibility();
        }

        private void BtnBeginSolve_Click(object sender, RoutedEventArgs e)
        {
            if(!IsCollating)
            {
                IsCollating = true;
                tblStepInfo.Text = "";
                SolverParam param = new SolverParam()
                {
                    MaxStep = stepLimits[cbMaxSteps.SelectedIndex],
                    NeedShuffle = (bool)tgShuffle.IsChecked,
                    Reuse = (bool)tgReuse.IsChecked
                };
                worker = new BackgroundWorker();
                worker.DoWork += SolveCollate;
                worker.RunWorkerCompleted += WorkDone;
                worker.RunWorkerAsync(param);
                SetCollating();
                lblInfo.Content = "搜索中…";
            }
        }

        private void SolveCollate(object sender, DoWorkEventArgs e)
        {
            SolverParam param = e.Argument as SolverParam;
            backend.ClearUsedIndices();
            if (IsDoingOther)
            {
                SuperChar sc = problems[selectedCharIdx];
                if (param.Reuse) backend.ClearUsedIndices();
                for (int nd = 0; nd < sc.descriptors.Count; ++nd)
                {
                    TagStroke desc = sc.descriptors[nd];
                    backend.ClearPuzzleData();
                    backend.MakePuzzleData(desc);
                    uint step = backend.Solve(MainLogic.Solver.BasicSearch, param);
                    sc.strokes.Add(backend.GetResult());
                    if (backend.SearchSuccess())
                    {
                        ShowResultOnScene(selectedCharIdx, nd);
                    }
                    Dispatcher.Invoke(new Action<int, int, uint>(PrintSteps), new object[] { selectedCharIdx, nd, step });
                }
            }
            else
            {
                for (int tsc = 0; tsc < 4; ++tsc)
                {
                    SuperChar sc = problems[tsc];
                    sc.strokes = new List<Stroke>();
                    for (int nd = 0; nd < sc.descriptors.Count; ++nd)
                    {
                        if (param.Reuse) backend.ClearUsedIndices();
                        TagStroke desc = sc.descriptors[nd];
                        backend.ClearPuzzleData();
                        backend.MakePuzzleData(desc);
                        uint step = backend.Solve(MainLogic.Solver.BasicSearch, param);
                        sc.strokes.Add(backend.GetResult());
                        if (backend.SearchSuccess())
                        {
                            ShowResultOnScene(tsc, nd);
                        }
                        Dispatcher.Invoke(new Action<int, int, uint>(PrintSteps), new object[] { tsc, nd, step });
                    }
                }
            }
            
        }

        private void WorkDone(object sender, RunWorkerCompletedEventArgs e)
        {
            IsCollating = false;
            UnsetCollating();
            lblInfo.Content = "搜索完成。";
        }

        private void PrintSteps(int tsc, int nd, uint step)
        {
            string information = string.Format("字{0}连接{1}:{2}步  ", tsc, nd, step);
            tblStepInfo.Text += information;
        }

        private void ShowResultOnScene(int tsc, int nd)
        {
            Stroke onScene = problems[tsc].strokes[nd];
            List<Point[]> pts = problems[tsc].descriptors[nd].positions;
            for(int npt = 0; npt < pts.Count; ++npt)
            {
                Point[] versePos = pts[npt];
                for(int vp = 0; vp < versePos.Length; ++vp)
                {
                    int scIdx = (int)versePos[vp].Y * sceneCols + (int)versePos[vp].X;
                    scene[scIdx].SelfContent = backend.TranslateOnce(onScene.Verses[npt].Content[vp]);
                }
            }

        }

        private void BtnClearBenchmark_Click(object sender, RoutedEventArgs e)
        {
            if(!IsCollating)
            {
                ResetScene();
                if(lbFunctions.SelectedIndex == 0)
                {
                    SetBenchmarkScene();
                }
                if(lbFunctions.SelectedIndex == 3)
                {
                    ShowOtherSuperChar();
                }
                lblInfo.Content = "";
                tblStepInfo.Text = "";
            }
        }

        private void LbiAdjustment_Selected(object sender, RoutedEventArgs e)
        {
            selectedStrokeIdx = 0;
            SetExtraButtonsEnabled();
            ClearExtraResults();
            PrepareExtraProblems();
        }

        private void SetExtraButtonsEnabled()
        {
            btnExtraLeft.IsEnabled = selectedStrokeIdx > 0;
            btnExtraRight.IsEnabled = selectedStrokeIdx < 8;
        }

        private void ClearExtraResults()
        {
            tblResultA.Text = "";
            tblResultV.Text = "";
            lblExtraProg.Content = "";
            tblStepInfo.Text = "";
        }

        private void PrepareExtraProblems()
        {
            TagStroke desc = problems[StrokeTable[selectedStrokeIdx][0]].descriptors[StrokeTable[selectedStrokeIdx][1]];
            table3 = new DataTable();
            table4 = new DataTable();
            SetExtraTable(table3, table4, desc);
            dgExtraA.ItemsSource = table3.DefaultView;
            dgExtraV.ItemsSource = table4.DefaultView;
            lblCurrentExtra.Content = ExNameTable[selectedStrokeIdx];
        }

        private void SetExtraTable(DataTable tb3, DataTable tb4, TagStroke stk)
        {
            tb3.Columns.Add("VSIdxEx", typeof(int));
            tb3.Columns.Add("CSIdxEx", typeof(int));
            tb3.Columns.Add("VEIdxEx", typeof(int));
            tb3.Columns.Add("CEIdxEx", typeof(int));
            tb3.Columns.Add("AnchorCharEx", typeof(string));
            foreach(int[] ancs in stk.Anchors)
            {
                DataRow row = tb3.NewRow();
                row["VSIdxEx"] = ancs[0];
                row["CSIdxEx"] = ancs[1];
                row["VEIdxEx"] = ancs[2];
                row["CEIdxEx"] = ancs[3];
                row["AnchorCharEx"] = "";
                tb3.Rows.Add(row);
            }
            tb4.Columns.Add("VerseIdxEx", typeof(int));
            tb4.Columns.Add("VerseLenEx", typeof(int));
            tb4.Columns.Add("VerseTextEx", typeof(string));
            int c = 0;
            foreach (int len in stk.LenVerses)
            {
                DataRow row = tb4.NewRow();
                row["VerseIdxEx"] = c;
                row["VerseLenEx"] = len;
                row["VerseTextEx"] = "";
                tb4.Rows.Add(row);
                ++c;
            }
        }

        private ExtraConstraint GetExtraTable(DataTable tb3, DataTable tb4, TagStroke tag)
        {
            List<string> extraAnchorInString = new List<string>(), extraVerseInString = new List<string>();
            tag.nbAnchors = tb3.Rows.Count;
            foreach(DataRow row in tb3.Rows)
            {
                int a0 = (int)row["VSIdxEx"], a1 = (int)row["CSIdxEx"], a2 = (int)row["VEIdxEx"], a3 = (int)row["CEIdxEx"];
                tag.Anchors.Add(new int[] { a0, a1, a2, a3 });
                extraAnchorInString.Add((string)row["AnchorCharEx"]);
            }
            tag.nbVerses = tb4.Rows.Count;
            foreach(DataRow row in tb4.Rows)
            {
                int len = (int)row["VerseLenEx"];
                string s = (string)row["VerseTextEx"];
                tag.LenVerses.Add(len);
                extraVerseInString.Add(s);
            }
            ExtraConstraint exc = new ExtraConstraint(extraVerseInString, extraAnchorInString);
            return exc;
        }

        private void BtnExtraLeft_Click(object sender, RoutedEventArgs e)
        {
            selectedStrokeIdx -= 1;
            SetExtraButtonsEnabled();
            ClearExtraResults();
            PrepareExtraProblems();
        }

        private void BtnExtraRight_Click(object sender, RoutedEventArgs e)
        {
            selectedStrokeIdx += 1;
            SetExtraButtonsEnabled();
            ClearExtraResults();
            PrepareExtraProblems();
        }

        private bool CollectEx()
        {
            extraDesc = new TagStroke();
            extraConstraint = GetExtraTable(table3, table4, extraDesc);
            int[] lens = extraDesc.LenVerses.ToArray();
            for(int k = 0; k < extraDesc.Anchors.Count; ++k)
            {
                int[] a = extraDesc.Anchors[k];
                int ivs = a[0], ics = a[1], ive = a[2], ice = a[3];
                if(ics < 0 || ics >= lens[ivs] || ice < 0 || ice >= lens[ive])
                {
                    return false;
                }
                if (ivs >= ive)
                {
                    return false;
                }
            }
            for(int k = 0; k < extraConstraint.extraVerses.Count; ++k)
            {
                if(extraConstraint.extraVerses[k].Length != lens[k] && extraConstraint.extraVerses[k].Length > 0)
                {
                    return false;
                }
            }
            return backend.ConvertChrShrEx(extraConstraint);
        }

        private void BtnExtraClear_Click(object sender, RoutedEventArgs e)
        {
            backend.ClearUsedIndices();
            ClearExtraResults();
        }

        private void BtnExtraSearch_Click(object sender, RoutedEventArgs e)
        {
            ClearExtraResults();
            bool CanSearch = CollectEx();
            if(CanSearch)
            {
                if (!IsExtraCollating)
                {
                    IsExtraCollating = true;
                    tblStepInfo.Text = "";
                    SolverParam param = new SolverParam()
                    {
                        MaxStep = stepLimits[cbMaxStepEx.SelectedIndex],
                        NeedShuffle = (bool)tgRandomInitEx.IsChecked,
                        Reuse = false
                    };
                    worker = new BackgroundWorker();
                    worker.DoWork += SearchExCollate;
                    worker.RunWorkerCompleted += SearchExDone;
                    worker.RunWorkerAsync(param);
                    SetCollating();
                    lblExtraProg.Content = "搜索中…";
                }
            }
            else
            {
                lblExtraProg.Content = "约束非法。";
            }
        }

        private void SearchExCollate(object sender, DoWorkEventArgs e)
        {
            SolverParam param = e.Argument as SolverParam;
            if(param.NeedShuffle) backend.ClearUsedIndices();
            backend.ClearPuzzleData();
            backend.MakePuzzleDataEx(extraDesc, extraConstraint);
            uint step = backend.Solve(MainLogic.Solver.ExtraSearch, param);
            Dispatcher.Invoke(
                new Action<int, int, uint>(PrintSteps), 
                new object[] { StrokeTable[selectedStrokeIdx][0], StrokeTable[selectedStrokeIdx][1], step }
                );
            Stroke stroke = backend.GetResult();
            bool success = backend.SearchSuccess();
            Dispatcher.Invoke(new Action<bool, Stroke>(ShowExtraResult), new object[] { success, stroke });
        }

        private void SearchExDone(object sender, RunWorkerCompletedEventArgs e)
        {
            IsExtraCollating = false;
            UnsetCollating();
            lblExtraProg.Content = "搜索完成。";
        }

        private void ShowExtraResult(bool isSuccess, Stroke stroke)
        {
            tblResultV.Inlines.Clear();
            tblResultA.Inlines.Clear();
            if (!isSuccess)
            {
                lblExtraProg.Content = "无解。";
                return;
            }
            string[] verses = backend.Translate(stroke);
            for(int k = 0; k < stroke.Verses.Count; ++k)
            {
                tblResultV.Inlines.Add(new Run(string.Format("句{0}:{1}", k, verses[k])));
                tblResultV.Inlines.Add(new LineBreak());
            }
            for(int k = 0; k < stroke.Anchors.Count; ++k)
            {
                int a0 = stroke.Anchors[k][0], a1 = stroke.Anchors[k][1];
                string ax = verses[a0][a1].ToString();
                tblResultA.Inlines.Add(new Run(string.Format("交叉点{0}:{1}", k, ax)));
                tblResultA.Inlines.Add(new LineBreak());
            }
        }

        private void LbiSolveOther_Selected(object sender, RoutedEventArgs e)
        {
            selectedCharIdx = 0;
            lblBenchmarkTitle.Content = "其他谜题——" + problems[selectedCharIdx].Name;
            btnPrevSuperChar.Visibility = Visibility.Visible;
            btnNextSuperChar.Visibility = Visibility.Visible;
            SetPrevNextSuperCharEnable();
            ShowOtherSuperChar();
        }

        private void SetPrevNextSuperCharEnable()
        {
            btnPrevSuperChar.IsEnabled = selectedCharIdx > 0;
            btnNextSuperChar.IsEnabled = selectedCharIdx < problems.Count - 1;
        }

        private void BtnPrevSuperChar_Click(object sender, RoutedEventArgs e)
        {
            selectedCharIdx -= 1;
            SetPrevNextSuperCharEnable();
            ShowOtherSuperChar();
            lblBenchmarkTitle.Content = "其他谜题——" + problems[selectedCharIdx].Name;
        }

        private void BtnNextSuperChar_Click(object sender, RoutedEventArgs e)
        {
            selectedCharIdx += 1;
            SetPrevNextSuperCharEnable();
            ShowOtherSuperChar();
            lblBenchmarkTitle.Content = "其他谜题——" + problems[selectedCharIdx].Name;
        }

        private void ShowOtherSuperChar()
        {
            ResetScene();
            SuperChar selected = problems[selectedCharIdx];
            for (int t = 0; t < selected.descriptors.Count; ++t)
            {
                TagStroke descriptor = selected.descriptors[t];
                for (int i = 0; i < descriptor.positions.Count; ++i)
                {
                    for (int j = 0; j < descriptor.positions[i].Length; ++j)
                    {
                        Point pt = descriptor.positions[i][j];
                        int sceneIdx = (int)pt.Y * sceneCols + (int)pt.X;
                        scene[sceneIdx].SelfType = GridPoint.GridPointType.Assigned;
                        scene[sceneIdx].AddVersePos(selectedCharIdx, t, i, j);
                    }
                }
            }
            lblInfo.Content = "";
            tblStepInfo.Text = "";
        }

        

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            dlgAbout.IsOpen = false;
        }

        private void LbiAbout_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dlgAbout.IsOpen = true;
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
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
