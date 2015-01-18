using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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
            var img = new BitmapImage(uri);
            return new Image { Source = img, Margin=new Thickness(2),
                Width=img.Width,
                Height=img.Height,
                ToolTip = parameter.ToString(),
                HorizontalAlignment=System.Windows.HorizontalAlignment.Center, 
                VerticalAlignment=System.Windows.VerticalAlignment.Center };
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}
