using HashMatcher;
using HashMatcher.Util;
using OpenSubtitlesSearch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HashMatcher
{
    public class HashMatcher
    {
        public static List<Subtitle> Match(string file, params string[] languages)
        {
            try
            {
                OpenSubtitlesDownloader sd = new OpenSubtitlesDownloader();
                SearchQuery sq = new SearchQuery(@"");
                sq.LanguageCodes = languages;
                sq.FileSize = (int)new FileInfo(file).Length;
                sq.FileHash = FileUtils.HexadecimalHash(file);
                var found = sd.SearchSubtitles(sq);
                return found;
            }
            catch
            {
                return new List<Subtitle>();
            }
        }
    }
}
