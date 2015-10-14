using HtmlAgilityPack;
using SubtitleDownloader.Core;
using SubtitleDownloader.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
namespace MediaPoint.VM
{
    public class PodnapisiDownloader : ISubtitleDownloader
    {
        private readonly string baseUrl = "http://www.podnapisi.net";
        private readonly string searchUrlBase = "http://www.podnapisi.net/ppodnapisi/search?sXML=1";
        private XmlDocument xmlDoc;
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
            return "Podnapisi";
        }
        public List<Subtitle> SearchSubtitles(SearchQuery query)
        {
            string arg = this.searchUrlBase + "&sK=" + query.Query;
            if (query.Year.HasValue)
            {
                arg = arg + "&sY=" + query.Year;
            }
            return this.Search(arg, query);
        }
        public List<Subtitle> SearchSubtitles(EpisodeSearchQuery query)
        {
            string text = string.Concat(new object[]
			{
				this.searchUrlBase,
				"&sK=",
				query.SerieTitle,
				"&sTS=",
				query.Season,
				"&sTE=",
				query.Episode
			});
            return this.Search(text, query);
        }
        public List<Subtitle> SearchSubtitles(ImdbSearchQuery query)
        {
            throw new NotImplementedException();
        }
        public List<FileInfo> SaveSubtitle(Subtitle subtitle)
        {
            string id = subtitle.Id;
            string tempFileName = FileUtils.GetTempFileName();
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDocument = htmlWeb.Load(id);
            HtmlNodeCollection htmlNodeCollection = htmlDocument.DocumentNode.SelectNodes("//form");
            foreach (HtmlNode current in (IEnumerable<HtmlNode>)htmlNodeCollection)
            {
                string attributeValue = current.GetAttributeValue("action", string.Empty);
                if (attributeValue.Contains("/download"))
                {
                    WebClient webClient = new WebClient();
                    webClient.DownloadFile(this.baseUrl + attributeValue, tempFileName);
                    return FileUtils.ExtractFilesFromZipOrRarFile(tempFileName);
                }
            }
            throw new Exception("No download link found for subtitle!");
        }
        private List<Subtitle> Search(string baseUrl, SubtitleSearchQuery query)
        {
            Dictionary<string, string> dictionary = this.ParseLanguageOptions();
            List<Subtitle> list = new List<Subtitle>();
            foreach (string current in dictionary.Keys)
            {
                if (query.HasLanguageCode(Languages.FindLanguageCode(current)))
                {
                    string str = dictionary[current];
                    string requestUriString = baseUrl + "&sJ=" + str;
                    HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUriString);
                    if (this.SearchTimeout > 0)
                    {
                        httpWebRequest.Timeout = this.SearchTimeout * 1000;
                    }
                    HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
                    xmlReaderSettings.ProhibitDtd = false;
                    xmlReaderSettings.ValidationType = ValidationType.None;
                    XmlReader reader = XmlReader.Create(httpWebResponse.GetResponseStream(), xmlReaderSettings);
                    this.xmlDoc = new XmlDocument();
                    this.xmlDoc.Load(reader);
                    XmlNodeList elementsByTagName = this.xmlDoc.GetElementsByTagName("subtitle");
                    foreach (XmlNode xmlNode in elementsByTagName)
                    {
                        string id = null;
                        string text = null;
                        for (int i = 0; i < xmlNode.ChildNodes.Count; i++)
                        {
                            if (xmlNode.ChildNodes[i].Name == "url")
                            {
                                id = xmlNode.ChildNodes[i].InnerText;
                            }
                            else
                            {
                                if (xmlNode.ChildNodes[i].Name == "release")
                                {
                                    text = xmlNode.ChildNodes[i].InnerText.TrimEnd(new char[0]).TrimStart(new char[0]);
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(text))
                        {
                            //if (text.Contains(" "))
                            //{
                            //    string[] array = text.Split(new char[]
                            //    {
                            //        ' '
                            //    });
                            //    string[] array2 = array;
                            //    for (int j = 0; j < array2.Length; j++)
                            //    {
                            //        string text2 = array2[j];
                            //        Subtitle item = new Subtitle(id, text2, text2, Languages.FindLanguageCode(current));
                            //        list.Add(item);
                            //    }
                            //}
                            //else
                            //{
                                Subtitle item2 = new Subtitle(id, text, text, Languages.FindLanguageCode(current));
                                list.Add(item2);
                            //}
                        }
                    }
                }
            }
            return list;
        }
        private Dictionary<string, string> ParseLanguageOptions()
        {
            return new Dictionary<string, string>
			{

				{
					"Albanian",
					"29"
				},

				{
					"Arabic",
					"12"
				},

				{
					"Argentino",
					"14"
				},

				{
					"Belarus",
					"50"
				},

				{
					"Bosnian",
					"10"
				},

				{
					"Brazilian",
					"48"
				},

				{
					"Bulgarian",
					"33"
				},

				{
					"Catalan",
					"53"
				},

				{
					"Chinese",
					"17"
				},

				{
					"Croatian",
					"38"
				},

				{
					"Czech",
					"7"
				},

				{
					"Danish",
					"24"
				},

				{
					"Dutch",
					"23"
				},

				{
					"English",
					"2"
				},

				{
					"Estonian",
					"20"
				},

				{
					"Farsi",
					"52"
				},

				{
					"Finnish",
					"31"
				},

				{
					"French",
					"8"
				},

				{
					"German",
					"5"
				},

				{
					"Greek",
					"16"
				},

				{
					"Hebrew",
					"22"
				},

				{
					"Hindi",
					"42"
				},

				{
					"Hungarian",
					"15"
				},

				{
					"Icelandic",
					"6"
				},

				{
					"Indonesian",
					"54"
				},

				{
					"Irish",
					"49"
				},

				{
					"Italian",
					"9"
				},

				{
					"Japanese",
					"11"
				},

				{
					"Korean",
					"4"
				},

				{
					"Latvian",
					"21"
				},

				{
					"Lithuanian",
					"19"
				},

				{
					"Macedonian",
					"35"
				},

				{
					"Malay",
					"55"
				},

				{
					"Mandarin",
					"40"
				},

				{
					"Norwegian",
					"3"
				},

				{
					"Polish",
					"26"
				},

				{
					"Portuguese",
					"32"
				},

				{
					"Romanian",
					"13"
				},

				{
					"Russian",
					"27"
				},

				{
					"Serbian",
					"36"
				},

				{
					"Slovak",
					"37"
				},

				{
					"Slovenian",
					"1"
				},

				{
					"Spanish",
					"28"
				},

				{
					"Swedish",
					"25"
				},

				{
					"Thai",
					"44"
				},

				{
					"Turkish",
					"30"
				},

				{
					"Ukrainian",
					"46"
				},

				{
					"Vietnamese",
					"51"
				}
			};
        }
    }
}
