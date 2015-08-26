using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutomateBarcode
{
    class Permutation
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public List<string> GetPossibleBarcodes(string s1, string s2, int needNum)
        {

            log.Info("GetPossibleBarcodes");
            //1 extract chars not Digital, char
            Dictionary<int,char> SpecialChar_Pos = new Dictionary<int,char>();
            //List<char> chars1 = new List<char>();
            List<char> chars2 = new List<char>();
            
            int DiffPos = GetDiffPos(s1, s2);
            string sCommonEnd = s1.Substring(DiffPos+1);
            for (int i = 0; i <= DiffPos; i++)
            {
                char ch = s1[i];
                bool isSpecial = !Char.IsLetterOrDigit(ch);
                if (isSpecial)
                    SpecialChar_Pos.Add(i, ch);
                else
                {
                    //chars1.Add(s1[i]);
                    chars2.Add(s2[i]);
                }
            }

            List<string> newStrs = new List<string>();
            string newStr = System.String.Concat(chars2.ToArray());
            
            for (int i = 0; i < needNum; i++)
            {
                bool bok = IncreamentString(ref newStr);
                newStrs.Add(AddSpecialCharsBack(newStr, SpecialChar_Pos) + sCommonEnd);
            }
            return newStrs;
        }

        private int GetDiffPos(string s1, string s2)
        {
            for (int i = 0; i < s1.Length; i++)
            {
                if (s1[i] != s2[i])
                    return i;
            }
            string sError = "From GetDiffPos():两个条形码完全一样！";
            log.Error(sError);
            throw new Exception(sError);
        }

        private string AddSpecialCharsBack(string newStr, Dictionary<int, char> SpecialChar_Pos)
        {
            foreach (KeyValuePair<int, char> pair in SpecialChar_Pos)
            {
                newStr = newStr.Insert(pair.Key, Convert.ToString(pair.Value));
            }
            return newStr;
        }

   

        bool IncreamentString(ref string s)
        {
            List<char> newStr = new List<char>();
            foreach (char ch in s)
            {
                newStr.Add(ch);
            }

            int curPos = s.Length - 1;
            char maxChar;
            for (; ; )
            {
                char ch = s[curPos];
                maxChar = GetMaxChar(ch);
                if (ch != maxChar)
                {
                    newStr[curPos] = ++ch;
                    s = GetString(newStr);
                    break;
                }
                else
                {
                    newStr[curPos] = GetMinChar(ch);
                }
                curPos--;
                if (curPos < 0)
                {
                    s = GetString(newStr);
                    break;
                }
            }
            return true;
        }

        private char GetMinChar(char ch)
        {
            if (Char.IsDigit(ch))
            {
                return '0';
            }
            
            if (Char.IsLower(ch))
                return 'a';

            if (Char.IsUpper(ch))
                return 'A';

            throw new Exception("all characters must be digital or letter");
        }

        private char GetMaxChar(char ch)
        {
            if (Char.IsDigit(ch))
            {
                return '9';
            }
            
            if (Char.IsLower(ch))
                return 'z';

            if (Char.IsUpper(ch))
                return 'Z';

            throw new Exception("all characters must be digital or letter");
        }

        private string GetString(List<char> newStr)
        {
            string s = "";
            foreach (char ch in newStr)
            {
                s += ch;
            }
            return s;
        }
    }
}

