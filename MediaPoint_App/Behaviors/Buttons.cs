using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace MediaPoint.App.Behaviors
{
  public class Buttons
  {
    #region Image dependency property

    /// <summary>
    /// An attached dependency property which provides an
    /// <see cref="ImageSource" /> for arbitrary WPF elements.
    /// </summary>
    public static readonly DependencyProperty ImageProperty;

    /// <summary>
    /// Gets the <see cref="ImageProperty"/> for a given
    /// <see cref="DependencyObject"/>, which provides an
    /// <see cref="ImageSource" /> for arbitrary WPF elements.
    /// </summary>
    public static ImageSource GetImage(DependencyObject obj)
    {
      return (ImageSource) obj.GetValue(ImageProperty);
    }

    /// <summary>
    /// Sets the attached <see cref="ImageProperty"/> for a given
    /// <see cref="DependencyObject"/>, which provides an
    /// <see cref="ImageSource" /> for arbitrary WPF elements.
    /// </summary>
    public static void SetImage(DependencyObject obj, ImageSource value)
    {
      obj.SetValue(ImageProperty, value);
    }

    #endregion


	#region NormalImage dependency property

	/// <summary>
	/// An attached dependency property which provides an
	/// <see cref="ImageSource" /> for arbitrary WPF elements.
	/// </summary>
	public static readonly DependencyProperty NormalImageProperty;

	/// <summary>
	/// Gets the <see cref="ImageProperty"/> for a given
	/// <see cref="DependencyObject"/>, which provides an
	/// <see cref="ImageSource" /> for arbitrary WPF elements.
	/// </summary>
	public static ImageSource GetNormalImage(DependencyObject obj)
	{
		return (ImageSource)obj.GetValue(NormalImageProperty);
	}

	/// <summary>
	/// Sets the attached <see cref="ImageProperty"/> for a given
	/// <see cref="DependencyObject"/>, which provides an
	/// <see cref="ImageSource" /> for arbitrary WPF elements.
	/// </summary>
	public static void SetNormalImage(DependencyObject obj, ImageSource value)
	{
		obj.SetValue(NormalImageProperty, value);
	}

	#endregion

	#region HoverImage dependency property

	/// <summary>
	/// An attached dependency property which provides an
	/// <see cref="ImageSource" /> for arbitrary WPF elements.
	/// </summary>
	public static readonly DependencyProperty HoverImageProperty;

	/// <summary>
	/// Gets the <see cref="ImageProperty"/> for a given
	/// <see cref="DependencyObject"/>, which provides an
	/// <see cref="ImageSource" /> for arbitrary WPF elements.
	/// </summary>
	public static ImageSource GetHoverImage(DependencyObject obj)
	{
		return (ImageSource)obj.GetValue(HoverImageProperty);
	}

	/// <summary>
	/// Sets the attached <see cref="ImageProperty"/> for a given
	/// <see cref="DependencyObject"/>, which provides an
	/// <see cref="ImageSource" /> for arbitrary WPF elements.
	/// </summary>
	public static void SetHoverImage(DependencyObject obj, ImageSource value)
	{
		obj.SetValue(HoverImageProperty, value);
	}

	#endregion

	private static void HoverImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var button = d as Button;
		button.MouseEnter -= button_MouseEnter;
		button.MouseLeave -= button_MouseLeave;
		button.MouseEnter += button_MouseEnter;
		button.MouseLeave += button_MouseLeave;
	}

	static void button_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
	{
		SetImage(sender as DependencyObject, GetNormalImage(sender as DependencyObject));
	}

	static void button_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
	{
		SetImage(sender as DependencyObject, GetHoverImage(sender as DependencyObject));
	}

	
    static Buttons()
    {
      //register attached dependency property
		var metadata = new FrameworkPropertyMetadata((ImageSource)null);
      ImageProperty = DependencyProperty.RegisterAttached("Image",
                                                          typeof (ImageSource),
                                                          typeof (Buttons), metadata);
	  //register attached dependency property
	  var metadata2 = new FrameworkPropertyMetadata((ImageSource)null, HoverImageChanged);
	  HoverImageProperty = DependencyProperty.RegisterAttached("HoverImage",
														  typeof(ImageSource),
														  typeof(Buttons), metadata2);
	  //register attached dependency property
	  var metadata3 = new FrameworkPropertyMetadata((ImageSource)null);
	  NormalImageProperty = DependencyProperty.RegisterAttached("NormalImage",
														  typeof(ImageSource),
														  typeof(Buttons), metadata3);
    }
  }
}