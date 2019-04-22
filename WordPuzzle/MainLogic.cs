using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace WordPuzzle
{
    public class SolverParam
    {
        public int MaxStep = -1;
        public bool NeedShuffle = false;
        public bool Reuse = false;
    }

    public class ExtraConstraint
    {
        public List<string> extraVerses = null;
        public List<string> extraAnchors = null;

        public List<ushort> extraA = null;
        public List<ushort[]> extraV = null;

        public ExtraConstraint(List<string> evs, List<string> eas)
        {
            extraVerses = evs;
            extraAnchors = eas;
        }
    }

    public class SuperChar
    {
        public string Name { get; set; }
        public List<Stroke> strokes;
        public List<TagStroke> descriptors;

        public SuperChar()
        {
            Name = "";
            strokes = new List<Stroke>();
            descriptors = new List<TagStroke>();
        }
    }
    public class MainLogic
    {
        public enum Solver
        {
            BasicSearch = 0,
            ExtraSearch = 1
        }
        // Singleton.
        private static MainLogic theInstance = null;
        // Data members.
        private Dictionary<char, ushort> dictChr2Shr = null;
        private readonly Dictionary<ushort, char> dictShr2Chr = null;
        private readonly Dictionary<int, List<string>> dictWordData = null;
        private Dictionary<int, List<ushort[]>> dictDataMatrices = null;
        private Dictionary<int, List<int>> usedIndices = null;
        public List<SuperChar> problemBank = null;
        private Stroke puzzleData = null;
        private Random rnd = null;

        private MainLogic()
        {
            dictChr2Shr = new Dictionary<char, ushort>();
            dictShr2Chr = new Dictionary<ushort, char>();
            dictWordData = new Dictionary<int, List<string>>();
            dictDataMatrices = new Dictionary<int, List<ushort[]>>();
            usedIndices = new Dictionary<int, List<int>>();
            problemBank = new List<SuperChar>();
            rnd = new Random();
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

        public bool ConvertChrShrEx(ExtraConstraint ec)
        {
            ec.extraA = new List<ushort>();
            foreach(string sa in ec.extraAnchors)
            {
                if(sa.Length == 0)
                {
                    ec.extraA.Add(0xFF);
                }
                else
                {
                    if (!dictChr2Shr.Keys.Contains(sa[0])) return false;
                    ec.extraA.Add(dictChr2Shr[sa[0]]);
                }
            }
            ec.extraV = new List<ushort[]>();
            foreach(string sv in ec.extraVerses)
            {
                if(sv.Length == 0)
                {
                    ec.extraV.Add(new ushort[] { 0xFF });
                }
                else
                {
                    char[] arr = sv.ToCharArray();
                    ushort[] vs = new ushort[arr.Length];
                    for (int i = 0; i < arr.Length; ++i)
                    {
                        if (!dictChr2Shr.Keys.Contains(arr[i])) return false;
                        vs[i] = dictChr2Shr[arr[i]];
                    }
                    int checkExist = dictDataMatrices[arr.Length].FindIndex(
                        (x)  => 
                        {
                            bool found = true;
                            for (int i = 0; i < arr.Length; ++i)
                            {
                                if(x[i] != vs[i])
                                {
                                    found = false;
                                    break;
                                }
                            }
                            return found;
                        });
                    if (checkExist < 0) return false;
                    ec.extraV.Add(vs);
                }
            }
            return true;
        }

        public void MakePuzzleDataEx(TagStroke tag, ExtraConstraint ecc)
        {
            Stroke stroke = new Stroke(tag.nbVerses, tag.nbAnchors);
            for (int i = 0; i < tag.nbVerses; ++i)
            {
                stroke.Verses[i] = new Verse(tag.LenVerses[i])
                {
                    Content = ecc.extraV[i]
                };
            }
            for (int i = 0; i < tag.nbAnchors; ++i)
            {
                stroke.Anchors[i] = new int[5];
            }
            for (int i = 0; i < tag.nbAnchors; ++i)
            {
                stroke.Anchors[i][0] = tag.Anchors[i][0];
                stroke.Anchors[i][1] = tag.Anchors[i][1];
                stroke.Anchors[i][2] = tag.Anchors[i][2];
                stroke.Anchors[i][3] = tag.Anchors[i][3];
                stroke.Anchors[i][4] = ecc.extraA[i];
                if (stroke.Verses[stroke.Anchors[i][0]].Content[0] != 0xFF)
                {
                    stroke.Anchors[i][4] = stroke.Verses[stroke.Anchors[i][0]].Content[stroke.Anchors[i][1]];
                }
                if (stroke.Verses[stroke.Anchors[i][2]].Content[0] != 0xFF)
                {
                    stroke.Anchors[i][4] = stroke.Verses[stroke.Anchors[i][2]].Content[stroke.Anchors[i][3]];
                }
            }
            puzzleData = stroke;
            
        }

        public void MakePuzzleData(TagStroke tag)
        {
            Stroke stroke = new Stroke(tag.nbVerses, tag.nbAnchors);
            for (int i = 0; i < tag.nbVerses; ++i)
            {
                stroke.Verses[i] = new Verse(tag.LenVerses[i]);
            }
            for (int i = 0; i < tag.nbAnchors; ++i)
            {
                stroke.Anchors[i] = new int[4];
            }
            for (int i = 0; i < tag.nbAnchors; ++i)
            {
                stroke.Anchors[i][0] = tag.Anchors[i][0];
                stroke.Anchors[i][1] = tag.Anchors[i][1];
                stroke.Anchors[i][2] = tag.Anchors[i][2];
                stroke.Anchors[i][3] = tag.Anchors[i][3];
            }
            puzzleData = stroke;
        }

        public void ClearPuzzleData()
        {
            puzzleData = null;
        }

        public void ClearUsedIndices()
        {
            foreach(int key in usedIndices.Keys)
            {
                usedIndices[key].Clear();
            }
        }

        public uint Solve(Solver solver, SolverParam param)
        {
            uint ns = 0;
            switch (solver)
            {
                case Solver.BasicSearch:
                    ns = DoBasicSearch(param);
                    break;
                case Solver.ExtraSearch:
                    ns = DoExtraSearch(param);
                    break;
                default:
                    break;
            }
            return ns;
        }
        private uint DoBasicSearch(SolverParam param)
        {
            uint walk = 0;
            
            Stroke stroke = puzzleData;
            int nbVerses = stroke.Verses.Count;
            Stack<int> proposal = new Stack<int>(),
                candidate = new Stack<int>(Enumerable.Reverse(Enumerable.Range(0, nbVerses)));
            List<Verse> result = new List<Verse>();
            List<int>[] visited = new List<int>[nbVerses];
            for(int vv = 0; vv < nbVerses; ++vv)
            {
                visited[vv] = new List<int>();
            }
            while(candidate.Count > 0)
            {
                int curIdx = candidate.Pop();
                List<int> curVisisted = visited[curIdx];
                // Existing verses construct the constraints.
                List<int[]> constraints = stroke.Anchors.FindAll(
                    a => a[2] == curIdx && proposal.Contains(a[0])
                    );
                int requiredLength = stroke.Verses[curIdx].Length;
                bool foundOne = true;
                // Look up in the data.
                int s = 0;
                Verse v = new Verse();
                if(curIdx > 0)
                {
                    for (s = 0; s < dictDataMatrices[requiredLength].Count; ++s)
                    {
                        if (param.MaxStep > 0 && walk > param.MaxStep)
                        {
                            return walk;
                        }
                        // Prevent replicate verses.
                        foundOne = true;
                        if (usedIndices[requiredLength].Contains(s) || curVisisted.Contains(s))
                        {
                            foundOne = false;
                            continue;
                        }
                        ++walk;
                        ushort[] content = dictDataMatrices[requiredLength][s];
                        foreach (int[] a in constraints)
                        {
                            // For each search, check the constraints.
                            ushort trait = content[a[3]],
                                check = result[a[0]].Content[a[1]];
                            if (trait != check)
                            {
                                foundOne = false;
                                break;
                            }
                        }
                        if (foundOne) break;
                    }
                }
                else
                {
                    if(param.NeedShuffle)
                    {
                        do
                        {
                            s = rnd.Next(dictDataMatrices[requiredLength].Count);
                        } while (curVisisted.Contains(s));
                    }
                    else
                    {
                        if (curVisisted.Count == 0) s = 0;
                        else s = curVisisted[curVisisted.Count - 1] + 1;
                    }
                }
                if(foundOne)
                {
                    // Next step.
                    curVisisted.Add(s);
                    v.Length = requiredLength;
                    v.Content = dictDataMatrices[requiredLength][s];
                    result.Add(v);
                    proposal.Push(curIdx);
                }
                else
                {                                                               
                    // Trace back.
                    curVisisted.Clear();
                    result.RemoveAt(result.Count - 1);
                    int lastIdx = proposal.Pop();
                    candidate.Push(curIdx);
                    candidate.Push(lastIdx);
                }
            }
            // Get result.
            stroke.Verses = result;
            for (int i = 0; i < nbVerses; ++i)
            {
                int requiredLength = stroke.Verses[i].Length;
                int usedIdx = visited[i][visited[i].Count - 1];
                if(!param.Reuse) usedIndices[requiredLength].Add(usedIdx);
            }
            stroke.IsCompleted = true;
            return walk;
        }
        
        public bool SearchSuccess()
        {
            return puzzleData.IsCompleted;
        }

        private uint DoExtraSearch(SolverParam param)
        {
            uint walk = 0;
            Random rnd = new Random();
            Stroke stroke = puzzleData;

            int nbVerses = stroke.Verses.Count;
            Stack<int> proposal = new Stack<int>(),
                candidate = new Stack<int>(Enumerable.Reverse(Enumerable.Range(0, nbVerses)));
            List<Verse> result = new List<Verse>();
            List<int>[] visited = new List<int>[nbVerses];
            for (int vv = 0; vv < nbVerses; ++vv)
            {
                visited[vv] = new List<int>();
            }
            while (candidate.Count > 0)
            {
                int curIdx = candidate.Pop();
                List<int> curVisisted = visited[curIdx];
                // Existing verses construct the constraints.
                List<int[]> constraints = stroke.Anchors;
                int requiredLength = stroke.Verses[curIdx].Length;
                bool foundOne = true;
                // Look up in the data.
                int s = 0;
                Verse v = new Verse();
                if (stroke.Verses[curIdx].Content[0] != 0xFF)
                {
                    // Fixed verse.
                    s = dictDataMatrices[requiredLength].FindIndex(
                        (x) =>
                        {
                            bool found = true;
                            for (int i = 0; i < requiredLength; ++i)
                            {
                                if(x[i] != stroke.Verses[curIdx].Content[i])
                                {
                                    found = false;
                                    break;
                                }
                            }
                            return found;
                        });
                }
                else
                {
                    // Variant verse.
                    for (s = 0; s < dictDataMatrices[requiredLength].Count; ++s)
                    {
                        if (param.MaxStep > 0 && walk > param.MaxStep)
                        {
                            return walk;
                        }
                        // Prevent replicate verses.
                        foundOne = true;
                        if (usedIndices[requiredLength].Contains(s) || curVisisted.Contains(s))
                        {
                            foundOne = false;
                            continue;
                        }
                        ++walk;
                        ushort[] content = dictDataMatrices[requiredLength][s];
                        foreach (int[] a in constraints)
                        {
                            if(a[4] == 0xFF)
                            {
                                // Variant anchor.
                                if (proposal.Contains(a[0]) && curIdx == a[2])
                                {
                                    ushort trait = content[a[3]],
                                    check = result[a[0]].Content[a[1]];
                                    if (trait != check)
                                    {
                                        foundOne = false;
                                        break;
                                    }
                                }
                            }
                            else
                            { 
                                // Fixed anchor.
                                if (a[0] == curIdx && content[a[1]] != a[4])
                                {
                                    foundOne = false;
                                    break;
                                }
                                if (a[2] == curIdx && content[a[3]] != a[4])
                                {
                                    foundOne = false;
                                    break;
                                }

                            }
                            // For each search, check the constraints.
                            
                        }
                        if (foundOne) break;
                    }
                }
                if (foundOne)
                {
                    // Next step.
                    curVisisted.Add(s);
                    v.Length = requiredLength;
                    v.Content = dictDataMatrices[requiredLength][s];
                    result.Add(v);
                    proposal.Push(curIdx);
                }
                else
                {
                    // Trace back.
                    curVisisted.Clear();
                    result.RemoveAt(result.Count - 1);
                    int lastIdx = proposal.Pop();
                    candidate.Push(curIdx);
                    candidate.Push(lastIdx);
                }
            }
            // Get result.
            stroke.Verses = result;
            for (int i = 0; i < nbVerses; ++i)
            {
                int requiredLength = stroke.Verses[i].Length;
                int usedIdx = visited[i][visited[i].Count - 1];
                if (!param.Reuse) usedIndices[requiredLength].Add(usedIdx);
            }
            stroke.IsCompleted = true;
            return walk;
        }

        public Stroke GetResult()
        {
            return puzzleData;
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

        public string TranslateOnce(ushort shr)
        {
            return dictShr2Chr[shr].ToString();
        }

        public void ShuffleDatabase()
        {
            Random rnd = new Random();
            for(int i = 0; i < dictDataMatrices.Count; ++i)
            {
                int key = dictDataMatrices.Keys.ElementAt(i);
                int len = dictDataMatrices[key].Count;
                dictDataMatrices[key] = dictDataMatrices[key].OrderBy(x => rnd.Next(0, len)).ToList();
            }
        }
    }

    public class TagStroke
    {
        public int nbVerses;
        public List<int> LenVerses { get; set; }
        public int nbAnchors;
        public List<int[]> Anchors { get; set; }
        public List<Point[]> positions;

        public TagStroke()
        {
            nbAnchors = 0;
            nbVerses = 0;
            LenVerses = new List<int>();
            Anchors = new List<int[]> { };
            positions = new List<Point[]>();
        }
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
