using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MediaPoint.Common.Helpers;
using MediaPoint.Helpers;
using SubtitleDownloader.Core;
using MediaPoint.Common.Extensions;
using System.Net;
using System.Threading;
using MediaPoint.MVVM.Services;
using MediaPoint.VM.Services.Model;

namespace MediaPoint.Common.Subtitles
{
    public class SubtitleMatch
    {
        public string MatchRelease { get; set; }
        public string MatchedBy { get; set; }
        public string Language { get; set; }
        public Subtitle Subtitle { get; set; }
        public double Score { get; set; }
        public string[] Releases { get; set; }
        public string Service { get; set; }
    }

    public class SubtitleUtil
    {
        public static string DownloadSubtitle(string fileName, string[] preferredLanguages, string[] preferredServices, out IMDb imdbMatch, out List<SubtitleMatch> otherChoices, Action<string> messageCallback)
        {
            var fn = Path.GetFileName(fileName);
            otherChoices = new List<SubtitleMatch>();
            List<SubtitleMatch> dummyChoices;
                
            SubtitleMatch subtitle = null;

            if (ServiceLocator.GetService<ISettings>().PreferenceToHashMatchedSubtitle)
            {
                subtitle = FindSubtitleForFilename(fileName, Path.GetDirectoryName(fileName), preferredLanguages, preferredServices, out imdbMatch, out dummyChoices, messageCallback, false, true, true);

                if (subtitle != null)
                {
                    return DownloadSubtitle(subtitle, fileName);
                }
            }

            subtitle = FindSubtitleForFilename(fn, Path.GetDirectoryName(fileName), preferredLanguages, preferredServices, out imdbMatch, out dummyChoices, messageCallback, false, true, false);

            if (subtitle != null)
            {
                return DownloadSubtitle(subtitle, fileName);
            }

            return null;
        }

        static object _fileLocker = new object();
        public static string DownloadSubtitle(SubtitleMatch subtitle, string fileName)
        {
            string resultFile = null;

            
            if (subtitle != null)
            {
                var dir = Path.GetDirectoryName(fileName);
                if (dir != null && Directory.Exists(dir))
                {
                    var files = SubtitleDownloaderFactory.GetSubtitleDownloader(subtitle.Service).SaveSubtitle(subtitle.Subtitle);
                    string tmpDir = files[0].Directory.FullName;

                    if (files.Count == 1)
                    {
                        string subNewName = Path.Combine(dir,
                                                         string.Format("{0}_{1}{2}", Path.GetFileNameWithoutExtension(fileName),
                                                                       subtitle.Subtitle.LanguageCode,Path.GetExtension(files[0].FullName)));
                        lock (_fileLocker)
                        {
                            if (File.Exists(subNewName))
                            {
                                File.Delete(subNewName);
                            }
                            resultFile = subNewName;
                            try
                            {
                                files[0].MoveTo(subNewName);
                            }
                            catch
                            {
                                // read only folder
                                resultFile = files[0].FullName;
                            }
                        }
                    }
                    else
                    {
                        var cd = new Regex(@"cd(\d+)", RegexOptions.IgnoreCase);
                        int cdIndex = 0;
                        bool isMultiVolume = false;

                        if (cd.IsMatch(Path.GetFileNameWithoutExtension(fileName)))
                        {
                            // multivolume video : find which CD is this
                            cdIndex = int.Parse(cd.Match(Path.GetFileNameWithoutExtension(fileName)).Groups[0].Value);
                            isMultiVolume = true;
                        }

                        string subNewName = Path.Combine(dir,
                                                         isMultiVolume
                                                             ? string.Format("{0}_CD{1}_{2}{3}",
                                                                             Path.GetFileNameWithoutExtension(fileName),
                                                                             cdIndex, subtitle.Subtitle.LanguageCode,
                                                                             Path.GetExtension(files[cdIndex].FullName))
                                                             : string.Format("{0}_{1}{2}",
                                                                             Path.GetFileNameWithoutExtension(fileName),
                                                                             subtitle.Subtitle.LanguageCode,
                                                                             Path.GetExtension(files[cdIndex].FullName)));

                        if (File.Exists(subNewName))
                        {
                            File.Delete(subNewName);
                        }
                        try
                        {
                            files[cdIndex].MoveTo(subNewName);
                            resultFile = files[cdIndex].FullName;
                        }
                        catch
                        {
                            // read only folder
                            resultFile = files[cdIndex].FullName;
                        }
                    }

                    if (Path.GetTempPath().ToLowerInvariant() != tmpDir.ToLowerInvariant() &&
                        tmpDir.ToLowerInvariant().StartsWith(Path.GetTempPath().ToLowerInvariant()))
                    {
                        if (File.Exists(tmpDir))
                        {
                            File.Delete(tmpDir);
                        }
                        else if (Directory.Exists(tmpDir))
                        {
                            Directory.Delete(tmpDir, true);
                        }
                    }
                }
            }

            return resultFile;
        }

