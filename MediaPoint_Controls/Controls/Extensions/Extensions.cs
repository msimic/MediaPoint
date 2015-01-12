using System.Windows.Interop;
using System.Windows.Media;

namespace MediaPoint.Controls.Extensions
{
	using System;
	using System.Runtime.InteropServices;
	using System.Windows;

	public static class WindowExtensions
	{
		#region Public Methods

		public static Size ComputeNewVideoSize(this Window window, Visual ctr, Size videoSize, bool full = false)
		{
			var source = PresentationSource.FromVisual(window);
			Matrix transformFromDevice = source.CompositionTarget.TransformFromDevice;
			var wpfSize = (Size)transformFromDevice.Transform((Vector)videoSize);
			var ms = MonitorSize(ref window, transformFromDevice, full);

			Size ret;

			if (ms.Width < wpfSize.Width) ret = ms;
			else if (ms.Height < wpfSize.Height) ret = ms;
			else ret = wpfSize;

			return ret;
		}

		public static bool ActivateCenteredToMouse(this Window window)
		{
			ComputeTopLeft(ref window);
			return window.Activate();
		}

		public static void ShowCenteredToMouse(this Window window)
		{
			// in case the default start-up location isn't set to Manual
			WindowStartupLocation oldLocation = window.WindowStartupLocation;
			// set location to manual -> window will be placed by Top and Left property
			window.WindowStartupLocation = WindowStartupLocation.Manual;
			ComputeTopLeft(ref window);
			window.Show();
			window.WindowStartupLocation = oldLocation;
		}

		#endregion

		#region Methods

        public static Size Difference(this Size size, Size other)
        {
            return new Size(size.Width - other.Width, size.Height - other.Height);
        }

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        public static Size MonitorSize(ref Window window, Matrix dpimatrix, out Vector position)
        {
            W32Point pt = new W32Point();
            pt.X = (int)(window.Left + (window.Width) / 2);
            pt.Y = (int)(window.Height + (window.Height) / 2);

            IntPtr monHandle = MonitorFromWindow(new WindowInteropHelper(window).Handle, 0x00000002);
            W32MonitorInfo monInfo = new W32MonitorInfo();
            monInfo.Size = Marshal.SizeOf(typeof(W32MonitorInfo));

            if (!GetMonitorInfo(monHandle, ref monInfo))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            // use WorkArea struct to include the taskbar position.
            W32Rect monitor = monInfo.WorkArea;

            position = dpimatrix.Transform(new Vector((monitor.Left), (monitor.Top)));

            return (Size)dpimatrix.Transform(new Vector((monitor.Right - monitor.Left), (monitor.Bottom - monitor.Top)));
        }

		private static Size MonitorSize(ref Window window, Matrix dpimatrix, bool full = false)
		{
			W32Point pt = new W32Point();
			pt.X = (int)(window.Left + (window.Width) / 2);
			pt.Y = (int)(window.Height + (window.Height) / 2);

            IntPtr monHandle = MonitorFromWindow(new WindowInteropHelper(window).Handle, 0x00000002);
			W32MonitorInfo monInfo = new W32MonitorInfo();
			monInfo.Size = Marshal.SizeOf(typeof (W32MonitorInfo));

			if (!GetMonitorInfo(monHandle, ref monInfo))
			{
				Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
			}

			// use WorkArea struct to include the taskbar position.
			W32Rect monitor = monInfo.WorkArea;
			if (full) monitor = monInfo.Monitor;

			return (Size)dpimatrix.Transform(new Vector(Math.Abs(monitor.Right - monitor.Left), Math.Abs(monitor.Bottom - monitor.Top)));
		}

		private static void ComputeTopLeft(ref Window window)
		{
			W32Point pt = new W32Point();
			if (!GetCursorPos(ref pt))
			{
				Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
			}

			// 0x00000002: return nearest monitor if pt is not contained in any monitor.
            IntPtr monHandle = MonitorFromWindow(new WindowInteropHelper(window).Handle, 0x00000002);
			W32MonitorInfo monInfo = new W32MonitorInfo();
			monInfo.Size = Marshal.SizeOf(typeof(W32MonitorInfo));

			if (!GetMonitorInfo(monHandle, ref monInfo))
			{
				Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
			}

			// use WorkArea struct to include the taskbar position.
			W32Rect monitor = monInfo.WorkArea;
			double offsetX = Math.Round(window.Width / 2);
			double offsetY = Math.Round(window.Height / 2);

			double top = pt.Y - offsetY;
			double left = pt.X - offsetX;

			Rect screen = new Rect(
				new Point(monitor.Left, monitor.Top),
				new Point(monitor.Right, monitor.Bottom));
			Rect wnd = new Rect(
				new Point(left, top),
				new Point(left + window.Width, top + window.Height));

			window.Top = wnd.Top;
			window.Left = wnd.Left;

			if (!screen.Contains(wnd))
			{
				if (wnd.Top < screen.Top)
				{
					double diff = Math.Abs(screen.Top - wnd.Top);
					window.Top = wnd.Top + diff;
				}

				if (wnd.Bottom > screen.Bottom)
				{
					double diff = wnd.Bottom - screen.Bottom;
					window.Top = wnd.Top - diff;
				}

				if (wnd.Left < screen.Left)
				{
					double diff = Math.Abs(screen.Left - wnd.Left);
					window.Left = wnd.Left + diff;
				}

				if (wnd.Right > screen.Right)
				{
					double diff = wnd.Right - screen.Right;
					window.Left = wnd.Left - diff;
				}
			}
		}

		#endregion

		#region W32 API

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetCursorPos(ref W32Point pt);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetMonitorInfo(IntPtr hMonitor, ref W32MonitorInfo lpmi);

		[DllImport("user32.dll")]
		private static extern IntPtr MonitorFromPoint(W32Point pt, uint dwFlags);

		[StructLayout(LayoutKind.Sequential)]
		public struct W32Point
		{
			public int X;
			public int Y;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct W32MonitorInfo
		{
			public int Size;
			public W32Rect Monitor;
			public W32Rect WorkArea;
			public uint Flags;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct W32Rect
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		#endregion
	}
}