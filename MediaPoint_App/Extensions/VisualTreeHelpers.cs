using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MediaPoint.App.Extensions
{
    public static class RemoveChildHelper
    {
        public static int RemoveChild(this DependencyObject parent, UIElement child)
        {
            var panel = parent as Panel;
            if (panel != null)
            {
                int index = panel.Children.IndexOf(child);
                panel.Children.Remove(child);
                return index;
            }

            var decorator = parent as Decorator;
            if (decorator != null)
            {
                if (decorator.Child == child)
                {
                    decorator.Child = null;
                }
                return -1;
            }

            var contentPresenter = parent as ContentPresenter;
            if (contentPresenter != null)
            {
                if (contentPresenter.Content == child)
                {
                    contentPresenter.Content = null;
                }
                return -1;
            }

            var contentControl = parent as ContentControl;
            if (contentControl != null)
            {
                if (contentControl.Content == child)
                {
                    contentControl.Content = null;
                }
                return -1;
            }

            return -1;

            // maybe more
        }

        public static void AddChild(this DependencyObject parent, UIElement child, int index = -1)
        {
            var panel = parent as Panel;
            if (panel != null)
            {
                int iindex = index != -1 ? index : panel.Children.Count;
                panel.Children.Insert(iindex, child);
            }

            var decorator = parent as Decorator;
            if (decorator != null)
            {
                decorator.Child = child;
            }

            var contentPresenter = parent as ContentPresenter;
            if (contentPresenter != null)
            {
                contentPresenter.Content = child;
            }

            var contentControl = parent as ContentControl;
            if (contentControl != null)
            {
                contentControl.Content = child;
            }

        }
    }
}
