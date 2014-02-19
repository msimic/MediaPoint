using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using MediaPoint.App.Themes;
using System.IO;
using System.Windows;

namespace MediaPoint.Converters
{
	public class BooleanAndConverter : IMultiValueConverter
	{

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Any(v => v == null || v == DependencyProperty.UnsetValue)) return false;
            var boolValues = values.Select(v => (bool)v).ToArray();
            return boolValues.All(v => v == true);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
