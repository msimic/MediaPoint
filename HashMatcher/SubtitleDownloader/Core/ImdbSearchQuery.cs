using HashMatcher.Util;
using System;

namespace HashMatcher
{
  public class ImdbSearchQuery : SubtitleSearchQuery
  {
    public string ImdbId { get; private set; }

    public int? ImdbIdNullable
    {
      get
      {
        int? nullable = new int?();
        if (this.ImdbId != null)
          nullable = new int?(int.Parse(this.ImdbId));
        return nullable;
      }
    }

    public ImdbSearchQuery(string imdbId)
    {
      if (!StringExtensions.IsNumeric(imdbId))
        throw new ArgumentException("IMDB ID value must be numeric, like \"0813715\"!");
      this.ImdbId = imdbId;
    }
  }
}
