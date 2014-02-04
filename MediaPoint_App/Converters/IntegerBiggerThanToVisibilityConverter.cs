using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace MediaPoint.Converters
{
    public class IntegerBiggerThanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
		    int v;
		    if (int.TryParse(value.ToString(), out v))
		    {
                int v2;
                if (int.TryParse(parameter.ToString(), out v2))
                {
                    
                }
		        return v > v2 ? Visibility.Visible : Visibility.Collapsed;
		    }
		    return Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