        public static IMDb GetIMDbFromFilename(string filename, string folderName, out string strTitle, out string strTitleAndYear, out string strYear, out int season, out int episode, Action<string> messageCallback, out Subtitle bestGuessSubtitle)
        {
            if (messageCallback != null) messageCallback("Matching media with IMDb ...");

            bestGuessSubtitle = null;

            // extract meaningful data from the filename
            var imd = GetMovieMetadata(
                filename,
                out strTitle,
                out strTitleAndYear,
                out strYear,
                out season,
                out episode, out bestGuessSubtitle, true, true);

            if (imd != null) return imd;

            if (strTitle != "" && strYear == "")
            {
                int tmpSeason, tmpEpisode;
                // no year, maybe we find one in the folder
                string tmpTitle;
                string tmpTitleAndYear;
                GetMovieMetadata(Path.GetDirectoryName(filename) + ".dummyextension",
                    out tmpTitle,
                    out tmpTitleAndYear,
                    out strYear,
                    out tmpSeason,
                    out tmpEpisode, out bestGuessSubtitle, true, false);

                if (tmpSeason != 0)
                {
                    // found season and episode in folder
                    season = tmpSeason;
                    episode = tmpEpisode;
                }

                if (strYear != "")
                    strTitleAndYear = string.Format("{0} ({1})", strTitle, strYear);
            }

            // cleanup multiple spaces
            strTitle = new Regex(@"\s+").Replace(strTitle, " ");

            IMDb imdb = null;

            if (!string.IsNullOrEmpty(strTitle))
            {
                // scrape imdb to find movie metadata
                if (season != 0)
                {
                    imdb = new IMDb(strTitle, season, episode, strYear, false);
                }
                else
                {
                    imdb = new IMDb(strTitle, strYear, false);
                }
            }

            if (imdb == null || imdb.status == false)
            {
                // if failed try with folder name
                GetMovieMetadata(
                    folderName + ".dummyextension",
                    out strTitle,
                    out strTitleAndYear,
                    out strYear,
                    out season,
                    out episode, out bestGuessSubtitle, true, false);

                // cleanup multiple spaces
                strTitle = new Regex(@"\s+").Replace(strTitle, " ");

                imdb = new IMDb(strTitle, strYear, false);
            }

            if (imdb == null || imdb.status == false)
            {
                // we need imdb match to do something
                return null;
            }

            char[] punctuation = new char[] { ',', '.', '-', ':', '"', ';', '\'', '(', ')', '[', ']' };
            string imdbTitle = new string(imdb.DisplayTitle.TrimEnd('.', ',', ' ').ToLowerInvariant().Replace(punctuation, ' ').ToArray());
            imdbTitle = new Regex(@"\s+").Replace(imdbTitle, " ");
            
            string fileTitle = new string(strTitle.Trim('.', ',', ' ').ToLowerInvariant().Replace(punctuation, ' ').ToArray());
            fileTitle = new Regex(@"\s+").Replace(fileTitle, " ");

            string imdbOrigTitle = new string(imdb.DisplaySubTitle.TrimEnd('.', ',', ' ').Replace(punctuation, ' ').ToArray());
            imdbOrigTitle = new Regex(@"\s+").Replace(imdbOrigTitle, " ");

            if (WordMatches(fileTitle, imdbOrigTitle) >= 0.3 || WordMatches(fileTitle, imdbTitle) >= 0.3)
            {
                return imdb;
            }
            else
            {
                if ((double)Levenshtein.Compare(fileTitle, imdbTitle) / Math.Max(fileTitle.Length, imdbTitle.Length) < 0.20)
                {
                    return imdb;
                }
                return null;
            }

        }

