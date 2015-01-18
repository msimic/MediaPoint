using MediaPoint.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MediaPoint.Converters
{
    public class DefaultConverter : IMultiValueConverter
	{
        public FrameworkElement FrameElem = new FrameworkElement();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
            if (values.Length != 3 || values[1] is EvernoteTagControl == false) return null;

            var val = (values[0]);
            var tgc = (values[1] as EvernoteTagControl).ConverterInstance as IValueConverter;

            return tgc.Convert(val, targetType, values[2], culture);
		}

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
