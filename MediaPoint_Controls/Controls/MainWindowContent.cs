using System.Windows;
using System.Windows.Controls;

namespace MediaPoint.Controls
{
	public class MainWindowContent : ContentControl
	{
		static MainWindowContent()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(MainWindowContent), new FrameworkPropertyMetadata(typeof(MainWindowContent)));
		}
	}
}
