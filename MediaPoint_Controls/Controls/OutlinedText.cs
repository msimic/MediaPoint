using System;
using System.Windows.Media;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace MediaPoint.Controls
{

	public class OutlinedText : FrameworkElement, IAddChild
	{
		#region Private Fields

		private Geometry _textGeometry;
		private Geometry _textHighlightGeometry;

		#endregion

		#region Private Methods

		/// <summary>
		/// Invoked when a dependency property has changed. Generate a new FormattedText object to display.
		/// </summary>
		/// <param name="d">OutlineText object whose property was updated.</param>
		/// <param name="e">Event arguments for the dependency property.</param>
		private static void OnOutlineTextInvalidated(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((OutlinedText)d).CreateText();
		}

		#endregion


		#region FrameworkElement Overrides

		/// <summary>
		/// OnRender override draws the geometry of the text and optional highlight.
		/// </summary>
		/// <param name="drawingContext">Drawing context of the OutlineText control.</param>
		protected override void OnRender(DrawingContext drawingContext)
		{
			CreateText();
			
			// Draw the outline based on the properties that are set.
			drawingContext.DrawGeometry(BackColor, new Pen(BackColor, 0), _textHighlightGeometry);
			drawingContext.DrawGeometry(Fill, new Pen(Stroke, StrokeThickness), _textGeometry);
			//drawingContext.DrawRectangle(new SolidColorBrush(Colors.Transparent), new Pen(new SolidColorBrush(Colors.Blue), 1), new Rect(0, 0, this.Width, this.Height));
		}

		/// <summary>
		/// Create the outline geometry based on the formatted text.
		/// </summary>
		public void CreateText()
		{
			FontStyle fontStyle = FontStyles.Normal;
			FontWeight fontWeight = FontWeights.Medium;
			var fontDecoration = new TextDecorationCollection();

			if (Bold == true) fontWeight = FontWeights.Bold;
			if (Italic == true) fontStyle = FontStyles.Italic;
			if (Underline) fontDecoration.Add(TextDecorations.Underline);

			// Create the formatted text based on the properties set.
			FormattedText formattedText = new FormattedText(
				string.IsNullOrEmpty(Text) ? string.Empty : Text.Trim(),
				CultureInfo.GetCultureInfo(System.Threading.Thread.CurrentThread.CurrentUICulture.Name),
				System.Threading.Thread.CurrentThread.CurrentUICulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
				new Typeface(Font, fontStyle, fontWeight, FontStretches.Normal),
				FontSize,
				Brushes.Black // This brush does not matter since we use the geometry of the text. 
				);

			formattedText.TextAlignment = TextAlignment.Center;

			//if (this.Parent is FrameworkElement)
			//{
			//    var parentWidth = GetParentWidth(this);

			//    if (!double.IsNaN(parentWidth))
			//    {
			//        if (this.Parent is System.Windows.Controls.Border)
			//        {
			//            parentWidth -= (this.Parent as System.Windows.Controls.Border).BorderThickness.Left;
			//            parentWidth -= (this.Parent as System.Windows.Controls.Border).BorderThickness.Right;
			//            parentWidth -= (this.Parent as System.Windows.Controls.Border).Padding.Left;
			//            parentWidth -= (this.Parent as System.Windows.Controls.Border).Padding.Right;
			//        }
			//        formattedText.MaxTextWidth = parentWidth - this.Margin.Left - this.Margin.Right;
			//    }
			//}

			formattedText.SetTextDecorations(fontDecoration);

			// Build the geometry object that represents the text.
			_textGeometry = formattedText.BuildGeometry(new Point(0, 0));
			_textHighlightGeometry = formattedText.BuildHighlightGeometry(new Point(0, 0));

			var transform = new TranslateTransform(-1 * _textGeometry.Bounds.X, 0);
			//var enlarge = new ScaleTransform(1.1, 0, 0.5, 0.5); // TranslateTransform(-1 * _textGeometry.Bounds.X, 0);
			if (_textGeometry != null) _textGeometry = Geometry.Combine(_textGeometry, _textGeometry, GeometryCombineMode.Intersect, transform);
			if (_textHighlightGeometry != null) _textHighlightGeometry = Geometry.Combine(_textHighlightGeometry, _textHighlightGeometry, GeometryCombineMode.Intersect, transform);
			//if (_textHighlightGeometry != null) _textHighlightGeometry = Geometry.Combine(_textHighlightGeometry, _textHighlightGeometry, GeometryCombineMode.Union, enlarge);

			//set the size of the custome control based on the size of the text
			//this.MinWidth = formattedText.Width;
			//this.MinHeight = formattedText.Height;
			if (!string.IsNullOrEmpty(Text))
			{
				this.Width = _textGeometry.Bounds.Width; // formattedText.Width;
				this.Height = _textGeometry.Bounds.Height; // formattedText.Height;
			}
		}

		private double GetParentWidth(FrameworkElement control) {
			if (control.Parent is FrameworkElement && double.IsNaN((control.Parent as FrameworkElement).Width))
			{
				return GetParentWidth(control.Parent as FrameworkElement);
			}
			else if (control.Parent is FrameworkElement && !double.IsNaN((control.Parent as FrameworkElement).Width))
			{
				return (control.Parent as FrameworkElement).Width;
			}

			return double.NaN;
		}

		#endregion

		#region DependencyProperties

		/// <summary>
		/// Specifies whether the font should display Bold font weight.
		/// </summary>
		public bool Bold
		{
			get
			{
				return (bool)GetValue(BoldProperty);
			}

			set
			{
				SetValue(BoldProperty, value);
			}
		}

		/// <summary>
		/// Identifies the Bold dependency property.
		/// </summary>
		public static readonly DependencyProperty BoldProperty = DependencyProperty.Register(
			"Bold",
			typeof(bool),
			typeof(OutlinedText),
			new FrameworkPropertyMetadata(
				false,
				FrameworkPropertyMetadataOptions.AffectsRender,
				new PropertyChangedCallback(OnOutlineTextInvalidated),
				null
				)
			);

		/// <summary>
		/// Specifies the brush to use for the fill of the formatted text.
		/// </summary>
		public Brush Fill
		{
			get
			{
				return (Brush)GetValue(FillProperty);
			}

			set
			{
				SetValue(FillProperty, value);
			}
		}

		/// <summary>
		/// Identifies the Fill dependency property.
		/// </summary>
		public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
			"Fill",
			typeof(Brush),
			typeof(OutlinedText),
			new FrameworkPropertyMetadata(
				new SolidColorBrush(Colors.LightSteelBlue),
				FrameworkPropertyMetadataOptions.AffectsRender,
				new PropertyChangedCallback(OnOutlineTextInvalidated),
				null
				)
			);

		/// <summary>
		/// The font to use for the displayed formatted text.
		/// </summary>
		public FontFamily Font
		{
			get
			{
				return (FontFamily)GetValue(FontProperty);
			}

			set
			{
				SetValue(FontProperty, value);
			}
		}

		/// <summary>
		/// Identifies the Font dependency property.
		/// </summary>
		public static readonly DependencyProperty FontProperty = DependencyProperty.Register(
			"Font",
			typeof(FontFamily),
			typeof(OutlinedText),
			new FrameworkPropertyMetadata(
				new FontFamily("Arial"),
				FrameworkPropertyMetadataOptions.AffectsRender,
				new PropertyChangedCallback(OnOutlineTextInvalidated),
				null
				)
			);

		/// <summary>
		/// The current font size.
		/// </summary>
		public double FontSize
		{
			get
			{
				return (double)GetValue(FontSizeProperty);
			}

			set
			{
				SetValue(FontSizeProperty, value);
			}
		}

		/// <summary>
		/// Identifies the FontSize dependency property.
		/// </summary>
		public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
			"FontSize",
			typeof(double),
			typeof(OutlinedText),
			new FrameworkPropertyMetadata(
				 (double)48.0,
				 FrameworkPropertyMetadataOptions.AffectsRender,
				 new PropertyChangedCallback(OnOutlineTextInvalidated),
				 null
				 )
			);


		/// <summary>
		/// Specifies whether the font should display Italic font style.
		/// </summary>
		public bool Italic
		{
			get
			{
				return (bool)GetValue(ItalicProperty);
			}

			set
			{
				SetValue(ItalicProperty, value);
			}
		}

		/// <summary>
		/// Identifies the Italic dependency property.
		/// </summary>
		public static readonly DependencyProperty ItalicProperty = DependencyProperty.Register(
			"Italic",
			typeof(bool),
			typeof(OutlinedText),
			new FrameworkPropertyMetadata(
				 false,
				 FrameworkPropertyMetadataOptions.AffectsRender,
				 new PropertyChangedCallback(OnOutlineTextInvalidated),
				 null
				 )
			);

		/// <summary>
		/// Specifies the brush to use for the stroke and optional hightlight of the formatted text.
		/// </summary>
		public Brush Stroke
		{
			get
			{
				return (Brush)GetValue(StrokeProperty);
			}

			set
			{
				SetValue(StrokeProperty, value);
			}
		}

		/// <summary>
		/// Identifies the Stroke dependency property.
		/// </summary>
		public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
			"Stroke",
			typeof(Brush),
			typeof(OutlinedText),
			new FrameworkPropertyMetadata(
				 new SolidColorBrush(Colors.Teal),
				 FrameworkPropertyMetadataOptions.AffectsRender,
				 new PropertyChangedCallback(OnOutlineTextInvalidated),
				 null
				 )
			);

		/// <summary>
		/// Specifies the brush to use for the stroke and optional hightlight of the formatted text.
		/// </summary>
		public Brush BackColor
		{
			get
			{
				return (Brush)GetValue(BackColorProperty);
			}

			set
			{
				SetValue(BackColorProperty, value);
			}
		}

		/// <summary>
		/// Identifies the Stroke dependency property.
		/// </summary>
		public static readonly DependencyProperty BackColorProperty = DependencyProperty.Register(
			"BackColor",
			typeof(Brush),
			typeof(OutlinedText),
			new FrameworkPropertyMetadata(
				 new SolidColorBrush(Color.FromArgb(120,0,0,0)),
				 FrameworkPropertyMetadataOptions.AffectsRender,
				 new PropertyChangedCallback(OnOutlineTextInvalidated),
				 null
				 )
			);

		/// <summary>
		///     The stroke thickness of the font.
		/// </summary>
		public double StrokeThickness
		{
			get
			{
				return (double)GetValue(StrokeThicknessProperty);
			}

			set
			{
				SetValue(StrokeThicknessProperty, value);
			}
		}

		/// <summary>
		/// Identifies the StrokeThickness dependency property.
		/// </summary>
		public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
			"StrokeThickness",
			typeof(double),
			typeof(OutlinedText),
			new FrameworkPropertyMetadata(
				 (double)0,
				 FrameworkPropertyMetadataOptions.AffectsRender,
				 new PropertyChangedCallback(OnOutlineTextInvalidated),
				 null
				 )
			);

		/// <summary>
		///     The stroke thickness of the font.
		/// </summary>
		public bool Underline
		{
			get
			{
				return (bool)GetValue(UnderlineProperty);
			}

			set
			{
				SetValue(UnderlineProperty, value);
			}
		}

		/// <summary>
		/// Identifies the StrokeThickness dependency property.
		/// </summary>
		public static readonly DependencyProperty UnderlineProperty = DependencyProperty.Register(
			"Underline",
			typeof(bool),
			typeof(OutlinedText),
			new FrameworkPropertyMetadata(
				 false,
				 FrameworkPropertyMetadataOptions.AffectsRender,
				 new PropertyChangedCallback(OnOutlineTextInvalidated),
				 null
				 )
			);

		/// <summary>
		/// Specifies the text string to display.
		/// </summary>
		public string Text
		{
			get
			{
				return (string)GetValue(TextProperty);
			}

			set
			{
				SetValue(TextProperty, value);
			}
		}

		/// <summary>
		/// Identifies the Text dependency property.
		/// </summary>
		public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
			"Text",
			typeof(string),
			typeof(OutlinedText),
			new FrameworkPropertyMetadata(
				 "",
				 FrameworkPropertyMetadataOptions.AffectsRender,
				 new PropertyChangedCallback(OnOutlineTextInvalidated),
				 null
				 )
			);

		public void AddChild(Object value)
		{

		}

		public void AddText(string value)
		{
			Text = value;
		}

		#endregion
	}
}