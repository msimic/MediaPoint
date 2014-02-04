using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using MediaPoint.Common.TaskbarNotification;
using System.Windows.Media;

namespace MediaPoint.Controls
{
	/// <summary>
	/// Interaction logic for FancyToolTip.xaml
	/// </summary>
	public partial class ToolTip
	{
	  #region InfoText dependency property

	  /// <summary>
	  /// The tooltip details.
	  /// </summary>
	  public static readonly DependencyProperty InfoTextProperty =
	      DependencyProperty.Register("InfoText",
	                                  typeof (string),
	                                  typeof (ToolTip),
	                                  new FrameworkPropertyMetadata(""));

	  /// <summary>
	  /// A property wrapper for the <see cref="InfoTextProperty"/>
	  /// dependency property:<br/>
	  /// The tooltip details.
	  /// </summary>
	  public string InfoText
	  {
	    get { return (string) GetValue(InfoTextProperty); }
	    set { SetValue(InfoTextProperty, value); }
	  }

	  #endregion

		public ToolTip()
		{
			this.InitializeComponent();
		}
	}
}