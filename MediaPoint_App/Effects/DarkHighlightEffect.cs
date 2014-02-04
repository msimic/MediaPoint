namespace MediaPoint.App.Effects
{
	using System;
	using System.Windows;
	using System.Windows.Media;
	using System.Windows.Media.Effects;
	using System.Diagnostics;
	using System.Text;
	using System.Reflection;

	/// <summary>
	/// This is the implementation of an extensible framework ShaderEffect which loads
	/// a shader model 2 pixel shader. Dependecy properties declared in this class are mapped
	/// to registers as defined in the *.ps file being loaded below.
	/// </summary>
	public class DarkHighlightEffect : ShaderEffect
	{		

		#region Dependency Properties

		/// <summary>
		/// Gets or sets the input brush used in the shader.
		/// </summary>
		public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(DarkHighlightEffect), 0);

		public static readonly DependencyProperty StrengthProperty = DependencyProperty.Register("Strength", typeof(double), typeof(DarkHighlightEffect), new UIPropertyMetadata(2.0d, PixelShaderConstantCallback(0)));
		public static readonly DependencyProperty BlurAmountProperty = DependencyProperty.Register("BlurAmount", typeof(double), typeof(DarkHighlightEffect), new UIPropertyMetadata(0.5d, PixelShaderConstantCallback(1)));

		/// <summary>
		/// Gets or sets the Strenght variable within the shader.
		/// </summary>
		public double Strength
		{
			get { return (double)GetValue(StrengthProperty); }
			set { SetValue(StrengthProperty, value); }
		}

		/// <summary>
		/// Gets or sets the BlurAmount variable within the shader.
		/// </summary>
		public double BlurAmount
		{
			get { return (double)GetValue(BlurAmountProperty); }
			set { SetValue(BlurAmountProperty, value); }
		}

		#endregion

		#region Member Data

		/// <summary>
		/// The pixel shader instance.
		/// </summary>
		private static PixelShader pixelShader;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates an instance of the shader from the included pixel shader.
		/// </summary>
		static DarkHighlightEffect()
		{
			pixelShader = new PixelShader();
			pixelShader.UriSource = EffectsGlobal.MakePackUri("Effects/FX/DarkHighlightEffect.ps");
		}

		/// <summary>
		/// Creates an instance and updates the shader's variables to the default values.
		/// </summary>
		public DarkHighlightEffect()
		{
			this.PixelShader = pixelShader;
			UpdateShaderValue(StrengthProperty);
			UpdateShaderValue(BlurAmountProperty);			
			UpdateShaderValue(InputProperty);
		}

		#endregion

		/// <summary>
		/// Gets or sets the input used within the shader.
		/// </summary>
		[System.ComponentModel.BrowsableAttribute(false)]
		public Brush Input
		{
			get { return (Brush)GetValue(InputProperty); }
			set { SetValue(InputProperty, value); }
		}
	}
}
