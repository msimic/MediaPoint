using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace MediaPoint.App.AttachedProperties
{
    public class ThreatAsInputControl : DependencyObject
    {
        public static readonly DependencyProperty TreatProperty = DependencyProperty.RegisterAttached("Treat", typeof(bool), typeof(ThreatAsInputControl), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));
        public static bool GetTreat(DependencyObject obj) { return (bool)obj.GetValue(TreatProperty); }
        public static void SetTreat(DependencyObject obj, bool value) { obj.SetValue(TreatProperty, value); }
    }
}
