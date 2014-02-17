using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MediaPoint.VM;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using MediaPoint.Common.Helpers;

namespace MediaPoint.App.AttachedProperties
{
    public class SliderData
    {
        public Slider Slider { get; set; }
        public Thumb Thumb { get; set; }
        public bool HasBeenDragged { get; set; }
        public object DataContext { get; set; }
    }

    public class TrackbarSlider : DependencyObject
    {
        public static bool GetDragScroll(DependencyObject obj) { return (bool)obj.GetValue(DragScrollProperty); }
        public static void SetDragScroll(DependencyObject obj, bool value) { obj.SetValue(DragScrollProperty, value); }
        public static readonly DependencyProperty DragScrollProperty = DependencyProperty.RegisterAttached("DragScroll", typeof(bool), typeof(TrackbarSlider), new PropertyMetadata
        {
            PropertyChangedCallback = (obj, changeEvent) =>
            {
                if ((bool)changeEvent.NewValue)
                {
                    var slider = (Slider)obj;
                    slider.Loaded += slider_Loaded;
                    slider_Loaded(slider, null);
                }
                else
                {
                    var slider = (Slider)obj;
                    DetachHandlers(slider, null);
                    var existing = _sliders.FindIndex(s => s.Slider == slider);
                    if (existing != -1)
                    {
                        _sliders.RemoveAt(existing);
                    }
                }
            }
        });

        private static List<SliderData> _sliders = new List<SliderData>();

        static void slider_Loaded(object sender, RoutedEventArgs e)
        {
            var slider = (Slider)sender;
            var ok = slider.ApplyTemplate();

            if (slider.Template == null) return;
            var thumb = (Thumb)slider.Template.FindName("Thumb", slider);

            if (!_sliders.Any(s => s.Slider == slider))
            {
                _sliders.Add(new SliderData { Slider = slider, Thumb = thumb, HasBeenDragged = false, DataContext = slider.DataContext });
                var handlers = ReflectionHelper.GetRoutedEventHandlers(thumb, UIElement.MouseLeftButtonDownEvent);
                if (handlers != null)
                {
                    foreach (var handler in handlers)
                        thumb.MouseLeftButtonDown -= (MouseButtonEventHandler)handler.Handler; // detach handlers
                }
            }
            else
            {
                return;
            }

            DetachHandlers(slider, thumb);
            AttachHandlers(slider, thumb);

            slider.MouseMove -= SliderOnMouseMove;
            slider.MouseMove += SliderOnMouseMove;
            thumb.MouseLeftButtonDown -= SliderOnMouseMove;
            thumb.MouseLeftButtonDown += SliderOnMouseMove;
            thumb.Tag = slider;
            
        }

        static TrackbarSlider()
        {
            Thread sliderUpdater = new Thread((ThreadStart)(()=>{
                while (true)
                {
                    Thread.Sleep(200);
                    //foreach (var sliderData in _sliders)
                    //{
                    //    if (sliderData.HasBeenDragged)
                    //    {
                    //        SetSliderState(sliderData.Slider, false);
                    //        var dc = sliderData.DataContext as Player;
                    //        if (dc == null && sliderData.DataContext is Main)
                    //            dc = (sliderData.DataContext as Main).Player;
                    //        if (dc == null) continue;

                    //        var newPos = (long)(double)sliderData.Slider.Dispatcher.Invoke((Func<double>)delegate { return (double)sliderData.Slider.GetValue(Slider.ValueProperty); }, DispatcherPriority.Background); ;
                    //        if (Math.Abs(dc.MediaPosition - newPos) > dc.MediaDuration / 1000)
                    //        {
                    //            dc.MediaPosition = newPos;
                    //        }
                    //    }
                    //}
                }
            }));
            sliderUpdater.IsBackground = true;
            sliderUpdater.Start();
        }

        private static void DetachHandlers(Slider slider, Thumb thumb)
        {
            if (thumb == null)
                thumb = (Thumb)slider.Template.FindName("Thumb", slider);

            slider.DataContextChanged -= slider_DataContextChanged;
            slider.RemoveHandler(Slider.MouseLeaveEvent, new MouseEventHandler(slider_MouseLeave));
            slider.RemoveHandler(Slider.MouseLeftButtonDownEvent, new MouseButtonEventHandler(slider_PreviewMouseLeftButtonDown));
            slider.RemoveHandler(Slider.MouseLeftButtonUpEvent, new MouseButtonEventHandler(slider_PreviewMouseLeftButtonUp));
            thumb.RemoveHandler(Thumb.MouseLeftButtonDownEvent, new MouseButtonEventHandler(slider_PreviewMouseLeftButtonDown));
            thumb.RemoveHandler(Thumb.MouseLeftButtonUpEvent, new MouseButtonEventHandler(slider_PreviewMouseLeftButtonUp));
        }

        static void slider_MouseLeave(object sender, MouseEventArgs e)
        {
            foreach (var sliderData in _sliders)
            {
                sliderData.Slider.Tag = null;
                MouseDownHelper.SetIsMouseDown(sliderData.Slider, false);
                MouseDownHelper.SetIsMouseLeftButtonDown(sliderData.Slider, false);
            }
        }

