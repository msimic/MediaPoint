using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace MediaPoint.Converters
{
	public class NotNullToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
            bool invert = parameter != null ? bool.Parse(parameter.ToString()) : false;

            if (invert)
            {
                if (value != null)
                {
                    value = null;
                }
                else
                {
                    value = new object();
                }
            }

		    return value != null ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
