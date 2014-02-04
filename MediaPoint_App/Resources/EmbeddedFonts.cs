using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace MediaPoint.App.Resources
{
    public static class EmbeddedFonts
    {
        static readonly FontFamily[] _fonts = new FontFamily[]
        {
            new FontFamily(new Uri("pack://application:,,,/"), "./Resources/#Buxton Sketch")
        };

        public static FontFamily[] Fonts
        {
            get { return _fonts; }
        }
    }
}
