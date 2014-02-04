using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MediaPoint.Controls.Extensions
{
	public static class Bitmaps
	{
		/// <summary>
		/// FxCop requires all Marshalled functions to be in a class called NativeMethods.
		/// </summary>
		internal static class NativeMethods
		{
			[DllImport("gdi32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool DeleteObject(IntPtr hObject);
		}

		/// <summary>
		/// Converts a <see cref="System.Drawing.Image"/> into a WPF <see cref="BitmapSource"/>.
		/// </summary>
		/// <param name="source">The source image.</param>
		/// <returns>A BitmapSource</returns>
		public static BitmapSource ToBitmapSource(this System.Drawing.Image source)
		{
			System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(source);

			var bitSrc = bitmap.ToBitmapSource();

			bitmap.Dispose();
			bitmap = null;

			return bitSrc;
		}

		/// <summary>
		/// Converts a <see cref="System.Drawing.Bitmap"/> into a WPF <see cref="BitmapSource"/>.
		/// </summary>
		/// <remarks>Uses GDI to do the conversion. Hence the call to the marshalled DeleteObject.
		/// </remarks>
		/// <param name="source">The source bitmap.</param>
		/// <returns>A BitmapSource</returns>
		public static BitmapSource ToBitmapSource(this System.Drawing.Bitmap source)
		{
			BitmapSource bitSrc = null;

			var hBitmap = source.GetHbitmap();

			try
			{
				bitSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
					hBitmap,
					IntPtr.Zero,
					Int32Rect.Empty,
					BitmapSizeOptions.FromEmptyOptions());
			}
			catch (Win32Exception)
			{
				bitSrc = null;
			}
			finally
			{
				NativeMethods.DeleteObject(hBitmap);
			}

			return bitSrc;
		}

	}
}
