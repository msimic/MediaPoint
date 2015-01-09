using CookComputing.XmlRpc;
using SubtitleDownloader;
using HashMatcher;
using HashMatcher.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenSubtitlesSearch
{
  public class OpenSubtitlesDownloader : ISubtitleDownloader
  {
    private readonly string UserAgent = Configuration.OpenSubtitlesUserAgent;
    private const string ApiUrl = "http://api.opensubtitles.org/xml-rpc";
    private IOpenSubtitlesProxy openSubtitlesProxy;
    private string token;
    private int searchTimeout;

    public int SearchTimeout
    {
      get
      {
        return this.searchTimeout;
      }
      set
      {
        this.searchTimeout = value;
      }
    }

    public string GetName()
    {
      return "OpenSubtitles";
    }

    public List<Subtitle> SearchSubtitles(SearchQuery query)
    {
      this.CreateConnectionAndLogin();
      if (this.searchTimeout > 0)
        this.openSubtitlesProxy.Timeout = this.searchTimeout * 1000;
      return this.PerformSearch(new subInfo[1]
      {
        new subInfo(this.GetLanguageCodes((SubtitleSearchQuery) query), query.FileHash ?? "", query.FileSize, new int?(), query.Query)
      }, query.Year);
    }

    public List<Subtitle> SearchSubtitles(EpisodeSearchQuery query)
    {
      this.CreateConnectionAndLogin();
      if (this.searchTimeout > 0)
        this.openSubtitlesProxy.Timeout = this.searchTimeout * 1000;
      string str1 = "e" + string.Format("{0:00}", (object) query.Episode);
      string str2 = "s" + string.Format("{0:00}", (object) query.Season);
      string query1 = query.SerieTitle + " " + str2 + str1;
      return this.PerformSearch(new subInfo[1]
      {
        new subInfo(this.GetLanguageCodes((SubtitleSearchQuery) query), "", new int?(), new int?(), query1)
      }, new int?());
    }

    public List<Subtitle> SearchSubtitles(ImdbSearchQuery query)
    {
      this.CreateConnectionAndLogin();
      if (this.searchTimeout > 0)
        this.openSubtitlesProxy.Timeout = this.searchTimeout * 1000;
      return this.PerformSearch(new subInfo[1]
      {
        new subInfo(this.GetLanguageCodes((SubtitleSearchQuery) query), "", new int?(), query.ImdbIdNullable, "")
      }, new int?());
    }

    public List<FileInfo> SaveSubtitle(Subtitle subtitle)
    {
      this.CreateConnectionAndLogin();
      subdata subdata = this.openSubtitlesProxy.DownloadSubtitles(this.token, new string[1]
      {
        subtitle.Id
      });
      if (subdata == null || subdata.data == null || Enumerable.Count<subtitle>((IEnumerable<subtitle>) subdata.data) <= 0)
        throw new Exception("Subtitle not found with ID '" + subtitle.Id + "'");
      string str = Path.GetTempPath() + subtitle.FileName;
      if (File.Exists(str))
        File.Delete(str);
      FileUtils.WriteNewFile(str, Decoder.DecodeAndDecompress(subdata.data[0].data));
      return new List<FileInfo>()
      {
        new FileInfo(str)
      };
    }

    private List<Subtitle> PerformSearch(subInfo[] searchQuery, int? queryYear)
    {
      subrt subResults;
      try
      {
        subResults = this.openSubtitlesProxy.SearchSubtitles(this.token, searchQuery);
      }
      catch (XmlRpcTypeMismatchException ex)
      {
        return new List<Subtitle>(0);
      }
      return this.CreateSubtitleResults(subResults, queryYear);
    }

    private List<Subtitle> CreateSubtitleResults(subrt subResults, int? queryYear)
    {
      List<Subtitle> list = new List<Subtitle>();
      if (subResults != null && subResults.data != null && Enumerable.Count<subRes>((IEnumerable<subRes>) subResults.data) > 0)
      {
        foreach (subRes subRes in subResults.data)
        {
          Subtitle subtitle = null;
          try
          {
              subtitle = new Subtitle(subRes.IDSubtitleFile, subRes.MovieNameEng, subRes.SubFileName, subRes.SubLanguageID, subRes.IDMovieImdb);
          }
          catch
          {
              if (Languages.IsSupportedLanguageCode(subRes.SubLanguageID) == false)
              {
                  string lCode;
                  if (null != (lCode = Languages.FindLanguageCode(subRes.LanguageName)))
                  {
                      try
                      {
                          subtitle = new Subtitle(subRes.IDSubtitleFile, subRes.MovieNameEng, subRes.SubFileName, lCode, subRes.IDMovieImdb);
                      }
                      catch
                      {
                          continue;
                      }
                  }
                  else
                  {
                      continue;
                  }
              }
          }

          if (queryYear.HasValue)
          {
            if (subRes.MovieYear != null)
            {
              int num = (int) Convert.ToInt16(subRes.MovieYear);
              if (queryYear.Equals((object) num))
                list.Add(subtitle);
            }
            else
              list.Add(subtitle);
          }
          else
            list.Add(subtitle);
        }
      }
      return list;
    }

    private void CreateConnectionAndLogin()
    {
      this.openSubtitlesProxy = XmlRpcProxyGen.Create<IOpenSubtitlesProxy>();
      this.openSubtitlesProxy.Url = "http://api.opensubtitles.org/xml-rpc";
      this.openSubtitlesProxy.KeepAlive = false;
      this.token = this.openSubtitlesProxy.LogIn("", "", "en", this.UserAgent)[(object) "token"].ToString();
    }

    private string GetLanguageCodes(SubtitleSearchQuery query)
    {
      string str1 = "";
      foreach (string str2 in query.LanguageCodes)
        str1 = str1 + str2 + ",";
      if (query.LanguageCodes.Length > 0)
        str1 = str1.Remove(str1.Length - 1, 1);
      return str1;
    }
  }
}
