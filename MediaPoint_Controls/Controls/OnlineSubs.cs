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

namespace MediaPoint.Controls
{
    public class OnlineSubs : ContentControl
    {
        static OnlineSubs()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OnlineSubs), new FrameworkPropertyMetadata(typeof(OnlineSubs)));
        }

        public OnlineSubs()
        {
        }
    }

}
