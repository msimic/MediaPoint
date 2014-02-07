using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPoint.Common.Helpers
{
    public static class LanguageISOTranslator
    {
        private static List<string[]> _all;

        public class Language
        {
            public string ISO639_1 {get;set;}
            public string ISO639_2B {get;set;}
            public string ISO639_2T { get; set; }
            public string EnglishName { get; set; }
        }

        public static LangByISO ISO839_1 { get; private set; }
        public static LangByISO ISO839_2B { get; private set; }
        public static LangByISO ISO839_2T { get; private set; }

        static LanguageISOTranslator()
        {
            _all = new List<string[]>
            {
                new string[] {"aa", "aar", "aar", "Afar"},
                new string[] {"ab", "abk", "abk", "Abkhazian"},
                new string[] {"ae", "ave", "ave", "Avestan"},
                new string[] {"af", "afr", "afr", "Afrikaans"},
                new string[] {"ak", "aka", "aka", "Akan"},
                new string[] {"am", "amh", "amh", "Amharic"},
                new string[] {"an", "arg", "arg", "Aragonese"},
                new string[] {"ar", "ara", "ara", "Arabic"},
                new string[] {"as", "asm", "asm", "Assamese"},
                new string[] {"av", "ava", "ava", "Avaric"},
                new string[] {"ay", "aym", "aym", "Aymara"},
                new string[] {"az", "aze", "aze", "Azerbaijani"},
                new string[] {"ba", "bak", "bak", "Bashkir"},
                new string[] {"be", "bel", "bel", "Belarusian"},
                new string[] {"bg", "bul", "bul", "Bulgarian"},
                new string[] {"bh", "bih", "bih", "Bihari languages"},
                new string[] {"bi", "bis", "bis", "Bislama"},
                new string[] {"bm", "bam", "bam", "Bambara"},
                new string[] {"bn", "ben", "ben", "Bengali"},
                new string[] {"bo", "tib", "bod", "Tibetan"},
                new string[] {"br", "bre", "bre", "Breton"},
                new string[] {"bs", "bos", "bos", "Bosnian"},
                new string[] {"ca", "cat", "cat", "Catalan; Valencian"},
                new string[] {"ce", "che", "che", "Chechen"},
                new string[] {"ch", "cha", "cha", "Chamorro"},
                new string[] {"co", "cos", "cos", "Corsican"},
                new string[] {"cr", "cre", "cre", "Cree"},
                new string[] {"cs", "cze", "ces", "Czech"},
                new string[] {"cu", "chu", "chu", "Church Slavic; Old Slavonic; Church Slavonic; Old Bulgarian; Old Church Slavonic"},
                new string[] {"cv", "chv", "chv", "Chuvash"},
                new string[] {"cy", "wel", "cym", "Welsh"},
                new string[] {"da", "dan", "dan", "Danish"},
                new string[] {"de", "ger", "deu", "German"},
                new string[] {"dv", "div", "div", "Divehi; Dhivehi; Maldivian"},
                new string[] {"dz", "dzo", "dzo", "Dzongkha"},
                new string[] {"ee", "ewe", "ewe", "Ewe"},
                new string[] {"el", "gre", "ell", "Greek, Modern (1453-)"},
                new string[] {"en", "eng", "eng", "English"},
                new string[] {"eo", "epo", "epo", "Esperanto"},
                new string[] {"es", "spa", "spa", "Spanish; Castilian"},
                new string[] {"et", "est", "est", "Estonian"},
                new string[] {"eu", "baq", "eus", "Basque"},
                new string[] {"fa", "per", "fas", "Persian"},
                new string[] {"ff", "ful", "ful", "Fulah"},
                new string[] {"fi", "fin", "fin", "Finnish"},
                new string[] {"fj", "fij", "fij", "Fijian"},
                new string[] {"fo", "fao", "fao", "Faroese"},
                new string[] {"fr", "fre", "fra", "French"},
                new string[] {"fy", "fry", "fry", "Western Frisian"},
                new string[] {"ga", "gle", "gle", "Irish"},
                new string[] {"gd", "gla", "gla", "Gaelic; Scottish Gaelic"},
                new string[] {"gl", "glg", "glg", "Galician"},
                new string[] {"gn", "grn", "grn", "Guarani"},
                new string[] {"gu", "guj", "guj", "Gujarati"},
                new string[] {"gv", "glv", "glv", "Manx"},
                new string[] {"ha", "hau", "hau", "Hausa"},
                new string[] {"he", "heb", "heb", "Hebrew"},
                new string[] {"hi", "hin", "hin", "Hindi"},
                new string[] {"ho", "hmo", "hmo", "Hiri Motu"},
                new string[] {"hr", "hrv", "hrv", "Croatian"},
                new string[] {"ht", "hat", "hat", "Haitian; Haitian Creole"},
                new string[] {"hu", "hun", "hun", "Hungarian"},
                new string[] {"hy", "arm", "hye", "Armenian"},
                new string[] {"hz", "her", "her", "Herero"},
                new string[] {"ia", "ina", "ina", "Interlingua (International Auxiliary Language Association)"},
                new string[] {"id", "ind", "ind", "Indonesian"},
                new string[] {"ie", "ile", "ile", "Interlingue; Occidental"},
                new string[] {"ig", "ibo", "ibo", "Igbo"},
                new string[] {"ii", "iii", "iii", "Sichuan Yi; Nuosu"},
                new string[] {"ik", "ipk", "ipk", "Inupiaq"},
                new string[] {"io", "ido", "ido", "Ido"},
                new string[] {"is", "ice", "isl", "Icelandic"},
                new string[] {"it", "ita", "ita", "Italian"},
                new string[] {"iu", "iku", "iku", "Inuktitut"},
                new string[] {"ja", "jpn", "jpn", "Japanese"},
                new string[] {"jv", "jav", "jav", "Javanese"},
                new string[] {"ka", "geo", "kat", "Georgian"},
                new string[] {"kg", "kon", "kon", "Kongo"},
                new string[] {"ki", "kik", "kik", "Kikuyu; Gikuyu"},
                new string[] {"kj", "kua", "kua", "Kuanyama; Kwanyama"},
                new string[] {"kk", "kaz", "kaz", "Kazakh"},
                new string[] {"kl", "kal", "kal", "Kalaallisut; Greenlandic"},
                new string[] {"km", "khm", "khm", "Central Khmer"},
                new string[] {"kn", "kan", "kan", "Kannada"},
                new string[] {"ko", "kor", "kor", "Korean"},
                new string[] {"kr", "kau", "kau", "Kanuri"},
                new string[] {"ks", "kas", "kas", "Kashmiri"},
                new string[] {"ku", "kur", "kur", "Kurdish"},
                new string[] {"kv", "kom", "kom", "Komi"},
                new string[] {"kw", "cor", "cor", "Cornish"},
                new string[] {"ky", "kir", "kir", "Kirghiz; Kyrgyz"},
                new string[] {"la", "lat", "lat", "Latin"},
                new string[] {"lb", "ltz", "ltz", "Luxembourgish; Letzeburgesch"},
                new string[] {"lg", "lug", "lug", "Ganda"},
                new string[] {"li", "lim", "lim", "Limburgan; Limburger; Limburgish"},
                new string[] {"ln", "lin", "lin", "Lingala"},
                new string[] {"lo", "lao", "lao", "Lao"},
                new string[] {"lt", "lit", "lit", "Lithuanian"},
                new string[] {"lu", "lub", "lub", "Luba-Katanga"},
                new string[] {"lv", "lav", "lav", "Latvian"},
                new string[] {"mg", "mlg", "mlg", "Malagasy"},
                new string[] {"mh", "mah", "mah", "Marshallese"},
                new string[] {"mi", "mao", "mri", "Maori"},
                new string[] {"mk", "mac", "mkd", "Macedonian"},
                new string[] {"ml", "mal", "mal", "Malayalam"},
                new string[] {"mn", "mon", "mon", "Mongolian"},
                new string[] {"mr", "mar", "mar", "Marathi"},
                new string[] {"ms", "may", "msa", "Malay"},
                new string[] {"mt", "mlt", "mlt", "Maltese"},
                new string[] {"my", "bur", "mya", "Burmese"},
                new string[] {"na", "nau", "nau", "Nauru"},
                new string[] {"nb", "nob", "nob", "Bokmål, Norwegian; Norwegian Bokmål"},
                new string[] {"nd", "nde", "nde", "Ndebele, North; North Ndebele"},
                new string[] {"ne", "nep", "nep", "Nepali"},
                new string[] {"ng", "ndo", "ndo", "Ndonga"},
                new string[] {"nl", "dut", "nld", "Dutch; Flemish"},
                new string[] {"nn", "nno", "nno", "Norwegian Nynorsk; Nynorsk, Norwegian"},
                new string[] {"no", "nor", "nor", "Norwegian"},
                new string[] {"nr", "nbl", "nbl", "Ndebele, South; South Ndebele"},
                new string[] {"nv", "nav", "nav", "Navajo; Navaho"},
                new string[] {"ny", "nya", "nya", "Chichewa; Chewa; Nyanja"},
                new string[] {"oc", "oci", "oci", "Occitan (post 1500)"},
                new string[] {"oj", "oji", "oji", "Ojibwa"},
                new string[] {"om", "orm", "orm", "Oromo"},
                new string[] {"or", "ori", "ori", "Oriya"},
                new string[] {"os", "oss", "oss", "Ossetian; Ossetic"},
                new string[] {"pa", "pan", "pan", "Panjabi; Punjabi"},
                new string[] {"pi", "pli", "pli", "Pali"},
                new string[] {"pl", "pol", "pol", "Polish"},
                new string[] {"ps", "pus", "pus", "Pushto; Pashto"},
                new string[] {"pt", "por", "por", "Portuguese"},
                new string[] {"qu", "que", "que", "Quechua"},
                new string[] {"rm", "roh", "roh", "Romansh"},
                new string[] {"rn", "run", "run", "Rundi"},
                new string[] {"ro", "rum", "ron", "Romanian; Moldavian; Moldovan"},
                new string[] {"ru", "rus", "rus", "Russian"},
                new string[] {"rw", "kin", "kin", "Kinyarwanda"},
                new string[] {"sa", "san", "san", "Sanskrit"},
                new string[] {"sc", "srd", "srd", "Sardinian"},
                new string[] {"sd", "snd", "snd", "Sindhi"},
                new string[] {"se", "sme", "sme", "Northern Sami"},
                new string[] {"sg", "sag", "sag", "Sango"},
                new string[] {"si", "sin", "sin", "Sinhala; Sinhalese"},
                new string[] {"sk", "slo", "slk", "Slovak"},
                new string[] {"sl", "slv", "slv", "Slovenian"},
                new string[] {"sm", "smo", "smo", "Samoan"},
                new string[] {"sn", "sna", "sna", "Shona"},
                new string[] {"so", "som", "som", "Somali"},
                new string[] {"sq", "alb", "sqi", "Albanian"},
                new string[] {"sr", "srp", "srp", "Serbian"},
                new string[] {"ss", "ssw", "ssw", "Swati"},
                new string[] {"st", "sot", "sot", "Sotho, Southern"},
                new string[] {"su", "sun", "sun", "Sundanese"},
                new string[] {"sv", "swe", "swe", "Swedish"},
                new string[] {"sw", "swa", "swa", "Swahili"},
                new string[] {"ta", "tam", "tam", "Tamil"},
                new string[] {"te", "tel", "tel", "Telugu"},
                new string[] {"tg", "tgk", "tgk", "Tajik"},
                new string[] {"th", "tha", "tha", "Thai"},
                new string[] {"ti", "tir", "tir", "Tigrinya"},
                new string[] {"tk", "tuk", "tuk", "Turkmen"},
                new string[] {"tl", "tgl", "tgl", "Tagalog"},
                new string[] {"tn", "tsn", "tsn", "Tswana"},
                new string[] {"to", "ton", "ton", "Tonga (Tonga Islands)"},
                new string[] {"tr", "tur", "tur", "Turkish"},
                new string[] {"ts", "tso", "tso", "Tsonga"},
                new string[] {"tt", "tat", "tat", "Tatar"},
                new string[] {"tw", "twi", "twi", "Twi"},
                new string[] {"ty", "tah", "tah", "Tahitian"},
                new string[] {"ug", "uig", "uig", "Uighur; Uyghur"},
                new string[] {"uk", "ukr", "ukr", "Ukrainian"},
                new string[] {"ur", "urd", "urd", "Urdu"},
                new string[] {"uz", "uzb", "uzb", "Uzbek"},
                new string[] {"ve", "ven", "ven", "Venda"},
                new string[] {"vi", "vie", "vie", "Vietnamese"},
                new string[] {"vo", "vol", "vol", "Volapük"},
                new string[] {"wa", "wln", "wln", "Walloon"},
                new string[] {"wo", "wol", "wol", "Wolof"},
                new string[] {"xh", "xho", "xho", "Xhosa"},
                new string[] {"yi", "yid", "yid", "Yiddish"},
                new string[] {"yo", "yor", "yor", "Yoruba"},
                new string[] {"za", "zha", "zha", "Zhuang; Chuang"},
                new string[] {"zh", "chi", "zho", "Chinese"},
                new string[] {"zu", "zul", "zul", "Zulu"},
            };

            ISO839_1 = new LangByISO(_all, 0);
            ISO839_2B = new LangByISO(_all, 1);
            ISO839_2T = new LangByISO(_all, 2);
        }

    }

    public class LangByISO
    {
        private List<string[]> _langs;
        private int _index;

        public LangByISO(List<string[]> langs, int indexInArray)
        {
            _langs = langs;
            _index = indexInArray;
        }

        public LanguageISOTranslator.Language this[string langCode]
        {
            get
            {
                var ret = _langs.FirstOrDefault(l => l[_index] == langCode);

                if (ret != null)
                {
                    return new LanguageISOTranslator.Language { ISO639_1 = ret[0], ISO639_2B=ret[1], ISO639_2T=ret[2], EnglishName=ret[3] };
                }

                return null;
            }
        }
    }
}
