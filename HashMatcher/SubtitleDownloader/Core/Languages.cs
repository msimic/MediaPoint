using System;
using System.Collections.Generic;
using System.Linq;

namespace HashMatcher
{
  public static class Languages
  {
    private static readonly SubLang DefaultLanguage = new SubLang("eng", "English");
    private static readonly SubLang[] aliases = new SubLang[1]
    {
      new SubLang("nld", "Dutch")
    };
    private static readonly SubLang[] languages = new SubLang[44]
    {
      new SubLang("bos", "Bosnian"),
      new SubLang("slv", "Slovenian"),
      new SubLang("hrv", "Croatian"),
      new SubLang("srp", "Serbian"),
      new SubLang("eng", "English"),
      new SubLang("spa", "Spanish"),
      new SubLang("fre", "French"),
      new SubLang("gre", "Greek"),
      new SubLang("ger", "German"),
      new SubLang("rus", "Russian"),
      new SubLang("chi", "Chinese"),
      new SubLang("por", "Portuguese"),
      new SubLang("dut", "Dutch"),
      new SubLang("ita", "Italian"),
      new SubLang("rum", "Romanian"),
      new SubLang("cze", "Czech"),
      new SubLang("ara", "Arabic"),
      new SubLang("pol", "Polish"),
      new SubLang("tur", "Turkish"),
      new SubLang("swe", "Swedish"),
      new SubLang("fin", "Finnish"),
      new SubLang("hun", "Hungarian"),
      new SubLang("dan", "Danish"),
      new SubLang("heb", "Hebrew"),
      new SubLang("est", "Estonian"),
      new SubLang("slo", "Slovak"),
      new SubLang("ind", "Indonesian"),
      new SubLang("per", "Persian"),
      new SubLang("bul", "Bulgarian"),
      new SubLang("jpn", "Japanese"),
      new SubLang("alb", "Albanian"),
      new SubLang("bel", "Belarusian"),
      new SubLang("hin", "Hindi"),
      new SubLang("gle", "Irish"),
      new SubLang("ice", "Icelandic"),
      new SubLang("cat", "Catalan"),
      new SubLang("kor", "Korean"),
      new SubLang("lav", "Latvian"),
      new SubLang("lit", "Lithuanian"),
      new SubLang("mac", "Macedonian"),
      new SubLang("nor", "Norwegian"),
      new SubLang("tha", "Thai"),
      new SubLang("ukr", "Ukrainian"),
      new SubLang("vie", "Vietnamese")
    };

    public static string GetLanguageCode(string languageName)
    {
      return Languages.FindLanguageCode(languageName) ?? Languages.DefaultLanguage.Code;
    }

    public static string FindLanguageCode(string languageName)
    {
      if (string.IsNullOrEmpty(languageName))
        throw new ArgumentException("Language name cannot be null or empty!");
      SubLang subLang = Enumerable.FirstOrDefault<SubLang>(Enumerable.Where<SubLang>((IEnumerable<SubLang>) Languages.languages, (Func<SubLang, bool>) (l => l.Name.Equals(languageName, StringComparison.OrdinalIgnoreCase))));
      if (subLang != null)
        return subLang.Code;
      return (string) null;
    }

    public static string GetLanguageName(string languageCode)
    {
      if (string.IsNullOrEmpty(languageCode))
        throw new ArgumentException("Language code cannot be null or empty!");
      if (Enumerable.Count<char>((IEnumerable<char>) languageCode) != 3)
        throw new ArgumentException("Invalid ISO 639-2 language code!");
      SubLang languageCodeInternal = Languages.FindLanguageByLanguageCodeInternal(languageCode);
      if (languageCodeInternal != null)
        return languageCodeInternal.Name;
      return Languages.DefaultLanguage.Name;
    }

    public static bool IsSupportedLanguageCode(string languageCode)
    {
      return Languages.FindLanguageByLanguageCodeInternal(languageCode) != null;
    }

    public static bool IsSupportedLanguageName(string languageName)
    {
      return Enumerable.Any<SubLang>((IEnumerable<SubLang>) Languages.languages, (Func<SubLang, bool>) (lang => lang.Name.Equals(languageName, StringComparison.OrdinalIgnoreCase)));
    }

    public static List<string> GetLanguageNames()
    {
      return Enumerable.ToList<string>(Enumerable.Select<SubLang, string>((IEnumerable<SubLang>) Languages.languages, (Func<SubLang, string>) (lang => lang.Name)));
    }

    private static SubLang FindLanguageByLanguageCodeInternal(string languageCode)
    {
      return Enumerable.FirstOrDefault<SubLang>(Enumerable.Where<SubLang>((IEnumerable<SubLang>) Languages.languages, (Func<SubLang, bool>) (l => l.Code.Equals(languageCode, StringComparison.OrdinalIgnoreCase)))) ?? Enumerable.FirstOrDefault<SubLang>(Enumerable.Where<SubLang>((IEnumerable<SubLang>) Languages.aliases, (Func<SubLang, bool>) (l => l.Code.Equals(languageCode, StringComparison.OrdinalIgnoreCase))));
    }
  }
}
