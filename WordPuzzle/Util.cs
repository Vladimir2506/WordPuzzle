using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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
    }
}
