using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HashMatcher.Util
{
  public static class SimilarityUtils
  {
    public static double CompareStrings(string str1, string str2)
    {
      List<string> list1 = SimilarityUtils.WordLetterPairs(str1.ToUpper());
      List<string> list2 = SimilarityUtils.WordLetterPairs(str2.ToUpper());
      int num1 = 0;
      int num2 = list1.Count + list2.Count;
      for (int index1 = 0; index1 < list1.Count; ++index1)
      {
        for (int index2 = 0; index2 < list2.Count; ++index2)
        {
          if (list1[index1] == list2[index2])
          {
            ++num1;
            list2.RemoveAt(index2);
            break;
          }
        }
      }
      return 2.0 * (double) num1 / (double) num2;
    }

    private static List<string> WordLetterPairs(string str)
    {
      List<string> list = new List<string>();
      string[] strArray = Regex.Split(str, "\\s");
      for (int index = 0; index < strArray.Length; ++index)
      {
        if (!string.IsNullOrEmpty(strArray[index]))
        {
          foreach (string str1 in SimilarityUtils.LetterPairs(strArray[index]))
            list.Add(str1);
        }
      }
      return list;
    }

    private static string[] LetterPairs(string str)
    {
      int length = str.Length - 1;
      string[] strArray = new string[length];
      for (int startIndex = 0; startIndex < length; ++startIndex)
        strArray[startIndex] = str.Substring(startIndex, 2);
      return strArray;
    }
  }
}
