using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AZUSA
{
    //static class for various string processing functions
    static class Utils
    {

        /// <summary>
        /// Extract content in innermost brackets
        /// i.e. 0 is extracted from {1{0}}
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="lbrac"></param>
        /// <param name="rbrac"></param>
        /// <returns></returns>
        public static List<string> ExtractFromBracket(string Source, char lbrac = '{', char rbrac = '}')
        {
            bool inBrac = false;
            string content = "";
            List<string> result = new List<string>();
            foreach (char c in Source)
            {
                if (c == lbrac)
                {
                    inBrac = true;

                    continue;
                }
                if (c == rbrac)
                {
                    if (inBrac)
                    {
                        inBrac = false;
                        result.Add(content);
                        content = "";
                    }
                }
                if (inBrac)
                {
                    content += c;
                }
            }

            return result;
        }


        public static List<string> SplitWithProtection(string Source,char sep= ',',char lquot = '"', char rquot = '"')
        {
            bool protecting = false;
            List<string> results = new List<string>();
            string tmp = "";

            foreach (char c in Source)
            {
                if (!protecting)
                {
                    if (c == sep && tmp != "")
                    {
                        results.Add(tmp);
                        tmp = "";
                    }
                    else if (c == lquot)
                    {
                        protecting = true;
                        tmp = tmp + c;

                    }
                    else
                    {

                        tmp = tmp + c;

                    }

                }
                else
                {
                    if (c == rquot)
                    {
                        protecting = false;                        
                    }
                    tmp = tmp + c;
                }

            }
            //last element
            if (tmp != "")
            {
                results.Add(tmp);
            }

            return results;
        }

        public static string UriEncode(string text)
        {
            return Uri.EscapeDataString(text).Replace("%5C", "\\").Replace("%2C", ",");
        }

        public static string UriDecode(string text)
        {
            return Uri.UnescapeDataString(text);
        }

        /// <summary>
        /// Return the string to the right of the first encountered separator.
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="sep">Separator</param>
        /// <returns>Content to the right of separator</returns>
        public static string RSplit(string source, string sep)
        {
            int index = source.ToLower().IndexOf(sep.ToLower());
            if (index == -1) return "";
            return source.Substring(index + sep.Length).Trim();
        }

        /// <summary>
        /// Return the string to the left of the first encountered separator.
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="sep">Separator</param>
        /// <returns>Content to the right of separator</returns>
        public static string LSplit(string source, string sep)
        {
            int index = source.ToLower().IndexOf(sep.ToLower());
            if (index == -1) return "";
            return source.Substring(0, index).Trim();
        }
    }
}
