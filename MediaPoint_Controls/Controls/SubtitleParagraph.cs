using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MediaPoint.Subtitles.Logic;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using FontStyle = System.Windows.FontStyle;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using MediaPoint.Controls.Extensions;

namespace MediaPoint.Controls
{

	public class SubtitleParagraph : FrameworkElement
	{
		#region Callbacks

		private Paragraph[] _lines;
		private Typeface _typeface;
		private GlyphTypeface _glyphTypeface;
		private ImageSource _img;
		private Brush _back;
		private Brush _fill;
		private Brush _stroke;
		private double _fontSize;
		private double _strokeThickness;
		private bool _underline;
		private bool _strokeGlow;
		private bool _italic;
		private bool _bold; 
		private FontFamily _font;
		private Thread _rendering;
		private static Semaphore _newText; 
		private const int _marginLeft = 10;
		private const int _marginRight = 10;
		private const int _marginBottom = 10;

		public SubtitleParagraph()
		{
			_font = (GetValue(FontProperty) as FontFamily);
			_fontSize = (double)(GetValue(FontSizeProperty));
			FontWeight weight = Bold ? FontWeights.Bold : FontWeights.Normal;
			FontStyle style = Italic ? FontStyles.Italic : FontStyles.Normal;
			_typeface = new Typeface(Font,
								style,
								weight,
								FontStretches.Normal);

			_typeface.TryGetGlyphTypeface(out _glyphTypeface);

			_newText = new Semaphore(0, 1);
			_rendering = new Thread((ThreadStart)MakeBitmap);
			_rendering.IsBackground = true;
			_rendering.Priority = ThreadPriority.BelowNormal;
			_rendering.Start();
		}

