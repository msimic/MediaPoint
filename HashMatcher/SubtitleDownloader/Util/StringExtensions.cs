
namespace HashMatcher.Util
{
  public static class StringExtensions
  {
    public static bool IsNumeric(this string str)
    {
      for (int index = 0; index < str.Length; ++index)
      {
        if (!char.IsDigit(str[index]))
          return false;
      }
      return true;
    }
  }
}
