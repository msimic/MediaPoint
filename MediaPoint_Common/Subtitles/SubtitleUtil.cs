using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MediaPoint.Common.Helpers;
using MediaPoint.Helpers;
using SubtitleDownloader.Core;

namespace MediaPoint.Common.Subtitles
{
    public class SubtitleUtil
    {

        class SubMatch
        {
            public int Rating { get; set; }
            public Subtitle Subtitle { get; set; }
            public ISubtitleDownloader Downloader { get; set; }
        }

        public static string DownloadSubtitle(string fileName, string[] preferredLanguages, string[] preferredServices, out IMDb imdbMatch)
        {
            ISubtitleDownloader downloader;
            string resultFile = null;

            Subtitle subtitle = FindSubtitleForFilename
                (
                    fileName,
                    preferredLanguages,
                    preferredServices,
                    out downloader,
                    out imdbMatch
                );

            if (downloader != null && subtitle != null)
            {
                var dir = Path.GetDirectoryName(fileName);
                if (dir != null && Directory.Exists(dir))
                {
                    var files = downloader.SaveSubtitle(subtitle);
                    string tmpDir = files[0].Directory.FullName;

                    if (files.Count == 1)
                    {
                        string subNewName = Path.Combine(dir,
                                                         string.Format("{0}_{1}{2}", Path.GetFileNameWithoutExtension(fileName),
                                                                       subtitle.LanguageCode,
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
                                                                             cdIndex, subtitle.LanguageCode,
                                                                             Path.GetExtension(files[cdIndex].FullName))
                                                             : string.Format("{0}_{1}{2}",
                                                                             Path.GetFileNameWithoutExtension(fileName),
                                                                             subtitle.LanguageCode,
                                                                             Path.GetExtension(files[cdIndex].FullName)));

                        if (File.Exists(subNewName))
                        {
                            File.Delete(subNewName);
                        }
                        files[cdIndex].MoveTo(subNewName);
                        resultFile = files[cdIndex].FullName;
                    }

                    if (Path.GetTempPath() != tmpDir &&
                        tmpDir.ToLowerInvariant().StartsWith(Path.GetTempPath().ToLowerInvariant()))
                        Directory.Delete(tmpDir, true);
                }
            }

            return resultFile;
        }

        public static IMDb GetIMDbFromFilename(string filename, out string strTitle, out string strTitleAndYear, out string strYear, bool bCleanChars = true)
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

            if (string.IsNullOrEmpty(strTitle))
            {
                // we need a title to do something
                return null;
            }

            // scrape imdb to find movie metadata
            var imdb = new IMDb(strTitle, strYear, false);

            if (imdb.status == false)
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

                if (imdb.status == false)
                {
                    // we need imdb match to do something
                    return null;
                }
            }

            char[] punctuation = new char[] { ',', '.', '-', ':', '"', ';', '\'' };
            string tmpTitle1 = new string(imdb.Title.TrimEnd('.', ',', ' ').ToLowerInvariant().ToCharArray().Where(c => !punctuation.Contains(c)).ToArray());
            tmpTitle1 = new Regex(@"\s+").Replace(tmpTitle1, " ");
            string tmpTitle2 = new string(strTitle.Trim('.', ',', ' ').ToLowerInvariant().ToCharArray().Where(c => !punctuation.Contains(c)).ToArray());
            tmpTitle2 = new Regex(@"\s+").Replace(tmpTitle2, " ");
            string lastWord = tmpTitle1.Split(' ').Last();

            // need to have the last word from imdb to check for validity (real match)
            if (tmpTitle2.IndexOf(lastWord) == -1)
            {
                return null;
            }

            // cut the part that ends where imdb ends
            tmpTitle2 = tmpTitle2.Substring(0, tmpTitle2.IndexOf(lastWord) + lastWord.Length);

            // check for minimal difference from imdb (punctuation perhaps)
            if (Levenshtein.Compare(tmpTitle1, tmpTitle2) > tmpTitle1.Length * 0.1)
            {
                if (string.IsNullOrEmpty(imdb.OriginalTitle))
                {
                    return null;
                }

                // now check if we have an original title and do the same
                tmpTitle1 = new string(imdb.OriginalTitle.TrimEnd('.', ',', ' ').ToLowerInvariant().ToCharArray().Where(c => !punctuation.Contains(c)).ToArray());
                tmpTitle1 = new Regex(@"\s+").Replace(tmpTitle1, " ");

                lastWord = tmpTitle1.Split(' ').Last();
                if (tmpTitle2.IndexOf(lastWord) == -1)
                {
                    return null;
                }

                tmpTitle2 = tmpTitle2.Substring(0, tmpTitle2.IndexOf(lastWord) + lastWord.Length);

                if (Levenshtein.Compare(tmpTitle1, tmpTitle2) > tmpTitle1.Length * 0.1)
                {
                    return null;
                }
            }

