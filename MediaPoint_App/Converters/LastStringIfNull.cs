using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MediaPoint.Converters
{
	public class LastStringIfNull : IValueConverter
	{
        private static object _last = null;
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
            if (value != null && (string)value!="") _last = value;
            return value == null ? _last : value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value;
		}
	}
}
