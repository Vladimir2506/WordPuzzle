using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace WordPuzzle
{
    public class Util
    {
        public static void LoadWordBase(
            string fnData, 
            string fnLUT, 
            Dictionary<char, ushort> dictChr2Shr, 
            Dictionary<ushort, char> dictShr2Chr,
            Dictionary<int, List<string>> dictWordData
            )
        {
            // Load LUT file.
            StreamReader sr = new StreamReader(fnLUT, Encoding.UTF8);
            string line;
            char[] delim = { ':' };
            while((line = sr.ReadLine()) != null)
            {
                string[] seps = line.Split(delim);
                if(seps.Length == 2)
                {
                    dictChr2Shr.Add(seps[0][0], Convert.ToUInt16(seps[1]));
                    dictShr2Chr.Add(Convert.ToUInt16(seps[1]), seps[0][0]);
                }
            }
            sr.Close();

            // Load word data.
            sr = new StreamReader(fnData, Encoding.UTF8);
            while((line = sr.ReadLine()) != null)
            {
                char[] removal = { '\t', '\n', '\r' };
                line = line.TrimEnd(removal);
                if(dictWordData.ContainsKey(line.Length))
                {
                    dictWordData[line.Length].Add(line);
                }
                else
                {
                    dictWordData.Add(line.Length, new List<string>() { line });
                }
            }
            sr.Close();
        }

        public static void Chr2Shr(
            Dictionary<int, List<string>> dictChr, 
            Dictionary<int, List<ushort[]>> dictShr,
            Dictionary<char, ushort> dictCvt
            )
        {
            foreach(int key in dictChr.Keys)
            {
                List<string> dataInChar = dictChr[key];
                List<ushort[]> dataInShort = new List<ushort[]>(dataInChar.Count);
                for(int i = 0; i < dataInChar.Count; ++i)
                {
                    dataInShort.Add(new ushort[key]);
                }
                for(int i = 0; i < dataInChar.Count; ++i)
                {
                    for(int j = 0; j < key; ++j)
                    {
                        dataInShort[i][j] = dictCvt[dataInChar[i][j]];
                    }
                }
                dictShr.Add(key, dataInShort);
            }
        }
        
        public static void SaveProblemBank(List<SuperChar> bank, string fnDB)
        {
            if (File.Exists(fnDB)) File.Delete(fnDB);
            using (StreamWriter sw = new StreamWriter(fnDB, false))
            {
                string line = "SuperChars:" + bank.Count.ToString();
                sw.WriteLine(line);
                for(int k = 0;k < bank.Count; ++k)
                {
                    SuperChar sc = bank[k];
                    line = "SuperChar:" + k.ToString() + ",Name:" + sc.Name;
                    sw.WriteLine(line);
                    int nbStrokes = sc.descriptors.Count;
                    line = "Strokes:" + nbStrokes.ToString();
                    sw.WriteLine(line);
                    for(int t = 0; t < nbStrokes; ++t)
                    {
                        TagStroke descriptor = sc.descriptors[t];
                        line = "Stroke:" + t.ToString();
                        sw.WriteLine(line);
                        int nV = descriptor.nbVerses, nA = descriptor.nbAnchors;
                        line = "Verses:" + nV.ToString();
                        sw.WriteLine(line);
                        for (int v = 0; v < nV; ++v)
                        {
                            Point[] points = descriptor.positions[v];
                            int len = descriptor.LenVerses[v];
                            line = v.ToString() + ":" + len.ToString() + " ";
                            for (int l = 0; l < len; ++l)
                            {
                                line += points[l].ToString() + " ";
                            }
                            sw.WriteLine(line);
                        }
                        line = "Anchors:" + nA.ToString();
                        sw.WriteLine(line);
                        for (int a = 0; a < nA; ++a)
                        {
                            line = a.ToString() + ":"
                                + descriptor.Anchors[a][0].ToString() + ","
                                + descriptor.Anchors[a][1].ToString() + ","
                                + descriptor.Anchors[a][2].ToString() + ","
                                + descriptor.Anchors[a][3].ToString();
                            sw.WriteLine(line);
                        }
                    }
                }
            }
        }

        public static void LoadProblemBank(List<SuperChar> bank, string fnDB)
        {
            if (!File.Exists(fnDB)) throw new FileNotFoundException();
            using (StreamReader sr = new StreamReader(fnDB, Encoding.UTF8))
            {
                int nbSuperChars = 0;
                string[] res = null;
                string line = sr.ReadLine();
                if (line.StartsWith("SuperChars:"))
                {
                    res = line.Split(new char[] { ':' });
                    nbSuperChars = Convert.ToInt32(res[1]);
                }
                bank.Capacity = nbSuperChars;
                for (int k = 0; k < nbSuperChars; ++k)
                {
                    SuperChar sc = new SuperChar
                    {
                        descriptors = new List<TagStroke>()
                    };
                    line = sr.ReadLine();
                    res = line.Split(new char[] { ':', ',' });
                    if (res[0] == "SuperChar" && res[2] == "Name")
                    {
                        if (k != Convert.ToInt32(res[1])) throw new InvalidDataException();
                        sc.Name = res[3];
                    }
                    line = sr.ReadLine();
                    res = line.Split(new char[] { ':' });
                    int nStroke = 0;
                    if(res[0] == "Strokes")
                    {
                        nStroke = Convert.ToInt32(res[1]);
                    }
                    for(int t = 0; t < nStroke; ++t)
                    {
                        TagStroke descriptor = new TagStroke
                        {
                            positions = new List<Point[]>()
                        };
                        line = sr.ReadLine();
                        res = line.Split(new char[] { ':' });
                        if(res[0] != "Stroke" || Convert.ToInt32(res[1]) != t) throw new InvalidDataException();
                        line = sr.ReadLine();
                        res = line.Split(new char[] { ':', ',' });
                        int nV = 0, nA = 0;
                        if (res[0] == "Verses")
                        {
                            nV = Convert.ToInt32(res[1]);
                            descriptor.nbVerses = nV;
                            for (int v = 0; v < nV; ++v)
                            {
                                line = sr.ReadLine();
                                res = line.Split(new char[] { ':', '_', ' ' });
                                if (v != Convert.ToInt32(res[0])) throw new InvalidDataException();
                                int len = Convert.ToInt32(res[1]);
                                descriptor.LenVerses.Add(len);
                                Point[] points = new Point[len];
                                for (int j = 0; j < len; ++j)
                                {
                                    points[j] = Point.Parse(res[j + 2]);
                                }
                                descriptor.positions.Add(points);
                            }
                        }
                        line = sr.ReadLine();
                        res = line.Split(new char[] { ':', ',' });
                        if (res[0] == "Anchors")
                        {
                            nA = Convert.ToInt32(res[1]);
                            descriptor.nbAnchors = nA;
                            descriptor.Anchors = new List<int[]>();
                            for (int a = 0; a < nA; ++a)
                            {
                                line = sr.ReadLine();
                                res = line.Split(new char[] { ':' });
                                if (a != Convert.ToInt32(res[0])) throw new InvalidDataException();
                                res = res[1].Split(new char[] { ',' });
                                int[] aa = new int[4];
                                for (int l = 0; l < 4; ++l)
                                {
                                    aa[l] = Convert.ToInt32(res[l]);
                                }
                                descriptor.Anchors.Add(aa);
                            }
                        }
                        sc.descriptors.Add(descriptor);
                    }
                    bank.Add(sc);
                }
            }
        }
    }
}
