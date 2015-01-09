using System;
using System.Collections.Generic;
using System.Linq;

namespace HashMatcher
{
  public class Subtitle
  {
    public string Id { get; private set; }

    public string ProgramName { get; private set; }

    public string FileName { get; private set; }

    public string LanguageCode { get; private set; }

    public string ImdbCode { get; private set; }

    public Subtitle(string id, string programName, string fileName, string languageCode, string imdbCode)
    {
      if (string.IsNullOrEmpty(id))
        throw new ArgumentException("ID cannot be null or empty!");
      if (string.IsNullOrEmpty(fileName))
        throw new ArgumentException("File name cannot be null or empty!");
      if (string.IsNullOrEmpty(languageCode))
        throw new ArgumentException("Language code cannot be null or empty!");
      if (languageCode != null && Enumerable.Count<char>((IEnumerable<char>) languageCode) != 3)
        throw new ArgumentException("Language code must be ISO 639-2 Code!");
      if (!Languages.IsSupportedLanguageCode(languageCode))
        throw new ArgumentException("Language code '" + languageCode + "' is not supported!");
      this.Id = id;
      this.ProgramName = programName;
      this.FileName = fileName;
      this.LanguageCode = languageCode;
      this.ImdbCode = imdbCode;
    }
  }
}
