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

namespace WordPuzzle
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        MainLogic backend = null;
        public MainWindow()
        {
            InitializeComponent();
            backend = MainLogic.GetInstance();
        }

        private void Backbone_Loaded(object sender, RoutedEventArgs e)
        {
            backend.LoadData(".\\THUOCL_poem.txt", ".\\LUT.txt");
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
            Console.WriteLine(b);
        }
    }
}
