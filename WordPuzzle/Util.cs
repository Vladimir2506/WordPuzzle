using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
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
            Dictionary<int, ushort[][]> dictShr,
            Dictionary<char, ushort> dictCvt
            )
        {
            foreach(int key in dictChr.Keys)
            {
                List<string> dataInChar = dictChr[key];
                ushort[][] dataInShort = new ushort[dataInChar.Count][];
                for(int i = 0; i < dataInChar.Count; ++i)
                {
                    dataInShort[i] = new ushort[key];
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
                    int nV = sc.descriptor.nbVerses, nA = sc.descriptor.nbAnchors;
                    line = "Verses:" + nV.ToString();
                    sw.WriteLine(line);
                    for(int v = 0; v < nV; ++v)
                    {
                        Point[] points = sc.positions[v];
                        int len = sc.descriptor.lenVerses[v];
                        line = v.ToString() + ":" + len.ToString() + " ";
                        for(int l = 0; l < len; ++l)
                        {
                            line += points[l].ToString() + " ";
                        }
                        sw.WriteLine(line);
                    }
                    line = "Anchors:" + nA.ToString();
                    sw.WriteLine(line);
                    for(int a = 0; a < nA; ++a)
                    {
                        line = a.ToString() + ":"
                            + sc.descriptor.anchors[a, 0].ToString() + ","
                            + sc.descriptor.anchors[a, 1].ToString() + ","
                            + sc.descriptor.anchors[a, 2].ToString() + ","
                            + sc.descriptor.anchors[a, 3].ToString();
                        sw.WriteLine(line);
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
                    SuperChar sc = new SuperChar();
                    sc.descriptor = new TagStroke();
                    sc.positions = new List<Point[]>();
                    line = sr.ReadLine();
                    res = line.Split(new char[] { ':', ',' });
                    if (res[0] == "SuperChar" && res[2] == "Name")
                    {
                        if (k != Convert.ToInt32(res[1])) throw new InvalidDataException();
                        sc.Name = res[3];
                    }

                    line = sr.ReadLine();
                    res = line.Split(new char[] { ':', ',' });
                    int nV = 0, nA = 0;
                    if (res[0] == "Verses")
                    {
                        nV = Convert.ToInt32(res[1]);
                        sc.descriptor.nbVerses = nV;
                        sc.descriptor.lenVerses = new int[nV];
                        for (int v = 0; v < nV; ++v)
                        {
                            line = sr.ReadLine();
                            res = line.Split(new char[] { ':', '_', ' ' });
                            if (v != Convert.ToInt32(res[0])) throw new InvalidDataException();
                            int len = Convert.ToInt32(res[1]);
                            sc.descriptor.lenVerses[v] = len;
                            Point[] points = new Point[len];
                            for (int j = 0; j < len; ++j)
                            {
                                points[j] = Point.Parse(res[j + 2]);
                            }
                            sc.positions.Add(points);
                        }
                    }
                    line = sr.ReadLine();
                    res = line.Split(new char[] { ':', ',' });
                    if (res[0] == "Anchors")
                    {
                        nA = Convert.ToInt32(res[1]);
                        sc.descriptor.nbAnchors = nA;
                        sc.descriptor.anchors = new int[nA, 4];
                        for (int a = 0; a < nA; ++a)
                        {
                            line = sr.ReadLine();
                            res = line.Split(new char[] { ':' });
                            if (a != Convert.ToInt32(res[0])) throw new InvalidDataException();
                            res = res[1].Split(new char[] { ',' });
                            for (int l = 0; l < 4; ++l) sc.descriptor.anchors[a, l] = Convert.ToInt32(res[l]);
                        }
                    }
                    bank.Add(sc);
                }
            }
        }
    }
}