            return imdb;
        }

        public static Subtitle FindSubtitleForFilename(string filename, string[] preferredLanguages, string[] preferredServices, out ISubtitleDownloader downloaderForResult, out IMDb imdbMatch)
        {
            string strTitle, strYear, strTitleAndYear;
            IMDb imdb = GetIMDbFromFilename(filename, out strTitle, out strTitleAndYear, out strYear);

            if (imdb == null)
            {
                downloaderForResult = null;
                imdbMatch = null;
                return null;
            }

            imdbMatch = imdb;

            // create a query for subtitle services
            var query = new SearchQuery(imdb.OriginalTitle == "" ? imdb.Title : imdb.OriginalTitle);
            query.LanguageCodes = preferredLanguages;

            // we might know the year from the filename
            // we will trust it only if the imdb result matches it
            int year;
            if (int.TryParse(strYear, out year))
            {
                // filename year and imdb year, use if they match else don't use year search at all
                int imdbYear;
                int.TryParse(imdb.Year, out imdbYear);
                if (year == imdbYear) query.Year = year;
            }
            else
            {
                // no filename year, so just use imdb year
                int imdbYear;
                int.TryParse(imdb.Year, out imdbYear);
                query.Year = imdbYear;
            }

            // loop over all subtitle services and remember best results for each
            var perServiceBest = new SubMatch[preferredServices.Length];
            for (int y = 0; y < preferredServices.Length; y++)
            {
                // search using this service
                var s = SubtitleDownloaderFactory.GetSubtitleDownloader(preferredServices[y]);
                var r = s.SearchSubtitles(query);

                // levenhstein rating of each result
                var rs = RateSubs(r, Path.GetFileNameWithoutExtension(filename), preferredServices[y] == "Podnapisi").ToArray();

                // we might get the first language here and skip looping later
                var sub = rs.OrderBy(st => st.Rating).FirstOrDefault(st => st.Subtitle.LanguageCode == query.LanguageCodes[0]);

                // find best for this service (language then rating)
                if (sub == null) for (int i = 0; i < query.LanguageCodes.Length; i++)
                    {
                        sub = rs.OrderBy(st => st.Rating).FirstOrDefault(st => st.Subtitle.LanguageCode == query.LanguageCodes[i]);
                        if (sub != null) break;
                    }

                // remember best for this service
                if (sub != null)
                {
                    sub.Downloader = s;
                    perServiceBest[y] = sub;

                    // perfect match, skip doing additional downloads and assume no better can be found
                    if (sub.Subtitle != null && sub.Rating == 0 && sub.Subtitle.LanguageCode == query.LanguageCodes[0]) break;
                }
            }

            SubMatch best = null;
            // we have one result per downloader which should be the best result for languages preferred, or the next language preferred if none was found
            for (int i = 0; i < query.LanguageCodes.Length; i++)
            {
                best = perServiceBest.Where(d => d != null).OrderBy(d => d.Rating).FirstOrDefault(d => d.Subtitle.LanguageCode == query.LanguageCodes[i]);
                if (best != null) break;
            }

            if (best != null)
            {
                downloaderForResult = best.Downloader;
                return best.Subtitle;
            }

            downloaderForResult = null;
            return null;
        }

        private static IEnumerable<SubMatch> RateSubs(IEnumerable<Subtitle> subs, string filename, bool podnapisi)
        {
            foreach (var subtitle in subs)
            {
                var files = podnapisi ? subtitle.FileName.Split(' ') : new string[] { subtitle.FileName };
                int best = -1000;
                foreach (var file in files)
                {
                    string cmp = file.ToUpperInvariant();
                    if (!podnapisi)
                    {
                        cmp = Path.GetFileNameWithoutExtension(cmp);
                    }
                    else
                    {
                        cmp = cmp.Replace('.', ' ');
                    }
                    string cmp2 = filename.ToUpperInvariant();
                    cmp2 = cmp2.Replace('.', ' ');
                    var rate = Levenshtein.Compare(cmp, cmp2);
                    if (Math.Abs(best) > Math.Abs(rate)) best = Math.Abs(rate);
                }
                yield return new SubMatch() { Rating = best, Subtitle = subtitle };
            }
        }

        public static void GetMovieMetadata(string strFileName, out string strTitle, out string strTitleAndYear, out string strYear, bool bCleanChars = true)
        {
            strFileName = Path.GetFileNameWithoutExtension(strFileName);
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
                                        strTitleAndYear.Substring(end, strTitleAndYear.Length - end);
                }
            }

            Regex rePrefix = new Regex(stringPrefixClean, RegexOptions.IgnoreCase);
            while (rePrefix.Matches(strTitleAndYear).Count > 0)
            {
                int start = rePrefix.Matches(strTitleAndYear)[0].Index;
                int end = start + rePrefix.Matches(strTitleAndYear)[0].Length;
                strTitleAndYear = strTitleAndYear.Substring(0, start) +
                                    strTitleAndYear.Substring(end+1, strTitleAndYear.Length - end-1);
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
