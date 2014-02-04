using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MediaPoint.MVVM;
using Path = System.Windows.Shapes.Path;

namespace MediaPoint.VM.Model
{
    public class FontObject : ViewModel
    {
        public FontObject(FontFamily font)
        {
            Font = font;
        }

        public FontFamily Font { get; set; }

        public object BuildTextBlock
        {
            get { return new TextBlock { Text = Font.Source }; }
        }

        public object BuildStyledFontName
        {
            get
            {
                return new Path { Data = GetStyledFontGeometryUsingCache(), Fill = Brushes.White};
            }
        }

        public Geometry GetStyledFontGeometryUsingCache()
        {
            Geometry geo;
            string geoString;

            lock (_fontGeometryCache2)
                if (_fontGeometryCache2.TryGetValue(Font.Source, out geo))
                    return geo;

            lock (_fontGeometryCache)
                if (_fontGeometryCache.TryGetValue(Font.Source, out geoString))
                {
                    return _fontGeometryCache2[Font.Source] = Geometry.Parse(geoString);
                }

            lock (FontGeometryBuildLock)
            {
                geo = BuildStyledFontGeometry();

                if (geo != null) {
                    lock (_fontGeometryCache)
                    {
                        _fontGeometryCache[Font.Source] = geo.ToString(CultureInfo.InvariantCulture);
                    }
                    lock (_fontGeometryCache2)
                    {
                        _fontGeometryCache2[Font.Source] = geo;
                    }
                }
            }
            return geo;
        }

        public static int GeometryCount { get { return _fontGeometryCache.Count; } }

        private static Dictionary<string, string> _fontGeometryCache = new Dictionary<string, string>();
        private static readonly Dictionary<string, Geometry> _fontGeometryCache2 = new Dictionary<string, Geometry>();

        static readonly object FontGeometryBuildLock = new object();

        public static void SaveDictionaryToStream(Stream stream)
        {
            Serialize(_fontGeometryCache, stream);
        }

        public static void ReadDictionaryFromStream(Stream stream, bool initGeometries = true)
        {
            _fontGeometryCache = Deserialize(stream);
            if (initGeometries)
            {
                foreach (var kv in _fontGeometryCache)
                {
                    var fg = Geometry.Parse(kv.Value);
                    fg.Freeze();
                    _fontGeometryCache2[kv.Key] = fg;
                }
            }
        }

        public static void Serialize(Dictionary<string, string> dictionary, Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(dictionary.Count);
            foreach (var kvp in dictionary)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
            writer.Flush();
        }

        //public static void Serialize(Dictionary<string, Geometry> dictionary, Stream stream)
        //{
        //    BinaryFormatter formatter = new BinaryFormatter();
        //    try
        //    {
        //        formatter.Serialize(stream, _fontGeometryCache2);
        //    }
        //    catch (SerializationException e)
        //    {
        //        Console.WriteLine("Failed to serialize. Reason: " + e.Message);
        //        throw;
        //    }
        //}

        //public static Dictionary<string, Geometry> Deserialize(Stream stream)
        //{
        //    try
        //    {
        //        BinaryFormatter formatter = new BinaryFormatter();

        //        // Deserialize the hashtable from the file and  
        //        // assign the reference to the local variable.
        //        return (Dictionary<string, Geometry>)formatter.Deserialize(stream);
        //    }
        //    catch (SerializationException e)
        //    {
        //        Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
        //        throw;
        //    }
        //}

        public static Dictionary<string, string> Deserialize(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            int count = reader.ReadInt32();
            var dictionary = new Dictionary<string, string>(count);
            for (int n = 0; n < count; n++)
            {
                var key = reader.ReadString();
                var value = reader.ReadString();
                dictionary.Add(key, value);
            }
            return dictionary;
        }

        private PathGeometry BuildStyledFontGeometry()
        {
            var typeface = new Typeface(Font,
            FontStyles.Normal,
            FontWeights.Normal,
            FontStretches.Normal);

            GlyphTypeface glyphTypeface;
            if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
                return null;

			var glyphIndexes = new ushort[Font.Source.Length];
			var advanceWidths = new double[Font.Source.Length];
			for (int n = 0; n < Font.Source.Length; n++)
			{
				if (glyphTypeface.CharacterToGlyphMap.ContainsKey(Font.Source[n]))
				{
					ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[Font.Source[n]];
					glyphIndexes[n] = glyphIndex;

					double width = glyphTypeface.AdvanceWidths[glyphIndex] * 12;
					advanceWidths[n] = width;
				}
			}

            var run = new GlyphRun(glyphTypeface, 0, false, 12, glyphIndexes, new Point(0,12), advanceWidths, null, null, null, null, null, null);
            PathGeometry ret = PathGeometry.CreateFromGeometry(run.BuildGeometry());
            ret.Freeze();
            return ret;
        }

    }
}
