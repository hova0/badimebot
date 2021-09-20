using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace badimebot
{
    public static class WordExtensions
    {
        public static IEnumerable<(string word, int index)> GetWordsWithIndex(this string fullstring)
        {
            int index = 0;
            while (true)
            {
                if (fullstring.IndexOf(' ', index) == -1)
                {
                    yield return (fullstring.Substring(index), index);
                    yield break;
                }
                string ret = fullstring.Substring(index, fullstring.IndexOf(' ', index) - index);
                yield return (ret, index);
                index += ret.Length + 1;
            }
        }

        public static IEnumerable<string> GetWords(this string fullstring)
        {
            foreach (var s in GetWordsWithIndex(fullstring))
                yield return s.word;

            //int index = 0;
            //while (true)
            //{
            //	if (fullstring.IndexOf(' ', index) == -1)
            //	{
            //		yield return fullstring.Substring(index);
            //		yield break;
            //	}
            //	string ret = fullstring.Substring(index, fullstring.IndexOf(' ', index) - index);
            //	index += ret.Length + 1;
            //	yield return ret;
            //}
        }

        public static IEnumerable<(string word, int index)> GetWordsWithIndexQuoteable(this string fullstring)
        {
            int index = 0;
            while (true)
            {
                if (index >= fullstring.Length)
                    yield break;
                if (fullstring.IndexOf(' ', index) == -1)
                {
                    yield return (fullstring.Substring(index), index);
                    yield break;
                }
                string ret;
                Char foundquote = isQuote(fullstring[index]);
                if (foundquote != Char.MinValue)
                {
                    ret = fullstring.Substring(index, fullstring.IndexOf(foundquote, index + 1) - index + 1);
                }
                else
                {
                    ret = fullstring.Substring(index, fullstring.IndexOf(' ', index) - index);
                }

                yield return (ret, index);
                index += ret.Length + 1;
            }
        }


        private static char isQuote(char c)
        {
            if (c == '\'')
                return c;
            if (c == '"')
                return c;
            return Char.MinValue;
        }

    }
}
