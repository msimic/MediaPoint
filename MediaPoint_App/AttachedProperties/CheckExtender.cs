using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace MediaPoint.App.AttachedProperties
{
	public class CheckExtender
	{
		public static readonly DependencyProperty IsCheckedProperty =
		  DependencyProperty.RegisterAttached("IsChecked",
											  typeof(bool),
											  typeof(CheckExtender),
											  new PropertyMetadata(OnChanged));

		public static bool GetIsChecked(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsCheckedProperty);
		}
		public static void SetIsChecked(DependencyObject obj, bool value)
		{
			obj.SetValue(IsCheckedProperty, value);
		}

		private static void OnChanged(DependencyObject o,
									  DependencyPropertyChangedEventArgs args)
		{
			ToggleButton tb = o as ToggleButton;
            if (null != tb && tb.IsChecked != (bool)args.NewValue)
            {
                tb.Dispatcher.BeginInvoke((Action) (() =>
                {
                    tb.IsChecked = (bool) args.NewValue;
                }), DispatcherPriority.ApplicationIdle);
            }
		}


	}
}
