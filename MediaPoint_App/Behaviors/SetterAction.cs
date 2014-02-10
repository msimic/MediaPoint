using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;

namespace MediaPoint.App.Behaviors
{
    public class SetterAction : TargetedTriggerAction<FrameworkElement>
    {
        public DependencyProperty Property { get; set; }
        public Object Value { get; set; }



        public Popup Popup
        {
            get { return (Popup)GetValue(PopupProperty); }
            set { SetValue(PopupProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Popup.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PopupProperty =
            DependencyProperty.Register("Popup", typeof(Popup), typeof(SetterAction), new UIPropertyMetadata(null));

        

        protected override void Invoke(object parameter)
        {
            Popup.SetValue(Property, Convert.ChangeType(Value, Property.PropertyType));
            Popup.Focus();
            if (parameter is RoutedEventArgs)
            {
                (parameter as RoutedEventArgs).Handled = true;
            }
        }
    }
}
