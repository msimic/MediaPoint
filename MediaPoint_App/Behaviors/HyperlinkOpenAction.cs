using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Interactivity;
using System.Windows.Navigation;

namespace MediaPoint.App.Behaviors
{
    public class HyperlinkOpenAction : TargetedTriggerAction<FrameworkElement>
    {
        public DependencyProperty Property { get; set; }
        
        protected override void Invoke(object parameter)
        {
            RequestNavigateEventArgs e = (RequestNavigateEventArgs)parameter;
            Uri u = (AssociatedObject as Hyperlink).NavigateUri;
            
            var w = Window.GetWindow(AssociatedObject as Hyperlink) as Window1;
            if (FullScreenBehavior.GetIsFullScreen(w))
            {
                FullScreenBehavior.SetIsFullScreen(w, false);
            }

            Process.Start(new ProcessStartInfo(u.AbsoluteUri));
            e.Handled = true;
        }
    }
}
