using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Google.API.Search;

namespace MediaPoint.Common.Helpers
{
    public class IMDb
    {
        public bool IsSeries { get; set; }
        public bool status { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public int SeriesSeason { get; set; }
        public int SeriesEpisode { get; set; }
        public string SeriesSubtitle { get; set; }
        public string DisplayTitle { get { return !string.IsNullOrEmpty(OriginalTitle) ? OriginalTitle : Title; } }
        public string DisplaySubTitle {
            get
            {
                if (SeriesSubtitle != null) return SeriesSubtitle;
                return !string.IsNullOrEmpty(OriginalTitle) ? (Title == OriginalTitle ? "" : Title) : ""; 
            }
        }
        public string OriginalTitle { get; set; }
        public string Year { get; set; }
        public string Rating { get; set; }
        public ArrayList Genres { get; set; }
        public string GenresString { get { return string.Join(", ", Genres.Cast<string>().Take(10).ToArray()); } }
        public ArrayList Directors { get; set; }
        public string DirectorsString { get { return string.Join(", ", Directors.Cast<string>().Take(10).ToArray()); } }
        public ArrayList Writers { get; set; }
        public ArrayList Cast { get; set; }
        public string CastString { get { return string.Join(", ", Cast.Cast<string>().Take(10).ToArray()); } }
        public ArrayList Producers { get; set; }
        public ArrayList Musicians { get; set; }
        public ArrayList Cinematographers { get; set; }
        public ArrayList Editors { get; set; }
        public string MpaaRating { get; set; }
        public string ReleaseDate { get; set; }
        public string Plot { get; set; }
        public ArrayList PlotKeywords { get; set; }
        public string Poster { get; set; }
        public string PosterLarge { get; set; }
        public string PosterFull { get; set; }
        public string Runtime { get; set; }
        public string Top250 { get; set; }
        public string Oscars { get; set; }
        public string Awards { get; set; }
        public string Nominations { get; set; }
        public string Storyline { get; set; }
        public string Tagline { get; set; }
        public string Votes { get; set; }
        public ArrayList Languages { get; set; }
        public ArrayList Countries { get; set; }
        public ArrayList ReleaseDates { get; set; }
        public ArrayList MediaImages { get; set; }
        public ArrayList RecommendedTitles { get; set; }
        public string ImdbURL { get; set; }

        public IMDb(int imdbId, bool GetExtraInfo = false)
        {
             parseIMDbPage(("http://www.imdb.com/title/tt" + imdbId).Replace("www.", "akas."), GetExtraInfo);
        }

        //Constructor
        public IMDb(string MovieName, string year = "", bool GetExtraInfo = true) : this(MovieName, 0, 0, year, GetExtraInfo)
        {
           
        }

        public IMDb(string MovieName, int season, int episode, string year = "", bool GetExtraInfo = true)
        {
            if (season != 0)
            {
                IsSeries = true;
                SeriesSeason = season;
                SeriesEpisode = episode;
            }
            int y;
            if (!int.TryParse(year, out y))
            {
                y = 0;
            }
            string imdbUrl = getIMDbUrl(MovieName, year, season, episode);
            status = false;
            if (!string.IsNullOrEmpty(imdbUrl))
            {
                parseIMDbPage(imdbUrl.Replace("www.", "akas."), GetExtraInfo);
            }
        }

        //Get IMDb URL from search results
        private string getIMDbUrl(string MovieName, string year, int season, int episode, string searchEngine = "google")
        {
            //string url = GoogleSearch + MovieName + (year != "" ? "+" + year : ""); //default to Google search
            //if (searchEngine.ToLower().Equals("bing")) url = BingSearch + MovieName + (year != "" ? "+" + year : "");
            //if (searchEngine.ToLower().Equals("ask")) url = AskSearch + MovieName + (year != "" ? "+" + year : "");
            //string html = getUrlData(url);
            //ArrayList imdbUrls = matchAll(@"(http://www.imdb.com/title/tt\d{7}/)", html);
            //if (imdbUrls.Count > 0)
            //    return (string)imdbUrls[0]; //return first IMDb result
            ////else if (searchEngine.ToLower().Equals("google")) //if Google search fails
            ////    return getIMDbUrl(MovieName, "bing"); //search using Bing
            ////else if (searchEngine.ToLower().Equals("bing")) //if Bing search fails
            ////    return getIMDbUrl(MovieName, "ask"); //search using Ask
            ////else //search fails
             
            Random r = new Random();

            GwebSearchClient client = null;
            string term = null;

            if (season != 0)
            {
                var termFormats = new string[] { "imdb tv episode {0}", "tv episode imdb {0}", "imdb {0} tv episode", "{0} imdb tv episode", "tv episode {0} imdb" };
                string movieWithSep = MovieName + " season " + season + " episode " + episode + " " + year;
                term = string.Format(termFormats[r.Next(termFormats.Length)], movieWithSep);
                client = new GwebSearchClient("http://www.imdb.com");
            }
            else
            {
                var termFormats = new string[] { "imdb title {0}", "title imdb {0}", "imdb {0} title", "{0} imdb title", "title {0} imdb" };
                term = string.Format(termFormats[r.Next(termFormats.Length)], MovieName + " " + year);
                client = new GwebSearchClient("http://www.imdb.com");
            }

            IList<IWebResult> results;
            try
            {
                int limit = 1;
                if (season != 0) limit = 20;
                results = client.Search(term, limit);
                if (results.Count == 0)
                    return string.Empty;
            }
            catch
            {
                return String.Empty;
            }

            if (season != 0)
            {
                var match = results.FirstOrDefault(m => m.Content.ToLowerInvariant().Contains("season " + season + ": episode " + episode));
                if (match != null)
                    return match.Url;
            }

            return results[0].Url;
        }

        //Parse IMDb page data
        private void parseIMDbPage(string imdbUrl, bool GetExtraInfo)
        {
            string html = getUrlData(imdbUrl + "combined");
            Id = match(@"<link rel=""canonical"" href=""http://www.imdb.com/title/(tt\d{7})/combined"" />", html);
            if (!string.IsNullOrEmpty(Id))
            {
                status = true;
                //Title = match(@"<title>(IMDb \- )*(.*?) \(.*?</title>", html, 2);
                Title = match(@"tn15title\""><h1>(.*?)<", html); //match(@"<title>(IMDb \- )*(.*?) \([^)]*?(\d{4}).*?\)?.*?</title>", html, 2);

                OriginalTitle = match(@"title-extra"">(.*?)<", html);

                if (IsSeries)
                {
                    string episodeName = match(@"tn15title\""><h1>[^<]*?<span><em>(.*?)<", html);
                    string episodeExtra = match(@"tn15title\""><h1>[^<]*?<span><em>[^<]*?</em>(.*?)<", html);
                    SeriesSubtitle = episodeName + " " + episodeExtra + "\r\n" + "Season " + SeriesSeason + ", Episode " + SeriesEpisode;
                }
                else
                {
                    try
                    {
                        var season = match(@"\(Season (\d*), Episode \d*\)", html);
                        var episode = match(@"\(Season \d*, Episode (\d*)\)", html);
                        string episodeName = match(@"tn15title\""><h1>[^<]*?<span><em>(.*?)<", html);
                        string episodeExtra = match(@"tn15title\""><h1>[^<]*?<span><em>[^<]*?</em>(.*?)<", html);
                        IsSeries = !string.IsNullOrEmpty(season);
                        if (IsSeries)
                        {
                            SeriesSeason = Convert.ToInt32(season);
                            SeriesEpisode = Convert.ToInt32(episode);
                            SeriesSubtitle = episodeName + " " + episodeExtra + "\r\n" + "Season " + SeriesSeason + ", Episode " + SeriesEpisode;
                        }
                        else
                        {
                            SeriesSeason = 0;
                            SeriesEpisode = 0;
                            SeriesSubtitle = "";
                        }
                    }
                    catch
                    {
                        IsSeries = false;
                    }
                }

                if (OriginalTitle == "")
                {
                    var eng = match(@">\s*\""([^<]*)\"" -.*?International <em>\(English title\)", html, 1);
                    OriginalTitle = eng;
                }
                //Year = match(@"<title>.*?\(.*?(\d{4}).*?\).*?</title>", html);
                Year = match(@"<title>(IMDb \- )*(.*?) \([^)]*?(\d{4}).*?\)?.*?</title>", html, 3);
                Rating = match(@"<b>(\d.\d)/10</b>", html);
                Genres = matchAll(@"<a.*?>(.*?)</a>", match(@"Genre.?:(.*?)(</div>|See more)", html));
                Directors = matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", match(@"Directed by</a></h5>(.*?)</table>", html));
                Writers = matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", match(@"Writing credits</a></h5>(.*?)</table>", html));
                Producers = matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", match(@"Produced by</a></h5>(.*?)</table>", html));
                Musicians = matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", match(@"Original Music by</a></h5>(.*?)</table>", html));
                Cinematographers = matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", match(@"Cinematography by</a></h5>(.*?)</table>", html));
                Editors = matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", match(@"Film Editing by</a></h5>(.*?)</table>", html));
                Cast = matchAll(@"<td class=""nm""><a.*?href=""/name/.*?/"".*?>(.*?)</a>", match(@"<h3>Cast</h3>(.*?)</table>", html));
                Plot = match(@"Plot:</h5>.*?<div class=""info-content"">(.*?)(<a|</div)", html);
                PlotKeywords = matchAll(@"<a.*?>(.*?)</a>", match(@"Plot Keywords:</h5>.*?<div class=""info-content"">(.*?)</div", html));
                ReleaseDate = match(@"Release Date:</h5>.*?<div class=""info-content"">.*?(\d{1,2} (January|February|March|April|May|June|July|August|September|October|November|December) (19|20)\d{2})", html);
                Runtime = match(@"Runtime:</h5><div class=""info-content"">(\d{1,4}) min[\s]*.*?</div>", html);
                Top250 = match(@"Top 250: #(\d{1,3})<", html);
                Oscars = match(@"Won (\d+) Oscars?\.", html);
                if (string.IsNullOrEmpty(Oscars) && "Won Oscar.".Equals(match(@"(Won Oscar\.)", html))) Oscars = "1";
                Awards = match(@"(\d{1,4}) wins", html);
                Nominations = match(@"(\d{1,4}) nominations", html);
                Tagline = match(@"Tagline:</h5>.*?<div class=""info-content"">(.*?)(<a|</div)", html);
                MpaaRating = match(@"MPAA</a>:</h5><div class=""info-content"">Rated (G|PG|PG-13|PG-14|R|NC-17|X) ", html);
                Votes = match(@">(\d+,?\d*) votes<", html);
                Languages = matchAll(@"<a.*?>(.*?)</a>", match(@"Language.?:(.*?)(</div>|>.?and )", html));
                Countries = matchAll(@"<a.*?>(.*?)</a>", match(@"Country:(.*?)(</div>|>.?and )", html));
                Poster = match(@"<div class=""photo"">.*?<a name=""poster"".*?><img.*?src=""(.*?)"".*?</div>", html);
                if (!string.IsNullOrEmpty(Poster) && Poster.IndexOf("media-imdb.com") > 0)
                {
                    Poster = Regex.Replace(Poster, @"_V1.*?.jpg", "_V1._SY200.jpg");
                    PosterLarge = Regex.Replace(Poster, @"_V1.*?.jpg", "_V1._SY500.jpg");
                    PosterFull = Regex.Replace(Poster, @"_V1.*?.jpg", "_V1._SY0.jpg");
                }
                else
                {
                    Poster = string.Empty;
                    PosterLarge = string.Empty;
                    PosterFull = string.Empty;
                }
                ImdbURL = "http://www.imdb.com/title/" + Id + "/";
                if (GetExtraInfo)
                {
                    string plotHtml = getUrlData(imdbUrl + "plotsummary");
                    Storyline = match(@"<p class=""plotpar"">(.*?)(<i>|</p>)", plotHtml);
                    ReleaseDates = getReleaseDates();
                    MediaImages = getMediaImages();
                    RecommendedTitles = getRecommendedTitles();
                }
            }

        }

        //Get all release dates
        private ArrayList getReleaseDates()
        {
            ArrayList list = new ArrayList();
            string releasehtml = getUrlData("http://www.imdb.com/title/" + Id + "/releaseinfo");
            foreach (string r in matchAll(@"<tr>(.*?)</tr>", match(@"Date</th></tr>\n*?(.*?)</table>", releasehtml)))
            {
                Match rd = new Regex(@"<td>(.*?)</td>\n*?.*?<td align=""right"">(.*?)</td>", RegexOptions.Multiline).Match(r);
                list.Add(StripHTML(rd.Groups[1].Value.Trim()) + " = " + StripHTML(rd.Groups[2].Value.Trim()));
            }
            return list;
        }

        //Get all media images
        private ArrayList getMediaImages()
        {
            ArrayList list = new ArrayList();
            string mediaurl = "http://www.imdb.com/title/" + Id + "/mediaindex";
            string mediahtml = getUrlData(mediaurl);
            int pagecount = matchAll(@"<a href=""\?page=(.*?)"">", match(@"<span style=""padding: 0 1em;"">(.*?)</span>", mediahtml)).Count;
            for (int p = 1; p <= pagecount + 1; p++)
            {
                mediahtml = getUrlData(mediaurl + "?page=" + p);
                foreach (Match m in new Regex(@"src=""(.*?)""", RegexOptions.Multiline).Matches(match(@"<div class=""thumb_list"" style=""font-size: 0px;"">(.*?)</div>", mediahtml)))
                {
                    String image = m.Groups[1].Value;
                    list.Add(Regex.Replace(image, @"_V1\..*?.jpg", "_V1._SY0.jpg"));
                }
            }
            return list;
        }

        //Get Recommended Titles
        private ArrayList getRecommendedTitles()
        {
            ArrayList list = new ArrayList();
            string recUrl = "http://www.imdb.com/widget/recommendations/_ajax/get_more_recs?specs=p13nsims%3A" + Id;
            string json = getUrlData(recUrl);
            list = matchAll(@"title=\\""(.*?)\\""", json);
            HashSet<String> set = new HashSet<string>();
            foreach (String rec in list) set.Add(rec);
            return new ArrayList(set.ToList());
        }

        /*******************************[ Helper Methods ]********************************/

        //Match single instance
        private string match(string regex, string html, int i = 1)
        {
            return HttpUtility.HtmlDecode(new Regex(regex, RegexOptions.Multiline).Match(html).Groups[i].Value.Trim().TrimEnd(' ', '|'));
        }

        //Match all instances and return as ArrayList
        private ArrayList matchAll(string regex, string html, int i = 1)
        {
            ArrayList list = new ArrayList();
            foreach (Match m in new Regex(regex, RegexOptions.Multiline).Matches(html))
                list.Add(HttpUtility.HtmlDecode(m.Groups[i].Value.Trim()));
            return list;
        }

        //Strip HTML Tags
        static string StripHTML(string inputString)
        {
            return Regex.Replace(inputString, @"<.*?>", string.Empty);
        }

        //Get URL Data
        private string getUrlData(string url)
        {
            int tries = 1;
            WebResponse response;
            s1:
            //WebClient client = new WebClient();
            WebRequest request = WebRequest.Create(url);
            Random r = new Random();
            request.Headers.Add("X-Forwarded-For", r.Next(0, 255) + "." + r.Next(0, 255) + "." + r.Next(0, 255) + "." + r.Next(0, 255));
            //Random IP Address
            //client.Headers["X-Forwarded-For"] = r.Next(0, 255) + "." + r.Next(0, 255) + "." + r.Next(0, 255) + "." + r.Next(0, 255);
            //Random User-Agent
            ((HttpWebRequest)request).UserAgent = string.Format("Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.2.{0}) Gecko/2010080{0} Firefox/3.6", r.Next(0, 8));
            //((HttpWebRequest)request).UserAgent = "Mozilla/3.0 (Windows NT " + r.Next(3, 5) + "." + r.Next(0, 2) + "; rv:2.0.1) Gecko/20100101 Firefox/" + r.Next(3, 5) + "." + r.Next(0, 5) + "." + r.Next(0, 5);
            //client.Headers["User-Agent"] = "Mozilla/" + r.Next(3, 5) + ".0 (Windows NT " + r.Next(3, 5) + "." + r.Next(0, 2) + "; rv:2.0.1) Gecko/20100101 Firefox/" + r.Next(3, 5) + "." + r.Next(0, 5) + "." + r.Next(0, 5);
            try
            {
                response = request.GetResponse();
            }
            catch
            {
                tries++;
                if (tries < 4)
                {
                    Thread.Sleep(1000);
                    goto s1;
                }
                return "";
            }
            Stream datastream = response.GetResponseStream(); //client.OpenRead(url);
            StreamReader reader = new StreamReader(datastream);
            StringBuilder sb = new StringBuilder();
            while (!reader.EndOfStream)
                sb.Append(reader.ReadLine());
            response.Close();
            datastream.Close();
            return sb.ToString();
        }
    }
}
