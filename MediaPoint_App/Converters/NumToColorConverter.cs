using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace MediaPoint.Converters
{
	public class NumToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
            string p = (string)parameter;
            var pp = p.Split('-');
            int min = int.Parse(pp[0]);
            int max = int.Parse(pp[1]);
            int val = (int)value;

            Brush ret = Brushes.Transparent;
            if (val < min) { ret = new SolidColorBrush(Color.FromArgb(127, 127, 0, 0)); }
            if (val > max) { ret = new SolidColorBrush(Color.FromArgb(127, 0, 127, 0)); }
            return ret;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}
