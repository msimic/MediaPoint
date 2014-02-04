using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MediaPoint.App.AttachedProperties
{
	public class Autohide
	{
		public static bool GetNormalMode(DependencyObject obj) { return (bool)obj.GetValue(NormalModeProperty); }
		public static void SetNormalMode(DependencyObject obj, bool value) { obj.SetValue(NormalModeProperty, value); }

		public static readonly DependencyProperty NormalModeProperty = DependencyProperty.RegisterAttached("NormalMode",
		                                                                                                   typeof (bool),
		                                                                                                   typeof (
		                                                                                                   	Autohide),
		                                                                                                   new PropertyMetadata
		                                                                                                   	(true));

		public static bool GetFullScreen(DependencyObject obj) { return (bool)obj.GetValue(FullScreenProperty); }
		public static void SetFullScreen(DependencyObject obj, bool value) { obj.SetValue(FullScreenProperty, value); }

		public static readonly DependencyProperty FullScreenProperty = DependencyProperty.RegisterAttached("FullScreen",
																										   typeof(bool),
																										   typeof(
																											Autohide),
																										   new PropertyMetadata
																											(true));
	}
}
