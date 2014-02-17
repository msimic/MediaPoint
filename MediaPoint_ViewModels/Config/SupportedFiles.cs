using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPoint.VM.Config
{
    public static class SupportedFiles
    {
        private static readonly Dictionary<string, string> AudioFiles = new Dictionary<string, string>()
        {
            {"flac", "Free Lossless Audio Codec File"},
            {"m4a", "MPEG-4 Audio File"},
            {"wav", "WAVE Audio File"},
            {"mpa", "MPEG Audio File"},
            {"mp2", "MPEG Layer II Compressed Audio File"},
            {"m2a", "Apple MPEG-1/2 Audio"},
            {"wma", "Windows Media Audio File"},
            {"ra", "Real Media Audio"},
            {"ram", "Real Audio Metadata File"},
            {"ogg", "Ogg Vorbis Audio File"},
            {"oga", "Ogg Vorbis Audio File"},
            {"mp3", "MPEG-1/2 Layer III Audio File"},
            {"aac", "Advanced Audio Coding File"},
        };

        private static readonly Dictionary<string, string> VideoFiles = new Dictionary<string, string>()
        {
            {"flv", "Flash Video File"},
            {"m4v", "iTunes Video File"},
            {"m2ts", "Blu-ray BDAV Video File"},
            {"bdmv", "Blu-ray Disc Movie Information File"},
            {"mpeg", "MPEG Video File"},
            {"mpg", "MPEG Video File"},
            {"mpe", "MPEG Video File"},
            {"m1s", "QuickTimeMPEG video/audio stream"},
            {"mp2v", "MPEG-2 Video File"},
            {"m2v", "MPEG-2 Video File"},
            {"m2s", "MPEG-2 Audio and Video"},
            {"avi", "Audio Video Interleave File"},
            {"mov", "Apple QuickTime Movie"},
            {"qt", "Apple QuickTime Movie"},
            {"asf", "Advanced Systems Format File"},
            {"asx", "Microsoft ASF Redirector File"},
            {"wmv", "Windows Media Video File"},
            {"wmx", "Windows Media Redirector File"},
            {"rm", "Real Media Video"},
            {"rmvb", "RealMedia Variable Bit Rate File"},
            {"mp4", "MPEG-4 Video File"},
            {"3gp", "3GPP Multimedia File"},
            {"ogm", "Ogg Media File"},
            {"mkv", "Matroska Video File"},
            {"ogv", "Ogg Vorbis Video File"},
            {"ogx", "Ogg Vorbis Multiplexed Media File"},
            {"vob", "DVD Video Object File"}
        };

        private static readonly Dictionary<string, string> SubFiles = new Dictionary<string, string>()
        {
            {"idx", "VobSub subtitle"},
            {"ssa", "SubStation Alpha subtitle"},
            {"ass", "Advanced SubStation Alpha subtitle"},
            {"srt", "SubRip subtitle"},
            {"sub", "MicroDVD subtitle"},
            {"smi", "SAMI subtitle"},
            {"psb", "PowerDivX subtitle"},
            {"usf", "Universal Subtitle Format subtitle"},
            {"ssf", "Structured Subtitle Format subtitle"}
        };

        public static Dictionary<string, string> Audio { get { return AudioFiles; } }
        public static Dictionary<string, string> Video { get { return VideoFiles; } }
        public static Dictionary<string, string> All { get { return AudioFiles.Union(VideoFiles).Union(SubFiles).ToDictionary(k => k.Key, v => v.Value); } }

        public static string OpenFileDialogFilter
        {
            get
            {
                //"Audio and video (most of)|*.flac;*.m4a;*.flv;*.m4v;*.m2ts;*.bdmv;*.wav;*.mpeg;*.mpg;*.mpe;*.mpeg;*.m1s;*.mpa;*.mp2;*.m2a;*.mp2v;*.m2v;*.m2s;*.avi;*.mov;*.qt;*.asf;*.asx;*.wmv;*.wma;*.wmx;*.rm;*.ra;*.ram;*.rmvb;*.mp4;*.3gp;*.ogm;*.mkv;*.ogv;*.ogg;*.oga;*.ogx;*.mp3;*.aac;*.vob|All files (*.*)|*.*"
                var filters = new []
                {
                    "All Media files|" + string.Join(";", VideoFiles.Select(v => "*." + v.Key).ToArray()) + ";" + string.Join(";", AudioFiles.Select(v => "*." + v.Key).ToArray()),
                    "Audio files|" + string.Join(";", AudioFiles.Select(v => "*." + v.Key).ToArray()),
                    "Video files|" + string.Join(";", VideoFiles.Select(v => "*." + v.Key).ToArray()),
                    "Subtitle files|" + string.Join(";", SubFiles.Select(v => "*." + v.Key).ToArray()),
                    "All files (*.*)|*.*"
                };

                var ret = new List<string>(filters);

                foreach (var file in All.OrderBy(o => o.Key))
                {
                    ret.Add(string.Format("{0} (*.{1})|*.{1}", file.Value, file.Key));
                }

                return string.Join("|", ret.ToArray());
            }
        }
    }
}
