using System.Configuration;
using System.Windows;
using System.Windows.Interactivity;

namespace MediaPoint.App.Behaviors
{
	public class WindowStateBehavior : Behavior<Window>
	{
		#region _____Fields_____________________

		private WindowStateSettings windowStateSettings;

		#endregion

		#region _____Private Implementation_____

		void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
		{
			ApplySettings();
		}

		void AssociatedObject_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			SaveSettings();
		}

		void SaveSettings()
		{
			if (this.AssociatedObject == null || this.windowStateSettings == null) return;

			if (this.AssociatedObject.WindowState == WindowState.Normal)
			{
				this.windowStateSettings.Width = this.AssociatedObject.Width;
				this.windowStateSettings.Height = this.AssociatedObject.Height;
				this.windowStateSettings.Top = this.AssociatedObject.Top;
				this.windowStateSettings.Left = this.AssociatedObject.Left;
			}

			this.windowStateSettings.WindowState = this.AssociatedObject.WindowState;
			this.windowStateSettings.RestoreBounds = this.AssociatedObject.RestoreBounds;

			this.windowStateSettings.Save();
		}

		void ApplySettings()
		{
			if (this.AssociatedObject == null) return;

			double left;
			double top;
			double width;
			double height;

			// If window was maximized, restore the size of the window in normal state
			if (this.windowStateSettings.WindowState == WindowState.Maximized)
			{
				left = this.windowStateSettings.RestoreBounds.Left;
				top = this.windowStateSettings.RestoreBounds.Top;
				width = this.windowStateSettings.RestoreBounds.Width;
				height = this.windowStateSettings.RestoreBounds.Height;
			}
			else
			{
				left = this.windowStateSettings.Left;
				top = this.windowStateSettings.Top;
				width = this.windowStateSettings.Width;
				height = this.windowStateSettings.Height;
			}

			var r = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
            if (r.Contains(left, top))
            {
                this.AssociatedObject.Left = left;
                this.AssociatedObject.Top = top;
            }
            else
            {
                return;
            }

			//restore size
			if (this.AssociatedObject.ResizeMode != ResizeMode.NoResize)
			{
				this.AssociatedObject.Width = width;
				this.AssociatedObject.Height = height;
			}

			// restore window state for the window
			if (this.windowStateSettings.WindowState != WindowState.Minimized)
				this.AssociatedObject.WindowState = this.windowStateSettings.WindowState;
		}

		#endregion

		#region _____Overrides__________________

		protected override void OnAttached()
		{
			this.AssociatedObject.Loaded += AssociatedObject_Loaded;
			this.AssociatedObject.Closing += AssociatedObject_Closing;
			this.windowStateSettings = new WindowStateSettings(this.AssociatedObject.GetType().FullName);
			base.OnAttached();
		}

		protected override void OnDetaching()
		{
			this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
			this.AssociatedObject.Closing -= AssociatedObject_Closing;
			base.OnDetaching();
		}

		#endregion
	}

	internal class WindowStateSettings : ApplicationSettingsBase
	{
		public WindowStateSettings(string settingsKey) : base(settingsKey) { }

		[UserScopedSetting]
		[DefaultSettingValue("820")]
		public double Width
		{
			get { return (double)this["Width"]; }
			set { this["Width"] = value; }
		}

		[UserScopedSetting]
		[DefaultSettingValue("500")]
		public double Height
		{
			get { return (double)this["Height"]; }
			set { this["Height"] = value; }
		}

		[UserScopedSetting]
		[DefaultSettingValue("Normal")]
		public WindowState WindowState
		{
			get { return (WindowState)this["WindowState"]; }
			set { this["WindowState"] = value; }
		}

		[UserScopedSetting]
		[DefaultSettingValue("0,0,0,0")]
		public Rect RestoreBounds
		{
			get { return (Rect)this["RestoreBounds"]; }
			set { this["RestoreBounds"] = value; }
		}

		[UserScopedSetting]
		[DefaultSettingValue("-1")]
		public double Left
		{
			get { return (double)this["Left"]; }
			set { this["Left"] = value; }
		}

		[UserScopedSetting]
		[DefaultSettingValue("-1")]
		public double Top
		{
			get { return (double)this["Top"]; }
			set { this["Top"] = value; }
		}
	}
}
