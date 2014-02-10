using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFSoundVisualizationLib;
using MediaPoint.Controls.Extensions;

namespace MediaPoint.Controls
{
    public class Visualizations : ContentControl
    {
        static Visualizations()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Visualizations), new FrameworkPropertyMetadata(typeof(Visualizations)));
        }

        public Visualizations()
        {
            
        }

        public IEnumerable<SpectrumAnalyzer> TryFindSpectrumAnalyzer()
        {
            if (this.VisualChildrenCount == 0)
            {
                this.BeginInit();
                this.EndInit();
                this.ApplyTemplate();
            }
            return this.FindVisualChildren<SpectrumAnalyzer>();
        }
    }

}