		public static BitmapSource CreateBitmap(int width, int height, double dpi, Action<DrawingContext> render)
		{
			DrawingVisual drawingVisual = new DrawingVisual();
			try
			{
				using (DrawingContext drawingContext = drawingVisual.RenderOpen())
				{
					render(drawingContext);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Exception");
			}
			RenderTargetBitmap bitmap = new RenderTargetBitmap(
				width, height, dpi, dpi, PixelFormats.Default);
			bitmap.Render(drawingVisual);

			return bitmap;
		}


 		private static void OnInvalidated(DependencyObject d, DependencyPropertyChangedEventArgs e)
 		{
			Debug.WriteLine("OnInvalidated " + e.Property.Name);
 			SubtitleParagraph p = d as SubtitleParagraph;
			if (e.Property.Name == "Text")
			{
				if (e.NewValue == null)
				{
					p._lines = null;
				}
				else
				{
					p._lines = p.Text.ToArray();	
				}
				try
				{
					_newText.Release();
				}
				catch
				{
					// dont care since it can happen only from outside and we have only one , eg changing subtitle streams or properties
				}
			}
			if (e.Property.Name == "BackColor")
			{
				p._back = (p.GetValue(BackColorProperty) as Brush);
				p._back.Freeze();
			}
			if (e.Property.Name == "Stroke")
			{
				p._stroke = (p.GetValue(StrokeProperty) as Brush);
				p._stroke.Freeze();
			}
			if (e.Property.Name == "Fill")
			{
				p._fill = (p.GetValue(FillProperty) as Brush);
				p._fill.Freeze();
			}
			if (e.Property.Name == "Font" || e.Property.Name == "FontSize")
			{
				p._font = (p.GetValue(FontProperty) as FontFamily);
				p._fontSize = (double)(p.GetValue(FontSizeProperty));
				FontWeight weight = p.Bold ? FontWeights.Bold : FontWeights.Normal;
				FontStyle style = p.Italic ? FontStyles.Italic : FontStyles.Normal;
				p._typeface = new Typeface(p.Font,
									style,
									weight,
									FontStretches.Normal);
				
				if (!p._typeface.TryGetGlyphTypeface(out p._glyphTypeface))
				{
					var tt = new GlyphTypeface(new Uri("pack://application:,,,/MediaPoint;component/Resources/BuxtonSketch.ttf",
										  UriKind.RelativeOrAbsolute));
					p._glyphTypeface = tt;
				}
			}
			if (e.Property.Name == "Underline")
			{
				p._underline = (bool)(p.GetValue(UnderlineProperty));
			}
			if (e.Property.Name == "Bold")
			{
				p._bold = (bool)(p.GetValue(BoldProperty));
			}
			if (e.Property.Name == "Italic")
			{
				p._italic = (bool)(p.GetValue(ItalicProperty));
			}
			if (e.Property.Name == "StrokeGlow")
			{
				p._strokeGlow = (bool)(p.GetValue(StrokeGlowProperty));
			} 
			if (e.Property.Name == "StrokeThickness")
			{
				p._strokeThickness = (double)(p.GetValue(StrokeThicknessProperty));
			}
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (_lines == null || _lines.Length == 0 || _img == null) return new Size(0,0);
			return new Size(_img.Width, _img.Height);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (_lines == null || _lines.Length == 0 || _img == null) return new Size(0,0);
			return new Size(_img.Width, _img.Height);
		}

		private void MakeBitmap()
		{
			while (true)
			{
				_newText.WaitOne();

				GlyphTypeface glyphTypeface = null;
				if (_glyphTypeface != null) lock (_glyphTypeface)
				{
					glyphTypeface = _glyphTypeface;
				}

				if (glyphTypeface != null)
				{
					if (_lines == null || _lines.Length == 0)
					{
						_img = null;
					}
					else
					{
						if (_lines[0].VobSubMergedPack != null)
						{
							var bitmaps = _lines.Select(l => l.GetImage()).ToArray();
							int bw = bitmaps.Max(b => b.Width);
							int bh = bitmaps.Sum(b => b.Height);
							BitmapSource bimage = CreateBitmap((int) bw, (int) bh, 96,
							                                   dc =>
							                                   	{
							                                   		double ypos = 0;
							                                   		for (int lb = 0; lb < bitmaps.Count(); lb++)
							                                   		{
																		var bs = bitmaps[lb].ToBitmapSource();
							                                   			Point bLoc = new Point((bw - bs.Width)/2, ypos);
							                                   			Size bSize = new Size(bs.Width, bs.Height);
							                                   			ypos += bs.Height;
							                                   			dc.DrawImage(bs, new Rect(bLoc, bSize));
							                                   		}
							                                   	});
							bimage.Freeze();
							_img = bimage;
						}
						else
						{
							var lines = new List<string>(_lines.Select(l => l.Text).SelectMany(t => t.Split(new string[] { Environment.NewLine }, 10000, StringSplitOptions.None))).ToArray();
							{
								ushort[][] glyphIndexes = new ushort[lines.Length][];
								double[][] advanceWidths = new double[lines.Length][];

								double[] totalWidth = new double[lines.Length];

								int l = 0;
								foreach (var text in lines)
								{
									glyphIndexes[l] = new ushort[text.Length];
									advanceWidths[l] = new double[text.Length];
									for (int n = 0; n < text.Length; n++)
									{
										if (glyphTypeface.CharacterToGlyphMap.ContainsKey(text[n]))
										{
											ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];
											glyphIndexes[l][n] = glyphIndex;

											double width = glyphTypeface.AdvanceWidths[glyphIndex] * _fontSize;
											advanceWidths[l][n] = width;

											totalWidth[l] += width;
										}
									}
									l++;
								}

								double w = totalWidth.Max(lw => lw) + _marginLeft + _marginRight;
								double h = (lines.Length * _fontSize) + _fontSize / 4 + _marginBottom;

								BitmapSource image = CreateBitmap((int)w, (int)h, 96,
																  dc =>
																  {
																	  int line = 1;
																	  if (_back != Brushes.Transparent)
																		  dc.DrawRectangle(_back, null, new Rect(0d, 0d, w, h));
																	  foreach (var text in lines)
																	  {
																		  if (string.IsNullOrEmpty(text))
																		  {
																			  line++;
																			  continue;
																		  }
																		  var origin = new Point((w - totalWidth[line - 1]) / 2, line * _fontSize);
																		  var orig = ((SolidColorBrush)_stroke.Clone()).Color;
																		  var strokeBrush = new SolidColorBrush
																							  {
																								  Color =
																									  new Color()
																										  {
																											  B = orig.B,
																											  R = orig.R,
																											  G = orig.G,
																											  A = _strokeGlow ? (byte)35 : orig.A
																										  }
																							  };

																		  var glyphRun = new GlyphRun(glyphTypeface, 0, false, _fontSize,
																									  glyphIndexes[line - 1], origin,
																									  advanceWidths[line - 1], null, null,
																									  null, null,
																									  null, null);

																		  if (_underline)
																		  {
																			  double y = origin.Y;
																			  y -= (glyphTypeface.Baseline + _typeface.UnderlinePosition * _fontSize);
																			  for (double i = _strokeThickness; i > 0; i--)
																			  {
																				  dc.DrawLine(
																					  new Pen(strokeBrush, (_typeface.UnderlineThickness * _fontSize) + i),
																					  new Point(origin.X - _strokeThickness / 2, y),
																					  new Point(origin.X + totalWidth[line - 1] + _strokeThickness / 2, y));
																				  if (!_strokeGlow) break;
																			  }
																		  }

																		  var geo = glyphRun.BuildGeometry();
																		  for (double i = _strokeThickness; i > 0; i--)
																		  {
																			  dc.DrawGeometry(null, new Pen(strokeBrush, i), geo);
																			  if (!_strokeGlow) break;
																		  }

																		  dc.DrawGlyphRun(_fill, glyphRun);

																		  if (_underline)
																		  {
																			  double y = origin.Y;
																			  y -= (glyphTypeface.Baseline + _typeface.UnderlinePosition * _fontSize);
																			  dc.DrawLine(new Pen(_fill, _typeface.UnderlineThickness * _fontSize),
																						  new Point(origin.X, y),
																						  new Point(origin.X + totalWidth[line - 1], y));
																		  }

																		  line++;
																	  }
																  });

								image.Freeze();
								_img = image;
							}
						}
					}
				}
				try
				{
					_newText.Release();
				}
				catch
				{
				}
				Dispatcher.Invoke(((Action)(() =>
					                            	{
					                            		_newText.WaitOne(); InvalidateVisual(); })), DispatcherPriority.Render, new object[] { });
			}
		}
		#endregion

