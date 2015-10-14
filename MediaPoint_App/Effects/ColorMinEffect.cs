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
	public class ColorMinEffect : ShaderEffect
	{		

		#region Dependency Properties

		/// <summary>
		/// Gets or sets the input brush used in the shader.
		/// </summary>
		public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(ColorMinEffect), 0);


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
		static ColorMinEffect()
		{
			pixelShader = new PixelShader();
			pixelShader.UriSource = EffectsGlobal.MakePackUri("Effects/FX/ColorMinEffect.ps");
		}

		/// <summary>
		/// Creates an instance and updates the shader's variables to the default values.
		/// </summary>
		public ColorMinEffect()
		{
			this.PixelShader = pixelShader;
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