        static void slider_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            foreach (var sliderData in _sliders)
            {
                if (sliderData.Slider == sender)
                {
                    sliderData.DataContext = e.NewValue;
                }
            }
        }

        private static void AttachHandlers(Slider slider, Thumb thumb)
        {
            if (thumb == null)
                thumb = (Thumb)slider.Template.FindName("Thumb", slider);

            slider.DataContextChanged += slider_DataContextChanged;
            slider.AddHandler(Slider.MouseLeaveEvent, new MouseEventHandler(slider_MouseLeave));
            slider.AddHandler(Slider.MouseLeftButtonDownEvent, new MouseButtonEventHandler(slider_PreviewMouseLeftButtonDown), true);
            slider.AddHandler(Slider.MouseLeftButtonUpEvent, new MouseButtonEventHandler(slider_PreviewMouseLeftButtonUp), true);
            thumb.AddHandler(Thumb.MouseLeftButtonDownEvent, new MouseButtonEventHandler(slider_PreviewMouseLeftButtonDown), true);
            thumb.AddHandler(Thumb.MouseLeftButtonUpEvent, new MouseButtonEventHandler(slider_PreviewMouseLeftButtonUp), true);
        }

        static void SetSliderState(FrameworkElement slider, bool dragged)
        {
            foreach (var sliderData in _sliders)
            {
                if (sliderData.Slider == slider)
                {
                    sliderData.HasBeenDragged = dragged;
                }
            }
        }

        static void slider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("slider_PreviewMouseLeftButtonUp " + sender.GetType().Name);
            var slider = (sender as FrameworkElement);
            MouseDownHelper.SetIsMouseDown(slider, false);
            MouseDownHelper.SetIsMouseLeftButtonDown(slider, false);
            SetPlaying(slider);
        }

        private static void SetPlaying(FrameworkElement slider)
        {
            var dc = GetDataContext(slider);
            if (dc is Player)
            {
                dc.IsTrackbarBeingMoved = false;
                var p = GetIsPaused(slider);
                if (!p.HasValue || (p.HasValue && p == false))
                {
                    dc.Play();
                }
                SetIsPaused(slider, null);                
            }
        }

        private static Player GetDataContext(FrameworkElement element)
        {
            foreach (var sliderData in _sliders)
            {
                if (sliderData.Slider == element)
                {
                    return (sliderData.DataContext as Main).Player;
                }
            }
            return null;
        }

        static void slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Thumb)
            {
                MouseButtonEventArgs args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left);
                args.RoutedEvent = Thumb.MouseLeftButtonUpEvent;
                (sender as Thumb).RaiseEvent(args);
                return;
            }

            Debug.WriteLine("slider_PreviewMouseLeftButtonDown " + sender.GetType().Name);
            var slider = (sender as FrameworkElement);
            SetSliderState(slider, true);
            MouseDownHelper.SetIsMouseDown(slider, true);
            MouseDownHelper.SetIsMouseLeftButtonDown(slider, true); 
            if (slider.Tag == null)
            {
                SetPaused(slider);
            }
        }

        private static void SetPaused(FrameworkElement slider)
        {
            var dc = GetDataContext(slider);
            if (dc is Player)
            {
                dc.IsTrackbarBeingMoved = true;
                SetIsPaused(slider, dc.IsPaused);
                dc.Pause();
            }
        }

        static void SetIsPaused(object sender, bool? val)
        {
            var ctrl = (sender as FrameworkElement);
            if (sender is Thumb)
            {
                ((Slider)ctrl.Tag).Tag = val;
            }
            else
            {
                ctrl.Tag = val;
            }
        }

        static bool? GetIsPaused(object sender)
        {
            var ctrl = (sender as FrameworkElement);
            if (sender is Thumb)
            {
                return (bool?)(((Slider)ctrl.Tag).Tag);
            }
            else
            {
                return (bool?)ctrl.Tag;
            }
        }

        private static void SliderOnMouseMove(object obj2, MouseEventArgs mouseEvent)
        {

            if (mouseEvent.LeftButton == MouseButtonState.Pressed)
            {
                var slider = (obj2 as Slider);
                if (slider == null) slider = (obj2 as Thumb).Tag as Slider;

                if (!MouseDownHelper.GetIsMouseLeftButtonDown(slider))
                {
                    slider_PreviewMouseLeftButtonDown(slider, null);
                }
                else
                {
                    var x = mouseEvent.GetPosition(slider);
                    if (slider.Orientation == Orientation.Horizontal)
                    {
                        var v = x.X / slider.ActualWidth;
                        if (v < 0) v = 0.0;
                        if (v > 1) v = 1.0;

                        slider.SetValue(Slider.ValueProperty, slider.Maximum * v);
                        SetSliderState(slider, true);
                    }
                    else
                    {
                        var v = x.Y / slider.ActualHeight;
                        if (v < 0) v = 0.0;
                        if (v > 1) v = 1.0;
                        slider.SetValue(Slider.ValueProperty, slider.Maximum * v);
                        SetSliderState(slider, true);
                    }
                }
            }
        }
    }

}