        public static SubtitleMatch FindSubtitleForFilename(string filename, string folderName, string[] preferredLanguages, string[] preferredServices, out IMDb imdbMatch, out List<SubtitleMatch> otherChoices, Action<string> messageCallback, bool noFiltering = false, bool needsImdb = true, bool allowBestGuess = false)
        {
            int season, episode;
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()) + @"\.\_";
            string strTitle, strYear, strTitleAndYear;
            
            Subtitle bestGuessSubtitle = null;
            imdbMatch = GetIMDbFromFilename(filename, folderName, out strTitle, out strTitleAndYear, out strYear, out season, out episode, messageCallback, out bestGuessSubtitle);

            if (bestGuessSubtitle != null && allowBestGuess)
            {
                otherChoices = null;
                return new SubtitleMatch
                {
                    Subtitle = bestGuessSubtitle,
                    Language = bestGuessSubtitle.LanguageCode,
                    MatchedBy = "Hasher",
                    MatchRelease = "",
                    Releases = new string [] {},
                    Score = 1,
                    Service = "OpenSubtitles"
                };
            }

            filename = Path.GetFileName(filename);
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            filename = r.Replace(filename, " ");

            filename = Path.GetFileName(filename);
            string sep1 = "";

            if (season != 0)
            {
                sep1 = string.Format("s{0:00}e{1:00}", season, episode);
            }

            if (sep1 != "") needsImdb = false;

            if (imdbMatch == null && needsImdb)
            {
                imdbMatch = null;
                otherChoices = null;
                return null;
            }

            string[] patterns = new string[imdbMatch == null ? 1 : string.IsNullOrEmpty(imdbMatch.OriginalTitle) || string.IsNullOrEmpty(imdbMatch.Title) ? 2 : 3];
            patterns[0] = filename;
            if (patterns.Length > 1) patterns[1] = imdbMatch.Title.TrimStart('\\', '\"').TrimEnd('\\', '\"');
            if (patterns.Length > 2) patterns[2] = imdbMatch.OriginalTitle.TrimStart('\\', '\"').TrimEnd('\\', '\"');
            if (imdbMatch != null) for (int i = 1; i < patterns.Length; i++) patterns[i] += string.Format(" ({0})", imdbMatch.Year);

            Array.Reverse(patterns); // we like imdb first sicne we might skip searching further on some matches

            var ret = GetOrderedSubsMatches(preferredServices, preferredLanguages, patterns, messageCallback, noFiltering);

            var settings = ServiceLocator.GetService<ISettings>();
            double minScore = settings != null ? settings.SubtitleMinScore : 0.55;

            ret.RemoveAll(m => m.Score <= minScore); // crap

            if (!needsImdb && imdbMatch == null)
            {
                ret.RemoveAll(m => WordMatches(m.MatchRelease, patterns[0]) <= 0.2 || (sep1 != "" && FindSeasonAndEpisode(m.MatchRelease) != sep1)); // even more crap if no imdb match
            }
            
            otherChoices = ret;
            
            var ret2 = ret.Select(s => new { priority = preferredLanguages.ToList().IndexOf(s.Language), Sub = s }).OrderByDescending(s => preferredLanguages.Length - s.priority).FirstOrDefault();

            return ret2 == null ? null : ret2.Sub;
        }

