using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MediaPoint.Common.Helpers;
using Xceed.Wpf.Toolkit.Primitives;

namespace MediaPoint.App.AttachedProperties
{
    public class InputBindingCommandSetter
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command",
            typeof(ICommand), typeof(InputBindingCommandSetter), new PropertyMetadata(new PropertyChangedCallback(CommandChanged)));

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetCommand(d as InputBinding, (ICommand)e.NewValue);
        }

        public static ICommand GetCommand(InputBinding element)
        {
            return (ICommand)element.GetValue(CommandProperty);
        }

        public static void SetCommand(InputBinding element, ICommand value)
        {
            element.Command = value;
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached("CommandParameter",
            typeof(object), typeof(InputBindingCommandSetter), new PropertyMetadata(new PropertyChangedCallback(CommandParameterChanged)));

        private static void CommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetCommandParameter(d as InputBinding, (object)e.NewValue);
        }

        public static object GetCommandParameter(InputBinding element)
        {
            return element.GetValue(CommandParameterProperty);
        }

        public static void SetCommandParameter(InputBinding element, object value)
        {
            element.CommandParameter = value;
        }
    }

    public class ItemsControlDoubleClickMouseInputBinding : DependencyObject
    {
        public ItemsControlDoubleClickMouseInputBinding() { }

        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached("Enabled",
            typeof(bool), typeof(ItemsControlDoubleClickMouseInputBinding), new PropertyMetadata(new PropertyChangedCallback(EnabledChanged)));

        private static void EnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetEnabled(d as ItemsControl, (bool)e.NewValue);
        }

        public static bool GetEnabled(ItemsControl element)
        {
            return (bool)element.GetValue(EnabledProperty);
        }

        public static void SetEnabled(ItemsControl element, bool value)
        {
            element.SetValue(EnabledProperty, value);

            if (value)
            {
                element.PreviewMouseDoubleClick += element_PreviewMouseDoubleClick;
            }
            else
            {
                element.PreviewMouseDoubleClick -= element_PreviewMouseDoubleClick;
            }
        }

        public static readonly DependencyProperty RestrictToProperty =
            DependencyProperty.RegisterAttached("RestrictTo",
            typeof(string), typeof(ItemsControlDoubleClickMouseInputBinding));

        public static string GetRestrictTo(ItemsControl element)
        {
            return (string)element.GetValue(RestrictToProperty);
        }

        public static void SetRestrictTo(ItemsControl element, string value)
        {
            element.SetValue(RestrictToProperty, value);
        }

        static void element_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ItemsControl control = sender as ItemsControl;

            foreach (InputBinding b in control.InputBindings)
            {
                if (!(b is MouseBinding))
                {
                    continue;
                }

                if (b.Gesture != null
                    && b.Gesture is MouseGesture
                    && ((MouseGesture)b.Gesture).MouseAction == MouseAction.LeftDoubleClick
                    && (b.Command != null && b.Command.CanExecute(b.CommandParameter)))
                {
                    if (control is ListBox)
                    {
                        var fe = e.OriginalSource as UIElement;
                        var li = VisualHelper.TryFindParent<ListBoxItem>(fe);
                        if (li != null) (control as ListBox).SetValue(ListBox.SelectedValueProperty, li.DataContext);
                    }
                    if (b.Command != null) b.Command.Execute(b.CommandParameter);
                    e.Handled = true;
                }
            }
        }

    }
}
