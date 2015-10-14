using MediaPoint.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace MediaPoint.Converters
{
    public class PositionToRectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length != 6 || values[4] == DependencyProperty.UnsetValue || values[5] == DependencyProperty.UnsetValue)
                return new System.Windows.Rect();

            double rectWidth = System.Convert.ToDouble(values[0]);
            double canvasWidth = System.Convert.ToDouble(values[1]);
            double rectHeight = System.Convert.ToDouble(values[2]);
            double canvasHeight = System.Convert.ToDouble(values[3]);
            double left = System.Convert.ToDouble(values[4]);
            double top = System.Convert.ToDouble(values[5]);

            if (double.IsNaN(left) || double.IsNaN(top)) return new System.Windows.Rect();

            return new System.Windows.Rect(left / canvasWidth, top / canvasHeight, rectWidth / canvasWidth, rectHeight / canvasHeight);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
