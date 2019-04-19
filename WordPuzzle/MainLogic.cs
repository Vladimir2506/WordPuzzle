using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace WordPuzzle
{
    public class SuperChar
    {
        public string Name { get; set; }
        public List<Stroke> strokes;
        public ObservableCollection<TagStroke> descriptors;
    }
    public class MainLogic
    {
        public enum Solver
        {
            NaiveSearch = 0
        }
        // Singleton.
        private static MainLogic theInstance = null;
        // Data members.
        private Dictionary<char, ushort> dictChr2Shr = null;
        private Dictionary<ushort, char> dictShr2Chr = null;
        private Dictionary<int, List<string>> dictWordData = null;
        private Dictionary<int, ushort[][]> dictDataMatrices = null;
        private Dictionary<int, List<int>> usedIndices = null;
        public List<SuperChar> problemBank = null;
        private List<Stroke> puzzleData = null;

        private MainLogic()
        {
            dictChr2Shr = new Dictionary<char, ushort>();
            dictShr2Chr = new Dictionary<ushort, char>();
            dictWordData = new Dictionary<int, List<string>>();
            dictDataMatrices = new Dictionary<int, ushort[][]>();
            usedIndices = new Dictionary<int, List<int>>();
            puzzleData = new List<Stroke>();
            problemBank = new List<SuperChar>();
        }

        private SuperChar ConstructRen()
        {
            SuperChar s = new SuperChar()
            {
                Name = "人",
                descriptors = new ObservableCollection<TagStroke>() {
                    new TagStroke()
                    {
                        nbVerses = 2,
                        nbAnchors = 1,
                        lenVerses = new int[] { 7, 5 },
                        anchors = new int[,] { { 0, 2, 1, 0 } },
                        positions = new List<Point[]>()
                        {
                            new Point[7] { new Point(4,0), new Point(4,1),new Point(4,2),new Point(3,3),new Point(2,4),new Point(5,1),new Point(6,0) },
                            new Point[5] { new Point(4, 2), new Point(5, 3), new Point(6, 4), new Point(7, 5), new Point(8, 6) }
                        }
                    }
                }
            };
            return s;
        }

        public static MainLogic GetInstance()
        {
            if (theInstance == null)
            {
                theInstance = new MainLogic();
            }
            return theInstance;
        }

        public void LoadData(string fnWordBase, string fnLUT)
        {
            Util.LoadWordBase(fnWordBase, fnLUT, dictChr2Shr, dictShr2Chr, dictWordData);
            Util.Chr2Shr(dictWordData, dictDataMatrices, dictChr2Shr);

            foreach (int key in dictDataMatrices.Keys)
            {
                usedIndices.Add(key, new List<int>());
            }
        }

        public void SaveBank(string fnBank)
        {
            Util.SaveProblemBank(problemBank, fnBank);
        }

        public void LoadBank(string fnBank)
        {
            Util.LoadProblemBank(problemBank, fnBank);
        }

        public void MakePuzzleData(List<TagStroke> tags)
        {
            foreach(TagStroke tag in tags)
            {
                if(tag.anchors.GetLength(1) != 4)
                {
                    throw new ArgumentException("Invalid anchor description.");
                }
                Stroke stroke = new Stroke(tag.nbVerses, tag.nbAnchors);
                for(int i = 0; i < tag.nbVerses; ++i)
                {
                    stroke.Verses[i] = new Verse(tag.lenVerses[i]);
                }
                for(int i = 0; i < tag.nbAnchors; ++i)
                {
                    stroke.Anchors[i] = new int[4];
                }
                for(int i = 0; i < tag.nbAnchors; ++i)
                {
                    stroke.Anchors[i][0] = tag.anchors[i, 0];
                    stroke.Anchors[i][1] = tag.anchors[i, 1];
                    stroke.Anchors[i][2] = tag.anchors[i, 2];
                    stroke.Anchors[i][3] = tag.anchors[i, 3];
                }
                puzzleData.Add(stroke);
            }
        }

        public void ClearPuzzleData()
        {
            puzzleData.Clear();
        }

        public bool Solve(Solver solver)
        {
            for(int i = 0; i < puzzleData.Count; ++i)
            {
                if (puzzleData[i].IsCompleted) continue;
                bool result = false;
                switch(solver)
                {
                    case Solver.NaiveSearch:
                        result = DoNaiveSearch(puzzleData[i]);
                        break;
                    default:
                        break;
                }
                if (!result) return false;
            }
            return true;
        }

        private bool DoNaiveSearch(Stroke stroke)
        {
            int nbVerses = stroke.Verses.Count;
            Stack<int> proposal = new Stack<int>(), 
                candidate = new Stack<int>(Enumerable.Reverse(Enumerable.Range(0, nbVerses)));
            List<Verse> result = new List<Verse>();
            Dictionary<int, Stack<int>> localUsed = new Dictionary<int, Stack<int>>();
            foreach(int len in usedIndices.Keys)
            {
                localUsed.Add(len, new Stack<int>());
            }
            while(candidate.Count > 0)
            {
                int curIdx = candidate.Pop();
                // Existing verses construct the constraints.
                List<int[]> constraints = stroke.Anchors.FindAll(
                    a => a[2] == curIdx && proposal.Contains(a[0])
                    );
                int requiredLength = stroke.Verses[curIdx].Length;
                Verse v = new Verse();
                bool stepDone = false;
                // Look up in the data.
                for (int s = 0; s < dictDataMatrices[requiredLength].Length; ++s)
                {
                    // Prevent replicate verses.
                    if (usedIndices[requiredLength].Contains(s) 
                        || localUsed[requiredLength].Contains(s)) continue;
                    stepDone = true;
                    ushort[] content = dictDataMatrices[requiredLength][s];
                    foreach(int[] a in constraints)
                    {
                        // For each search, check the constraints.
                        ushort trait = content[a[3]], 
                            check = result[a[0]].Content[a[1]];
                        if(trait != check)
                        {
                            stepDone = false;
                            break;
                        }
                    }
                    if (stepDone)
                    {
                        // Success, copy searched to result.
                        v.Content = content;
                        v.Length = requiredLength;
                        localUsed[requiredLength].Push(s);
                        break;
                    }
                }
                if(stepDone)
                {
                    // Success, to next verse.
                    proposal.Push(curIdx);
                    result.Add(v);
                }
                else
                {
                    // One Step Failure, trace back.
                    if (proposal.Count == 0) return false; // Trace back to top and failed.
                    int lastIdx = proposal.Pop();
                    candidate.Push(curIdx);
                    candidate.Push(lastIdx);
                    if (localUsed[requiredLength].Count > 0)
                    {
                        localUsed[requiredLength].Pop();
                    }
                }
            }
            // Get result.
            int ijk = result.Count - 1;
            while(proposal.Count > 0)
            {
                int idx = proposal.Pop();
                Verse v = result[ijk];
                ijk--;
                stroke.Verses[idx].Content = v.Content;
            }
            // Update used indices.
            foreach(int key in localUsed.Keys)
            {
                usedIndices[key].AddRange(localUsed[key]);
            }
            stroke.IsCompleted = true;
            return true;
        }

        public Stroke[] GetResult()
        {
            return puzzleData.ToArray();
        }
        
        public string[] Translate(Stroke stk)
        {
            List<string> result = new List<string>();
            foreach(Verse v in stk.Verses)
            {
                StringBuilder sb = new StringBuilder(v.Length);
                foreach(ushort shr in v.Content)
                {
                    sb.Append(dictShr2Chr[shr]);
                }
                result.Add(sb.ToString());
            }
            return result.ToArray();
        }
    }

    public class TagStroke
    {
        public int nbVerses;
        public int[] lenVerses { get; set; }
        public int nbAnchors;
        public int[,] anchors { get; set; }
        public List<Point[]> positions;
        public int Idx { get; set; }
    }

    public class Stroke
    {
        public List<Verse> Verses { get; set; }
        public bool IsCompleted { get; set; }
        public List<int[]> Anchors { get; set; }

        public Stroke(int verses = 0, int anchors = 0)
        {
            Anchors = new List<int[]>(new int[anchors][]);
            Verses = new List<Verse>(new Verse[verses]);
            IsCompleted = false;
        }
    }

    public class Verse
    {
        public int Length { get; set; }
        public ushort[] Content { get; set; }

        public Verse(int len = 0, ushort[] content = null)
        {
            if(content == null && len > 0)
            {
                content = new ushort[len];
            }
            Content = content;
            Length = len;
        }

        public Verse(Verse v)
        {
            Content = v.Content;
            Length = v.Length;
        }
    }
}