        public static string FindSeasonAndEpisode(string text)
        {
            Regex regex = new Regex(@"(?<seasonandepisode>S(?<season>\d{1,2})E(?<episode>\d{1,2}))", RegexOptions.IgnoreCase);

            Match match = regex.Match(text);
            if (match.Success)
            {
                string sep = match.Groups["seasonandepisode"].Value;
                return sep.ToLowerInvariant();
            }

            regex = new Regex(@"(?<seasonandepisode>S(?<season>\d{1,2}).E(?<episode>\d{1,2}))", RegexOptions.IgnoreCase);

            match = regex.Match(text);
            if (match.Success)
            {
                string sep = "S" + match.Groups["season"].Value + "E" + match.Groups["episode"].Value;
                return sep.ToLowerInvariant();
            }

            regex = new Regex(@"(?<seasonandepisode>(?<season>\d{1,2})x(?<episode>\d{2}))", RegexOptions.IgnoreCase);

            match = regex.Match(text);
            if (match.Success)
            {
                string sep = "S" + match.Groups["season"].Value + "E" + match.Groups["episode"].Value;
                return sep.ToLowerInvariant();
            }
            return "";
        }

        public static List<SubtitleMatch> GetOrderedSubsMatches(string[] services, string[] languages, string[] patterns, Action<string> messageCallback, bool noFiltering = false)
        {
            List<SubtitleMatch> scores = new List<SubtitleMatch>();
            List<Subtitle> subtitles = new List<Subtitle>();
            
            foreach (var service in services)
            {
                
                var d = SubtitleDownloaderFactory.GetSubtitleDownloader(service);
                List<Subtitle> foundSubtitles = new List<Subtitle>();

                for (int i = 0; i < patterns.Length; i++)
                {
                    if (messageCallback != null) messageCallback(string.Format("Searching {0} {1}", service, "".PadLeft(i+3, '.') ));

                    string pat = patterns[i].Replace('.', ' ');
                    string title, titleYear, strYear;
                    int season, episode;
                    Subtitle bestGuess;

                    GetMovieMetadata(pat, out title, out titleYear, out strYear, out season, out episode, out bestGuess, false);

                    SearchQuery q;

                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(strYear))
                    {
                        q = new SearchQuery(title);
                        q.Year = Convert.ToInt32(strYear);
                    }
                    else
                    {
                        q = new SearchQuery(pat);
                    }

                    q.LanguageCodes = languages;
                    var subs = d.SearchSubtitles(q);

                    if (subs.Count > 0)
                    {
                        foundSubtitles.AddRange(subs);
                        //if (!noFiltering) break;
                        if (foundSubtitles.GroupBy(s => s.LanguageCode).Max(s => s.Count()) > 10) break; // 10 per service per language should be enough
                    }
                }

                if (foundSubtitles.Count > 0)
                {
                    var newScores = RateSubs(foundSubtitles, patterns, service == "Podnapisi").Where(m => m.Score > 0).OrderBy(m => m.Score).Reverse().ToList();
                    foreach (var score in newScores)
                        score.Service = service;

                    scores.AddRange(newScores);

                    if (newScores.Any() && newScores.First().Score > 1.2 && !noFiltering)
                        break;
                }
            }

            PriritizebyMedium(scores, patterns);
            PriritizebyLanguage(scores, patterns);

