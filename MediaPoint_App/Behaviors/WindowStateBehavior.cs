using System;
using System.Configuration;
using System.Windows;
using System.Windows.Interactivity;

namespace MediaPoint.App.Behaviors
{
	public class WindowStateBehavior : Behavior<Window>
	{
		#region _____Fields_____________________

		public WindowStateSettings WindowStateSettings;

		#endregion

		#region _____Private Implementation_____

		void AssociatedObject_Loaded(object sender, EventArgs e)
		{
			ApplySettings();
		}

		void AssociatedObject_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			SaveSettings();
		}

		void SaveSettings()
		{
			if (this.AssociatedObject == null || this.WindowStateSettings == null) return;

			if (this.AssociatedObject.WindowState == WindowState.Normal)
			{
				this.WindowStateSettings.Width = this.AssociatedObject.Width;
				this.WindowStateSettings.Height = this.AssociatedObject.Height;
				this.WindowStateSettings.Top = this.AssociatedObject.Top;
				this.WindowStateSettings.Left = this.AssociatedObject.Left;
			}

			this.WindowStateSettings.WindowState = this.AssociatedObject.WindowState;
			this.WindowStateSettings.RestoreBounds = this.AssociatedObject.RestoreBounds;

			this.WindowStateSettings.Save();
		}

		void ApplySettings()
		{
			if (this.AssociatedObject == null) return;

			double left;
			double top;
			double width;
			double height;

			// If window was maximized, restore the size of the window in normal state
			if (this.WindowStateSettings.WindowState == WindowState.Maximized)
			{
				left = this.WindowStateSettings.RestoreBounds.Left;
				top = this.WindowStateSettings.RestoreBounds.Top;
				width = this.WindowStateSettings.RestoreBounds.Width;
				height = this.WindowStateSettings.RestoreBounds.Height;
			}
			else
			{
				left = this.WindowStateSettings.Left;
				top = this.WindowStateSettings.Top;
				width = this.WindowStateSettings.Width;
				height = this.WindowStateSettings.Height;
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
                try
                {
                    this.AssociatedObject.Width = width;
                    this.AssociatedObject.Height = height;
                }
                catch
                {
                }
			}

			// restore window state for the window
			if (this.WindowStateSettings.WindowState != WindowState.Minimized)
				this.AssociatedObject.WindowState = this.WindowStateSettings.WindowState;
		}

		#endregion

		#region _____Overrides__________________

		protected override void OnAttached()
		{
			this.AssociatedObject.Loaded += AssociatedObject_Loaded;

			this.AssociatedObject.Closing += AssociatedObject_Closing;
            this.WindowStateSettings = new WindowStateSettings(this.AssociatedObject.Name ?? this.AssociatedObject.GetType().FullName);


            if (this.AssociatedObject.IsLoaded)
            {
                AssociatedObject_Loaded(AssociatedObject, null);
            }
            
            base.OnAttached();
		}

		protected override void OnDetaching()
		{
			this.AssociatedObject.Initialized -= AssociatedObject_Loaded;
			this.AssociatedObject.Closing -= AssociatedObject_Closing;
			base.OnDetaching();
		}

		#endregion
	}

	public class WindowStateSettings : ApplicationSettingsBase
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
