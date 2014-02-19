using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace MediaPoint.App.AttachedProperties
{
    public static class PanelBringToFrontOnClick
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled",
        typeof(bool), typeof(PanelBringToFrontOnClick), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnNotifyPropertyChanged)));

        public static void SetIsEnabled(UIElement element, bool value)
        {
            element.SetValue(IsEnabledProperty, value);
        }

        public static bool GetIsEnabled(UIElement element)
        {
            return (bool)element.GetValue(IsEnabledProperty);
        }

        static Dictionary<UIElement, int> _panels = new Dictionary<UIElement, int>();

        private static void OnNotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as UIElement;
            if (element != null && e.NewValue != null)
            {
                if ((bool)e.NewValue)
                {
                    _panels[element] = (int)element.GetValue(Panel.ZIndexProperty);
                    Register(element);
                }
                else
                {
                    UnRegister(element);
                    _panels.Remove(element);
                }
            }
        }

        private static void Register(UIElement element)
        {
            element.PreviewMouseLeftButtonDown += element_PreviewMouseLeftButtonUp;
        }

        private static void UnRegister(UIElement element)
        {
            element.PreviewMouseLeftButtonDown -= element_PreviewMouseLeftButtonUp;
        }

        static void element_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var element = sender as UIElement;
            if (element != null)
            {
                var zindexes = _panels.OrderBy(v => v.Value).ToList();
                int min = zindexes.First().Value;
                int max = zindexes.Last().Value;
                int current = _panels[element];
                int indexOfCurrent = zindexes.FindIndex(p => p.Value == current);
                for (int i = zindexes.Count - 1; i > indexOfCurrent; i--)
                {
                    _panels[zindexes[i].Key] = zindexes[i - 1].Value;
                    zindexes[i].Key.SetValue(Panel.ZIndexProperty, zindexes[i - 1].Value);
                }
                zindexes.RemoveAt(indexOfCurrent);
                zindexes.Add(new KeyValuePair<UIElement, int>(element, max));
                _panels[element] = max;
                element.SetValue(Panel.ZIndexProperty, max);
            }
        }
    }
}