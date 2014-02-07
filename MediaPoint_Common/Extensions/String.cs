using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPoint.Common.Extensions
{
    public static class String
    {
        public static string Replace(this string s, char[] chars, char replacement)
        {
            StringBuilder sb = new StringBuilder(s);

            foreach (var str in chars)
            {
                sb.Replace(str, replacement);
            }

            return sb.ToString();
        }

        public static string Replace(this string s, string[] chars, string replacement)
        {
            StringBuilder sb = new StringBuilder(s);

            foreach (var str in chars)
            {
                sb.Replace(str, replacement);
            }
            
            return sb.ToString();
        }
    }
}
