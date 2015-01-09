using System;

namespace HashMatcher
{
  public class EpisodeSearchQuery : SubtitleSearchQuery
  {
    public string SerieTitle { get; private set; }

    public int Episode { get; private set; }

    public int Season { get; private set; }

    public int? TvdbId { get; private set; }

    [Obsolete("This method is preserved for backwards compatibility")]
    public EpisodeSearchQuery(string serieTitle, int season, int episode)
    {
      this.SerieTitle = serieTitle;
      this.Season = season;
      this.Episode = episode;
    }

    public EpisodeSearchQuery(string serieTitle, int season, int episode, int? tvdbId = null)
    {
      this.SerieTitle = serieTitle;
      this.Season = season;
      this.Episode = episode;
      this.TvdbId = tvdbId;
    }
  }
}
