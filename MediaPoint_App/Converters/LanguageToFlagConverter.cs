using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MediaPoint.Converters
{
    public class LanguageToFlagConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
            if (value == null) return null;

            var uri = new Uri("pack://application:,,,/MediaPoint;component/Images/countryflags/" + value.ToString() + ".gif", UriKind.RelativeOrAbsolute);
            return new BitmapImage(uri);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}
