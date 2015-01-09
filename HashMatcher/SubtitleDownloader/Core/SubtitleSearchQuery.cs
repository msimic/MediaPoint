using System;
using System.Collections.Generic;
using System.Linq;

namespace HashMatcher
{
  public abstract class SubtitleSearchQuery
  {
    private string[] languageCodes;

    public string[] LanguageCodes
    {
      get
      {
        return this.languageCodes;
      }
      set
      {
        if (Enumerable.Any<string>((IEnumerable<string>) value, (Func<string, bool>) (lang => lang.Length != 3)))
          throw new ArgumentException("Language codes must be ISO 639-2 Code!");
        this.languageCodes = value;
      }
    }

    protected SubtitleSearchQuery()
    {
      this.LanguageCodes = new string[1]
      {
        "eng"
      };
    }

    public bool HasLanguageCode(string languageCode)
    {
      if (languageCode == null)
        return false;
      if (languageCode.Length != 3)
        throw new ArgumentException("Language code must be ISO 639-2 Code!");
      return Enumerable.Any<string>((IEnumerable<string>) this.languageCodes, (Func<string, bool>) (code => code.Equals(languageCode.ToLower())));
    }
  }
}
