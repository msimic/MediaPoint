using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MediaPoint.App.AttachedProperties
{
	public class TrackbarSlider : DependencyObject
	{
		public static bool GetDragScroll(DependencyObject obj) { return (bool)obj.GetValue(DragScrollProperty); }
		public static void SetDragScroll(DependencyObject obj, bool value) { obj.SetValue(DragScrollProperty, value); }
		public static readonly DependencyProperty DragScrollProperty = DependencyProperty.RegisterAttached("DragScroll", typeof(bool), typeof(TrackbarSlider), new PropertyMetadata
		{
			PropertyChangedCallback = (obj, changeEvent) =>
			{
				var slider = (Slider)obj;
				if ((bool)changeEvent.NewValue)
				{
					slider.MouseMove -= SliderOnMouseMove;
					slider.MouseMove += SliderOnMouseMove;
				}
			}
		});

		private static void SliderOnMouseMove(object obj2, MouseEventArgs mouseEvent)
		{
			if (mouseEvent.LeftButton == MouseButtonState.Pressed)
			(obj2 as Slider).RaiseEvent(new MouseButtonEventArgs(mouseEvent.MouseDevice, mouseEvent.Timestamp, MouseButton.Left)
			{
				RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
				Source = mouseEvent.Source,
			});
		}
	}

}
