using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MediaPoint.Controls.Extensions;

namespace MediaPoint.Converters
{
    public class FullscreenPaddingConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
            var w = value as Window;

            if (w == null) return 0;

            Size actual = new Size(w.ActualWidth, w.ActualHeight);
            var source = PresentationSource.FromVisual(w);
	        Matrix transformFromDevice = source.CompositionTarget.TransformFromDevice;
            Vector monitorPosition;
            Size monitor = MediaPoint.Controls.Extensions.WindowExtensions.MonitorSize(ref w, transformFromDevice, out monitorPosition);
            //w.Visibility = Visibility.Collapsed;
            //w.Dispatcher.BeginInvoke((Action)(() =>
            //{
               
            //}), System.Windows.Threading.DispatcherPriority.ContextIdle);
            var s = actual.Difference(monitor);
            //var ret2 = new Thickness(s.Width / 2, s.Height / 2, s.Width / 2, s.Height / 2);
            System.Diagnostics.Debug.WriteLine("Padding " + s.Width / 2);
            return s.Width / 2;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return !(bool)value;
		}
	}
}
