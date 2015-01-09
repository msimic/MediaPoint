using System.Collections.Generic;
using System.IO;

namespace HashMatcher
{
  public interface ISubtitleDownloader
  {
    int SearchTimeout { get; set; }

    string GetName();

    List<Subtitle> SearchSubtitles(SearchQuery query);

    List<Subtitle> SearchSubtitles(EpisodeSearchQuery query);

    List<Subtitle> SearchSubtitles(ImdbSearchQuery query);

    List<FileInfo> SaveSubtitle(Subtitle subtitle);
  }
}
