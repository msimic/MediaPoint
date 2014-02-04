using System;
using System.Collections.Generic;
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
	public class HidingOverlay : ContentControl
	{
		static HidingOverlay()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(HidingOverlay), new FrameworkPropertyMetadata(typeof(HidingOverlay)));
		}
	}
}
