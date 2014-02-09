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
        public static string DownloadSubtitle(string fileName, string[] preferredLanguages, string[] preferredServices, out IMDb imdbMatch, out List<SubtitleMatch> otherChoices)
        {
            var fn = Path.GetFileName(fileName);
            SubtitleMatch subtitle = FindSubtitleForFilename(fn, preferredLanguages, preferredServices, out imdbMatch, out otherChoices);
            return DownloadSubtitle(subtitle, fileName);
        }

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
                                                                       subtitle.Subtitle.LanguageCode,
                                                                       Path.GetExtension(files[0].FullName)));
                        if (File.Exists(subNewName))
                        {
                            File.Delete(subNewName);
                        }
                        files[0].MoveTo(subNewName);
                        resultFile = files[0].FullName;
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
                        files[cdIndex].MoveTo(subNewName);
                        resultFile = files[cdIndex].FullName;
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

        public static IMDb GetIMDbFromFilename(string filename, out string strTitle, out string strTitleAndYear, out string strYear)
        {
            // extract meaningful data from the filename
            GetMovieMetadata(
                filename,
                out strTitle,
                out strTitleAndYear,
                out strYear);

            if (strTitle != "" && strYear == "")
            {
                // no year, maybe we find one in the folder
                string tmpTitle;
                string tmpTitleAndYear;
                GetMovieMetadata(Path.GetDirectoryName(filename) + ".dummyextension",
                    out tmpTitle,
                    out tmpTitleAndYear,
                    out strYear);

                if (strYear != "")
                    strTitleAndYear = string.Format("{0} ({1})", strTitle, strYear);
            }

            // cleanup multiple spaces
            strTitle = new Regex(@"\s+").Replace(strTitle, " ");

            IMDb imdb = null;

            if (!string.IsNullOrEmpty(strTitle))
            {
                // scrape imdb to find movie metadata
                imdb = new IMDb(strTitle, strYear, false);
            }

            if (imdb == null || imdb.status == false)
            {
                // if failed try with folder name
                GetMovieMetadata(
                Path.GetDirectoryName(filename) + ".dummyextension",
                out strTitle,
                out strTitleAndYear,
                out strYear);

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

        public static SubtitleMatch FindSubtitleForFilename(string filename, string[] preferredLanguages, string[] preferredServices, out IMDb imdbMatch, out List<SubtitleMatch> otherChoices, bool noFiltering = false, bool needsImdb = true)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            filename = r.Replace(filename, "");

            filename = Path.GetFileName(filename);
            string strTitle, strYear, strTitleAndYear;
            imdbMatch = GetIMDbFromFilename(filename, out strTitle, out strTitleAndYear, out strYear);
            string sep1 = FindSeasonAndEpisode(filename);

            if (sep1 != "") needsImdb = false;

            if (imdbMatch == null && needsImdb)
            {
                imdbMatch = null;
                otherChoices = null;
                return null;
            }

            string[] patterns = new string[imdbMatch == null ? 1 : string.IsNullOrEmpty(imdbMatch.OriginalTitle) || string.IsNullOrEmpty(imdbMatch.Title) ? 2 : 3];
            patterns[0] = filename;
            if (patterns.Length > 1) patterns[1] = imdbMatch.Title;
            if (patterns.Length > 2) patterns[2] = imdbMatch.OriginalTitle;
            if (imdbMatch != null) for (int i = 1; i < patterns.Length; i++) patterns[i] += string.Format(" ({0})", imdbMatch.Year);
            
            var ret = GetOrderedSubsMatches(preferredServices, preferredLanguages, patterns, noFiltering);
            
            ret.RemoveAll(m => m.Score <= 0.1); // crap

            if (!needsImdb && imdbMatch == null)
            {
                ret.RemoveAll(m => WordMatches(m.MatchRelease, patterns[0]) <= 0.2 || (sep1 != "" && FindSeasonAndEpisode(m.MatchRelease) != sep1)); // even more crap if no imdb match
            }
            
            otherChoices = ret;
            
            var ret2 = ret.Select(s => new { priority = preferredLanguages.ToList().IndexOf(s.Language), Sub = s }).OrderBy(s => preferredLanguages.Length - s.priority).FirstOrDefault();

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

            return "";
        }

        public static List<SubtitleMatch> GetOrderedSubsMatches(string[] services, string[] languages, string[] patterns, bool noFiltering = false)
        {
            List<SubtitleMatch> scores = new List<SubtitleMatch>();
            List<Subtitle> subtitles = new List<Subtitle>();
            
            foreach (var service in services)
            {
                var d = SubtitleDownloaderFactory.GetSubtitleDownloader(service);
                List<Subtitle> foundSubtitles = new List<Subtitle>();

                for (int i = 0; i < patterns.Length; i++)
                {
                    var q = new SearchQuery(patterns[i].Replace('.', ' '));
                    q.LanguageCodes = languages;
                    var subs = d.SearchSubtitles(q);

                    if (subs.Count > 0)
                    {
                        foundSubtitles.AddRange(subs);
                        if (!noFiltering) break;
                    }
                }

                if (foundSubtitles.Count > 0)
                {
                    var newScores = RateSubs(foundSubtitles, patterns, service == "Podnapisi").Where(m => m.Score > 0).OrderBy(m => m.Score).Reverse().ToList();
                    foreach (var score in newScores)
                        score.Service = service;

                    scores.AddRange(newScores);

                    if (newScores.First().Score > 1 && !noFiltering)
                        break;
                }
            }

            PriritizebyMedium(scores, patterns);
            PriritizebyLanguage(scores, patterns);

            return scores.OrderBy(m => m.Score).Reverse().ToList();
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
            foreach (var subtitle in subs)
            {
                var files = podnapisi ? subtitle.FileName.Split(' ') : new string[] { Path.GetFileNameWithoutExtension(subtitle.FileName) };
                double best = 0;
                string bestFile = "";
                string bestPattern = "";
                foreach (var file in files)
                {
                    string title1, year1, titleAndYear1;

                    GetMovieMetadata(file, out title1, out titleAndYear1, out year1);

                    foreach (var pattern in patterns)
                    {
                        string title2, year2, titleAndYear2;

                        GetMovieMetadata(pattern, out title2, out titleAndYear2, out year2);

                        double m = WordMatches(year2 == "" ? title1 : titleAndYear1, year1 == "" ? title2 : titleAndYear2);

                        if (!string.IsNullOrEmpty(year1) && year1 == year2)
                            m *= 1.1;
                        if (!string.IsNullOrEmpty(year1) && !string.IsNullOrEmpty(year2) && year1 != year2)
                            m *= 0.9;

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

        public static void GetMovieMetadata(string strFileName, out string strTitle, out string strTitleAndYear, out string strYear, bool bCleanChars = true)
        {
            strFileName = strFileName.Length < 5 || !strFileName.Substring(strFileName.Length - 4, 4).StartsWith(".") ? strFileName : Path.GetFileNameWithoutExtension(strFileName);
            strTitle = "";
            strYear = "";

            const string videoCleanDateTimeRegExp = "(.*[^ _\\,\\.\\(\\)\\[\\]\\-])[ _\\.\\(\\)\\[\\]\\-]+(19[0-9][0-9]|20[0-1][0-9])([ _\\,\\.\\(\\)\\[\\]\\-]|[^0-9]$)";

            var videoCleanStringRegExps = new List<string>();
            videoCleanStringRegExps.Add("[ _\\,\\.\\(\\)\\[\\]\\-](ac3|dts|custom|dc|remastered|divx|divx5|dsr|dsrip|dutch|dvd|dvd5|dvd9|dvdrip|dvdscr|dvdscreener|screener|dvdivx|cam|fragment|fs|hdtv|hdrip|hdtvrip|internal|limited|multisubs|ntsc|ogg|ogm|pal|pdtv|proper|repack|rerip|retail|r3|r5|bd5|se|svcd|swedish|german|read.nfo|nfofix|unrated|extended|ws|telesync|ts|telecine|tc|brrip|bdrip|480p|480i|576p|576i|720p|720i|1080p|1080i|3d|hrhd|hrhdtv|hddvd|bluray|x264|h264|xvid|xvidvd|xxx|www.www|cd[1-9]|\\[.*\\])([ _\\,\\.\\(\\)\\[\\]\\-]|$)");
            videoCleanStringRegExps.Add("(\\[.*\\])");

            string stringPrefixClean = ("^(ac3|dts|remastered|divx|divx5|dsr|dsrip|dvd|dvd5|dvd9|dvdrip|dvdscr|dvdscreener|screener|dvdivx|hdtv|hdrip|hdtvrip|internal|limited|multisubs|ntsc|ogg|ogm|pal|pdtv|repack|rerip|retail|r3|r5|bd5|se|svcd|read.nfo|nfofix|unrated|ws|telesync|ts|telecine|tc|brrip|bdrip|480p|480i|576p|576i|720p|720i|1080p|1080i|3d|hrhd|hrhdtv|hddvd|bluray|x264|h264|xvid|xvidvd|www.www|cd[1-9]|\\[.*\\])");

            strTitleAndYear = strFileName;

            if (string.IsNullOrEmpty(strFileName) || strFileName.Equals(".."))
            {
                return;
            }

            Regex reYear = new Regex(videoCleanDateTimeRegExp, RegexOptions.IgnoreCase);
            if (reYear.IsMatch(strTitleAndYear))
            {
                var m = reYear.Matches(strTitleAndYear);
                strTitleAndYear = m[0].Groups[1].Value;
                strYear = m[0].Groups[2].Value;
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

        }
    }
}