            var serviceList = services.ToList();
            return scores.Distinct(new SubEquality()).OrderBy(m => ((int)(m.Score * 100))).ThenBy(m => GetLanguagePriority(languages, m.Language)).ThenBy(m => serviceList.Count - serviceList.IndexOf(m.Service)).Reverse().ToList();
        }

        class SubEquality : EqualityComparer<SubtitleMatch>
        {
            public override bool Equals(SubtitleMatch x, SubtitleMatch y)
            {
                return x.Service == y.Service &&
                    x.Language == y.Language &&
                    x.MatchRelease == y.MatchRelease;
            }

            public override int GetHashCode(SubtitleMatch obj)
            {
                return (obj.Service + obj.Language + "#" + obj.MatchRelease).GetHashCode();
            }
        }

        static int GetLanguagePriority(string[] languages, string language)
        {
            var ret = languages.ToList().IndexOf(language);

            if (ret != -1)
            {
                ret = languages.Length - ret;
            }

            return ret;
        }

        static void PriritizebyLanguage(IEnumerable<SubtitleMatch> matches, string[] languages)
        {
            var lLanguages = new List<string>(languages);

            foreach (var match in matches)
            {
                double multiplier = 1 + (lLanguages.Count - 1 - lLanguages.IndexOf(match.Language))/100;
                match.Score *= multiplier;
            }
        }

        static void PriritizebyMedium(IEnumerable<SubtitleMatch> matches, string[] patterns)
        {
            bool dvd2 = patterns.Any(p => p.ToLowerInvariant().Contains("dvd"));
            bool bd2 = patterns.Any(p => p.ToLowerInvariant().Contains("bdrip") ||
                      p.ToLowerInvariant().Contains("brrip") ||
                      p.ToLowerInvariant().Contains("bluray") ||
                      p.ToLowerInvariant().Contains("720p") ||
                      p.ToLowerInvariant().Contains("1080p"));

            foreach (var match in matches)
            {
                bool dvd1 = match.MatchRelease.ToLowerInvariant().Contains("dvd");
                bool bd1 = match.MatchRelease.ToLowerInvariant().Contains("bdrip") ||
                          match.MatchRelease.ToLowerInvariant().Contains("brrip") ||
                          match.MatchRelease.ToLowerInvariant().Contains("bluray") ||
                          match.MatchRelease.ToLowerInvariant().Contains("720p") ||
                          match.MatchRelease.ToLowerInvariant().Contains("1080p");

                if (((dvd1 == true && dvd1 == dvd2) || (bd1 == true && bd1 == bd2)) && !(((dvd1 == true) && (bd2 == true)) || ((dvd2 == true) && (bd1 == true))))
                {
                    match.Score *= 1.1; // match subtitle and pattern
                }
                else if (((dvd1 == true) && (bd2 == true)) || ((dvd2 == true) && (bd1 == true)))
                {
                    match.Score *= 0.9; // mismatch sub and pattern
                }
                else if (bd1)
                {
                    match.Score *= 1.01; // we prefer bluray subs if we dont know the medium... it is 2014... dvds are extinct
                }
            }
        }

        private static IEnumerable<SubtitleMatch> RateSubs(IEnumerable<Subtitle> subs, string[] patterns, bool podnapisi)
        {
            int season;
            int episode;
            Subtitle bestGuessSubtitle = null;
            foreach (var subtitle in subs)
            {
                string subFileName = subtitle.FileName;
                if (podnapisi && subtitle.FileName.Length > 15)
                {
                    subFileName = subtitle.FileName.RemoveSpecialCharacters();
                    string[] words = subFileName.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    bool fromReleases = false;
                    if (words.Length > 1)
                    {
                        if (words.Skip(1).Any(w => w == words[0]))
                        {
                            List<string> cuts = new List<string>();
                            int lastCut = 0;
                            int cut = subFileName.IndexOf(words[0], subFileName.IndexOf(words[1]));
                            while (cut != -1)
                            {
                                cuts.Add(subFileName.Substring(lastCut, cut - lastCut));
                                lastCut = cut;
                                if (lastCut + 1 >= subFileName.Length) break;
                                cut = subFileName.IndexOf(words[0], lastCut+1);
                            }

                            if (cuts.Count > 1)
                            {
                                fromReleases = true;

                                subFileName = string.Join(" ", cuts.Select(c => c.Trim().Replace(' ', '.')));
                            }
                        }
                    }
                    if (!fromReleases)
                    {
                        subFileName = subFileName.Trim();
                        subFileName = subFileName.Replace(' ', '.');
                    }
                }
                var files = podnapisi ? subFileName.Split(new string[] {" "}, StringSplitOptions.RemoveEmptyEntries) : new string[] { Path.GetFileNameWithoutExtension(subFileName) };
                double best = 0;
                string bestFile = "";
                string bestPattern = "";
                foreach (var file in files)
                {
                    if (file.Length < 2)
                    {
                        continue;
                    }
                    string title1, year1, titleAndYear1;

                    GetMovieMetadata(file, out title1, out titleAndYear1, out year1, out season, out episode, out bestGuessSubtitle, true, false);

                    if (string.IsNullOrEmpty(year1))
                    {
                        var yearmatch = Regex.Match(file, @"\d{4}");
                        if (yearmatch.Success)
                        {
                            year1 = yearmatch.Value;
                        }
                    }

                    string movieEpisode = "";
                    foreach (var pattern in patterns)
                    {
                        var tmpEpisode = FindSeasonAndEpisode(pattern);
                        if (tmpEpisode != "")
                        {
                            movieEpisode = tmpEpisode;
                        }
                    }

                    foreach (var pattern in patterns)
                    {
                        string title2, year2, titleAndYear2;

                        GetMovieMetadata(pattern, out title2, out titleAndYear2, out year2, out season, out episode, out bestGuessSubtitle, true, false);

                        double m = WordMatches(year2 == "" ? title1 : titleAndYear1, year1 == "" ? title2 : titleAndYear2);

                        if (!string.IsNullOrEmpty(year1) && year1 == year2)
                            m *= 1.1;
                        if (string.IsNullOrEmpty(year1) && !string.IsNullOrEmpty(year2))
                            m *= 0.9;
                        if (!string.IsNullOrEmpty(year1) && !string.IsNullOrEmpty(year2) && year1 != year2)
                            m *= 0.6;

                        if (Levenshtein.Compare(file, pattern) < pattern.Length / 10)
                        {
                            m += 0.05;
                        }

                        if (patterns.Any(p => Regex.IsMatch(p.ToLowerInvariant(), @"cd\d") == false &&
                            Regex.IsMatch(file.ToLowerInvariant(), @"cd\d") == true))
                        {
                            // movie not on multi cd
                            m -= 0.3;
                        }

                        if (movieEpisode != "" && 
                            FindSeasonAndEpisode(file) != "")
                        {
                            if (movieEpisode.ToLowerInvariant() == FindSeasonAndEpisode(file).ToLowerInvariant())
                            {
                                m *= 1.1;
                            }
                            else
                            {
                                m *= 0;
                            }
                        }

                        if (best < m)
                        {
                            best = m;
                            bestFile = file;
                            bestPattern = pattern;
                        }
                    }
                }
                yield return new SubtitleMatch() { Subtitle = subtitle, Score = best, Releases = files, Language = subtitle.LanguageCode, MatchRelease = bestFile, MatchedBy = bestPattern };
            }
        }

        public static double WordMatches(string s1, string s2)
        {
            s1 = s1.ToLowerInvariant();
            s2 = s2.ToLowerInvariant();

            s1 = Regex.Replace(s1, @"[^\w\s]|_", " ");
            s1 = Regex.Replace(s1, @"\s+", " ");

            s2 = Regex.Replace(s2, @"[^\w\s]|_", " ");
            s2 = Regex.Replace(s2, @"\s+", " ");


            var w1 = s1.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            var w2 = s2.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            var found = w1.Intersect(w2).ToArray();
            int longest = Math.Max(w1.Length, w2.Length);
            return (double)found.Length / longest;
        }

        public static IMDb GetMovieMetadata(string strFileName, out string strTitle, out string strTitleAndYear, out string strYear, out int season, out int episode, out Subtitle bestSubtitleGuess, bool bCleanChars = true, bool tryHash = false)
        {
            season = 0;
            episode = 0;
            strTitle = "";
            strYear = "";
            bestSubtitleGuess = null;

            string sep1 = FindSeasonAndEpisode(strFileName);

            if (sep1 != "")
            {
                var sep = sep1.ToLowerInvariant().Split('s', 'e');
                if (sep.Length == 3)
                {
                    season = int.Parse(sep[1]);
                    episode = int.Parse(sep[2]);
                }
            }

            string originalFilename = strFileName;

            if (string.IsNullOrEmpty(Path.GetDirectoryName(strFileName)) == false && strFileName.ToLowerInvariant().Contains(Path.GetDirectoryName(strFileName).ToLowerInvariant()))
            {
                strFileName = Path.GetFileNameWithoutExtension(strFileName);
            }

            const string videoCleanDateTimeRegExp = "(.*[^ _\\,\\.\\(\\)\\[\\]\\-])[ _\\.\\(\\)\\[\\]\\-]+(19[0-9][0-9]|20[0-1][0-9])([ _\\,\\.\\(\\)\\[\\]\\-]|[^0-9]$)";

            var videoCleanStringRegExps = new List<string>();
            videoCleanStringRegExps.Add("[ _\\,\\.\\(\\)\\[\\]\\-](ac3|dts|custom|dc|remastered|divx|divx5|dsr|dsrip|dutch|dvd|dvd5|dvd9|dvdrip|dvdscr|dvdscreener|screener|dvdivx|cam|fragment|fs|hdtv|hdrip|hdtvrip|internal|limited|multisubs|ntsc|ogg|ogm|pal|pdtv|proper|repack|rerip|retail|r3|r5|bd5|se|svcd|swedish|german|read.nfo|nfofix|unrated|extended|ws|telesync|ts|telecine|tc|brrip|bdrip|480p|480i|576p|576i|720p|720i|1080p|1080i|3d|hrhd|hrhdtv|hddvd|bluray|x264|h264|xvid|xvidvd|xxx|www.www|cd[1-9]|\\[.*\\])([ _\\,\\.\\(\\)\\[\\]\\-]|$)");
            videoCleanStringRegExps.Add("(\\[.*\\])");
            videoCleanStringRegExps.Add("(-.*$)");

            string stringPrefixClean = ("^(ac3|dts|remastered|divx|divx5|dsr|dsrip|dvd|dvd5|dvd9|dvdrip|dvdscr|dvdscreener|screener|dvdivx|hdtv|hdrip|hdtvrip|internal|limited|multisubs|ntsc|ogg|ogm|pal|pdtv|repack|rerip|retail|r3|r5|bd5|se|svcd|read.nfo|nfofix|unrated|ws|telesync|ts|telecine|tc|brrip|bdrip|480p|480i|576p|576i|720p|720i|1080p|1080i|3d|hrhd|hrhdtv|hddvd|bluray|x264|h264|xvid|xvidvd|www.www|cd[1-9]|\\[.*\\])");

            strTitleAndYear = strFileName;

            if (string.IsNullOrEmpty(strFileName) || strFileName.Equals(".."))
            {
                return null;
            }

            Regex reYear = new Regex(videoCleanDateTimeRegExp, RegexOptions.IgnoreCase);
            if (reYear.IsMatch(strTitleAndYear))
            {
                var m = reYear.Matches(strTitleAndYear);
                strTitleAndYear = m[0].Groups[1].Value;
                strYear = m[0].Groups[2].Value;
            }

            Regex episodeRegex = new Regex(@"(?<seasonandepisode>S(?<season>\d{1,2})E(?<episode>\d{1,2}))", RegexOptions.IgnoreCase);
            if (episodeRegex.IsMatch(strTitleAndYear))
            {
                strTitleAndYear = strTitleAndYear.Substring(0, episodeRegex.Match(strTitleAndYear).Index);
            }

            for (int i = 0; i < videoCleanStringRegExps.Count; i++)
            {
                Regex reTags = new Regex(videoCleanStringRegExps[i], RegexOptions.IgnoreCase);
                while (reTags.Matches(strTitleAndYear).Count > 0)
                {
                    int start = reTags.Matches(strTitleAndYear)[0].Index;
                    int end = start + reTags.Matches(strTitleAndYear)[0].Length;
                    strTitleAndYear = strTitleAndYear.Substring(0, start) +
                                        strTitleAndYear.Substring(end - 1, strTitleAndYear.Length - end);
                }
            }

            Regex rePrefix = new Regex(stringPrefixClean, RegexOptions.IgnoreCase);
            while (rePrefix.Matches(strTitleAndYear).Count > 0)
            {
                int start = rePrefix.Matches(strTitleAndYear)[0].Index;
                int end = start + rePrefix.Matches(strTitleAndYear)[0].Length;
                strTitleAndYear = end + 1 > strTitleAndYear.Length ? "" : strTitleAndYear.Substring(0, start) +
                                    strTitleAndYear.Substring(end + 1, strTitleAndYear.Length - end - 1);
            }

            if (bCleanChars)
            {
                bool initialDots = true;
                bool alreadyContainsSpace = (strTitleAndYear.Contains(' '));

                for (int i = 0; i < strTitleAndYear.Length; i++)
                {
                    char c = strTitleAndYear[i];

                    if (c != '.')
                        initialDots = false;

                    if ((c == '_') || ((!alreadyContainsSpace) && !initialDots && (c == '.')))
                    {
                        var chars = strTitleAndYear.ToCharArray();
                        chars[i] = ' ';
                        strTitleAndYear = new string(chars);
                    }
                }
            }

            strTitleAndYear = strTitleAndYear.Trim();
            strTitle = strTitleAndYear;

            if (!string.IsNullOrEmpty(strYear))
            {
                strTitleAndYear = strTitle + " (" + strYear + ")";
            }


            if (tryHash && File.Exists(originalFilename))
            {
                var langs = ServiceLocator.GetService<ISettings>().SubtitleLanguagesCodes.ToArray();
                var ret = HashMatcher.HashMatcher.Match(originalFilename, langs);
                if (ret.Any(r => !string.IsNullOrEmpty(r.ImdbCode)))
                {
                    try
                    {
                        var bestGuess = ret.GroupBy(g => g.ImdbCode).OrderByDescending(g => g.Count()).First().OrderBy(s => Levenshtein.Compare(Path.GetFileNameWithoutExtension(strFileName), Path.GetFileNameWithoutExtension(s.FileName))).First();

                        string sep2 = FindSeasonAndEpisode(bestGuess.FileName);
                        int tmpSeason = 0; int tmpEpisode = 0;
                        if (sep2 != "")
                        {
                            var sepx = sep1.ToLowerInvariant().Split('s', 'e');
                            if (sepx.Length == 3)
                            {
                                tmpSeason = int.Parse(sepx[1]);
                                tmpEpisode = int.Parse(sepx[2]);
                            }
                        }


                        var imdb = new IMDb(Convert.ToInt32(bestGuess.ImdbCode));

                        int imdbYear;
                        if (int.TryParse(imdb.Year, out imdbYear))
                        {
                            int fileYear;
                            if (int.TryParse(strYear, out fileYear))
                            {
                                if (imdbYear != fileYear)
                                {
                                    bestSubtitleGuess = null;
                                    return null;
                                }
                            }
                        }

                        if (imdb.IsSeries == false && tmpSeason > 0)
                        {
                            imdb.SeriesSeason = tmpSeason;
                            imdb.SeriesEpisode = tmpEpisode;
                            imdb.IsSeries = true;
                        }

                        if (imdb.status && (season == 0 || (imdb.SeriesSeason == season && imdb.SeriesEpisode == episode)))
                        {
                            strTitle = imdb.Title;
                            strTitleAndYear = imdb.Title + " (" + imdb.Year + ")";
                            strYear = imdb.Year;
                            season = 0;
                            episode = 0;
                            if (imdb.IsSeries)
                            {
                                season = imdb.SeriesSeason;
                                episode = imdb.SeriesEpisode;
                            }

                            var ordered = ret.Where(s => s.ImdbCode == bestGuess.ImdbCode).OrderBy(s => langs.ToList().IndexOf(s.LanguageCode)).ThenBy(s => Levenshtein.Compare(Path.GetFileNameWithoutExtension(strFileName), Path.GetFileNameWithoutExtension(s.FileName))).First();
                            bestSubtitleGuess = new Subtitle(ordered.Id, ordered.ProgramName, ordered.FileName, ordered.LanguageCode);
                            return imdb;
                        }
                    }
                    catch { }
                }
            }

            return null;
        }
    }
}