		#region FrameworkElement Overrides

		/// <summary>
		/// OnRender override draws the geometry of the text and optional highlight.
		/// </summary>
		/// <param name="dc">Drawing context of the OutlineText control.</param>
		protected override void OnRender(DrawingContext dc)
		{
			Debug.WriteLine("Render imgnull: " + (_img == null).ToString());
			if (_img != null)
			{
				lock(_img)
					dc.DrawImage(_img, new Rect(0,0,_img.Width,_img.Height));
			}
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
			typeof(SubtitleParagraph),
			new FrameworkPropertyMetadata(
				false,
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
				new PropertyChangedCallback(OnInvalidated),
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
			typeof(SubtitleParagraph),
			new FrameworkPropertyMetadata(
				new SolidColorBrush(Colors.LightSteelBlue),
				FrameworkPropertyMetadataOptions.AffectsRender,
				new PropertyChangedCallback(OnInvalidated),
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
			typeof(SubtitleParagraph),
			new FrameworkPropertyMetadata(
				new FontFamily("Arial"),
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
				new PropertyChangedCallback(OnInvalidated),
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
			typeof(SubtitleParagraph),
			new FrameworkPropertyMetadata(
				 (double)48.0,
				 FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
				 new PropertyChangedCallback(OnInvalidated),
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
			typeof(SubtitleParagraph),
			new FrameworkPropertyMetadata(
				 false,
				 FrameworkPropertyMetadataOptions.AffectsRender,
				 new PropertyChangedCallback(OnInvalidated),
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
			typeof(SubtitleParagraph),
			new FrameworkPropertyMetadata(
				 new SolidColorBrush(Colors.Teal),
				 FrameworkPropertyMetadataOptions.AffectsRender,
				 new PropertyChangedCallback(OnInvalidated),
				 null
				 )
			);

		/// <summary>
		/// If the hightlight is glowing
		/// </summary>
		public bool StrokeGlow
		{
			get
			{
				return (bool)GetValue(StrokeGlowProperty);
			}

			set
			{
				SetValue(StrokeGlowProperty, value);
			}
		}

		/// <summary>
		/// Identifies the Stroke dependency property.
		/// </summary>
		public static readonly DependencyProperty StrokeGlowProperty = DependencyProperty.Register(
			"StrokeGlow",
			typeof(bool),
			typeof(SubtitleParagraph),
			new FrameworkPropertyMetadata(
				 false,
				 FrameworkPropertyMetadataOptions.AffectsRender,
				 new PropertyChangedCallback(OnInvalidated),
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
			typeof(SubtitleParagraph),
			new FrameworkPropertyMetadata(
				 Brushes.Transparent,
				 FrameworkPropertyMetadataOptions.AffectsRender,
				 new PropertyChangedCallback(OnInvalidated),
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
			typeof(SubtitleParagraph),
			new FrameworkPropertyMetadata(
				 (double)0,
				 FrameworkPropertyMetadataOptions.AffectsRender,
				 new PropertyChangedCallback(OnInvalidated),
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
			typeof(SubtitleParagraph),
			new FrameworkPropertyMetadata(
				 false,
				 FrameworkPropertyMetadataOptions.AffectsRender,
				 new PropertyChangedCallback(OnInvalidated),
				 null
				 )
			);

		/// <summary>
		/// Specifies the text string to display.
		/// </summary>
		public IEnumerable<Paragraph> Text
		{
			get
			{
				return (IEnumerable<Paragraph>)GetValue(TextProperty);
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
			typeof(IEnumerable<Paragraph>),
			typeof(SubtitleParagraph),
			new FrameworkPropertyMetadata(
				 null,
				 FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
				 new PropertyChangedCallback(OnInvalidated),
				 null
				 )
			);

		#endregion
	}
}
