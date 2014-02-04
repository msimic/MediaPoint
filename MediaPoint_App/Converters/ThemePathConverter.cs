using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using MediaPoint.App.Themes;
using System.IO;

namespace MediaPoint.Converters
{
	public class ThemePathConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{			
			var ret = value.ToString() + parameter.ToString();

			if (!File.Exists(ret))
			{
				return "D:\\x.png";
			}
			else
			{
				return ret;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}
