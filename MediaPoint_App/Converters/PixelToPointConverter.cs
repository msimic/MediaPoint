using MediaPoint.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace MediaPoint.Converters
{
    public class PixelToPointConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Any(va => va == DependencyProperty.UnsetValue))
                return 0;

            double v = (double)values[0];
            double r = (double)values[1];
            MediaUriElement mp = (MediaUriElement)values[2];

            if (v == 0 || r == 0 || mp == null) return 0;

            double video = (string)parameter == "width" ? mp.MediaUriPlayer.NaturalVideoWidth : mp.MediaUriPlayer.NaturalVideoHeight;
            if (video == 0) return 0;

            double val = v * (r / video);
            return val;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
