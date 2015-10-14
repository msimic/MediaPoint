using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Interactivity;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MediaPoint.Controls.Extensions;
using System.Windows.Controls;
using MediaPoint.App.Extensions;
using System.Windows.Media;
using MediaPoint.Common.Helpers;
using System.Globalization;
using System.ComponentModel;
using MediaPoint.MVVM.Services;
using MediaPoint.VM.ViewInterfaces;
using System.Diagnostics;

namespace MediaPoint.App.Behaviors
{

    /// <summary>
    /// A behavior that adds opening in a new window
    /// </summary>
    public sealed class PopupInWindow : Behavior<FrameworkElement>
    {

        private DependencyObject _parent;
        private int _insertIndex = -1;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:PopupInWindow"/> class.
        /// </summary>
        public PopupInWindow()
        {
        }

        #endregion

        #region Properties

        #region IsAutomatic

        /// <summary>
        /// returns or sets if the window is automatic
        /// </summary>
        public bool IsAutomatic
        {
            get
            {
                return (bool)GetValue(IsAutomaticProperty);
            }
            set
            {
                SetValue(IsAutomaticProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the <see cref="P:IsAutomaticProperty"/> property.
        /// </summary>
        private static readonly DependencyProperty IsAutomaticProperty = DependencyProperty.RegisterAttached("IsAutomatic", typeof(bool), typeof(PopupInWindow), new PropertyMetadata(default(bool), AutomaticChanged));

        private static void AutomaticChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PopupInWindow me = null;
            if (d is FrameworkElement)
            {
                me = Interaction.GetBehaviors(d).FirstOrDefault(b => b is PopupInWindow) as PopupInWindow;
            }
            else
            {
                me = d as PopupInWindow;
            }
            if (me == null || me._isInitialized == false) return;

            lock (me)
            {
                if ((bool)e.NewValue == false)
                {
                    me.Hide();
                    me.AssociatedObject.SetValue(IsAutomaticProperty, false);
                }
                else
                {
                    me.Show();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the to open a popup window.
        /// </summary>
        public static bool GetIsAutomatic(Window window)
        {
            return (bool)window.GetValue(IsAutomaticProperty);
        }

        /// <summary>
        /// Sets a value indicating whether or not to open in window.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <param name="value">The value.</param>
        public static void SetIsAutomatic(Window window, bool value)
        {
            window.SetValue(IsAutomaticProperty, value);
        }



        public Window PopupWindow
        {
            get { return (Window)GetValue(PopupWindowProperty); }
            set { SetValue(PopupWindowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PopupWindow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PopupWindowProperty =
            DependencyProperty.Register("PopupWindow", typeof(Window), typeof(PopupInWindow), new PropertyMetadata(null));

        
        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>Override this to hook up functionality to the AssociatedObject.</remarks>
        protected override void OnAttached()
        {
            if (AssociatedObject == null) return;
            AssociatedObject.SetValue(IsAutomaticProperty, IsAutomatic);
            AssociatedObject.IsVisibleChanged += AssociatedObject_IsVisibleChanged;
            base.OnAttached();
        }

        bool _skipHandler = false;
        bool _isInitialized;


        void AssociatedObject_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (AssociatedObject == null || _skipHandler || ((bool)e.NewValue && PopupWindow != null && PopupWindow.IsVisible)) return;

            var app = Application.Current as App;

            Action execute = () => {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    lock (this)
                    {
                        if (PopupWindow == null)
                        {
                            TextInfo ti = CultureInfo.CurrentUICulture.TextInfo;
                            var owenr = AssociatedObject.TryFindParent<Window>();

                            PopupWindow = new Window();
                            PopupWindow.SizeToContent = SizeToContent.WidthAndHeight;
                            PopupWindow.AllowsTransparency = true;
                            PopupWindow.ShowInTaskbar = false;
                            PopupWindow.Title = ti.ToTitleCase(AssociatedObject.Name);
                            PopupWindow.Name = PopupWindow.Title;
                            PopupWindow.Background = Brushes.Transparent;
                            PopupWindow.WindowStyle = WindowStyle.None;
                            PopupWindow.DataContext = AssociatedObject.DataContext;
                            PopupWindow.Owner = owenr;
                            PopupWindow.Activated += (s,args) =>
                            {
                                if (Mouse.Captured != null)
                                {
                                    Mouse.Captured.ReleaseMouseCapture();
                                }
                            };
                            PopupWindow.SizeChanged += (s, args) =>
                            {
                                if (Mouse.Captured != null)
                                {
                                    Mouse.Captured.ReleaseMouseCapture();
                                }
                            };
                            PopupWindow.IsVisibleChanged += (s, args) =>
                            {
                                if (Mouse.Captured != null)
                                {
                                    Mouse.Captured.ReleaseMouseCapture();
                                }
                            };
                            //PopupWindow.Topmost = true;
                            PopupWindow.MinHeight = 200;
                            if (double.IsNaN(AssociatedObject.MinHeight) != true)
                            {
                                PopupWindow.MinHeight = AssociatedObject.MinHeight;
                            }
                            PopupWindow.MinWidth = 200;
                            if (double.IsNaN(AssociatedObject.MinWidth) != true)
                            {
                                PopupWindow.MinWidth = AssociatedObject.MinWidth;
                            }
                            PopupWindow.Icon = owenr.Icon;
                            PopupWindow.ResizeMode = ResizeMode.CanResizeWithGrip;

                            if (double.IsNaN(AssociatedObject.Height) != true)
                            {
                                PopupWindow.Height = AssociatedObject.Height;
                                PopupWindow.ResizeMode = ResizeMode.NoResize;
                            }
                            if (double.IsNaN(AssociatedObject.Width) != true)
                            {
                                PopupWindow.Width = AssociatedObject.Width;
                                PopupWindow.ResizeMode = ResizeMode.NoResize;
                            }

                            BehaviorCollection itemBehaviors = Interaction.GetBehaviors(PopupWindow);
                            var bh = new WindowStateBehavior();
                            itemBehaviors.Add(bh);

                            if (bh.WindowStateSettings.Left != -1)
                            {
                                PopupWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                            }
                            else
                            {
                                PopupWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            }

                            _isInitialized = true;
                        }

                        if ((bool)(sender as FrameworkElement).GetValue(IsAutomaticProperty) == false)
                        {
                            return;
                        }

                        if ((bool)e.NewValue)
                        {
                            Debug.WriteLine("ShowingInPopup");
                            Show();
                        }
                        else
                        {
                            Debug.WriteLine("Closing Popup");
                            Hide();
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);

            };

            if (app.Initialized)
            {
                execute();
            }
            else
            {
                app.InitializationSequence[this] = execute;
            }
        }

        private void Hide()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("PopupInWindow.Hide(" + AssociatedObject.Name ?? "?" + ")");

                if (PopupWindow == null || PopupWindow.Visibility == Visibility.Hidden)
                {
                    return;
                }
                _skipHandler = true;
                var obj = AssociatedObject;
                var parent = obj.Parent;
                obj.MaxWidth = PopupWindow.ActualWidth;
                obj.MaxHeight = PopupWindow.ActualHeight;
                _insertIndex = parent.RemoveChild(obj);
                _parent.AddChild(obj, _insertIndex);
            }
            finally
            {
                PopupWindow.Hide();
                _skipHandler = false;
            }
        }

        private void Show()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("PopupInWindow.Show(" + AssociatedObject.Name ?? "?" + ")");
                _skipHandler = true;
                var parent = AssociatedObject.Parent;
                _parent = parent;
                _insertIndex = parent.RemoveChild(AssociatedObject);
                AssociatedObject.MaxWidth = double.MaxValue;
                AssociatedObject.MaxHeight = double.MaxValue;
                ClearTranslateTransform(AssociatedObject);
                PopupWindow.Content = AssociatedObject;
                PopupWindow.Show();
                ServiceLocator.GetService<IMainView>().GetWindow().Focus();
                PopupWindow.Focus();
                if (PopupWindow.Tag is IWindowShadow == false)
                {
                    var ih = new WindowInteropHelper(PopupWindow);
                    ih.EnsureHandle();
                    var shadower = WindowShadow.CreateNew().Shadower;
                    IntPtr hinstance = Marshal.GetHINSTANCE(this.GetType().Module);
                    int hr = shadower.Init(hinstance);
                    hr = shadower.CreateForWindow(ih.Handle);
                    shadower.SetShadowSize(0);
                    PopupWindow.Tag = shadower;
                    TypeDescriptor.GetProperties(AssociatedObject)["Opacity"].AddValueChanged(AssociatedObject, OpacityValueChanged);
                }
                else
                {
                    var shadower = PopupWindow.Tag as IWindowShadow;
                    var ih = new WindowInteropHelper(PopupWindow);
                    shadower.Show(ih.Handle);
                }
            }
            finally
            {

                if (Mouse.Captured != null)
                {
                    Mouse.Captured.ReleaseMouseCapture();
                }

                _skipHandler = false;
            }
        }

        private void ClearTranslateTransform(FrameworkElement AssociatedObject)
        {
            TranslateTransform t = null;
            if (AssociatedObject.RenderTransform is TranslateTransform)
            {
                t = AssociatedObject.RenderTransform as TranslateTransform;
            }
            else if (AssociatedObject.RenderTransform is TransformGroup)
            {
                var tg = AssociatedObject.RenderTransform as TransformGroup;
                t = tg.Children.FirstOrDefault(v => v is TranslateTransform) as TranslateTransform;
            }
            else if (AssociatedObject.RenderTransform is MatrixTransform)
            {
                var matrixTransform = AssociatedObject.RenderTransform as MatrixTransform;
                Matrix matrix = matrixTransform.Matrix;
                matrix.OffsetX = 0;
                matrix.OffsetY = 0;
                MatrixTransform matrixTransform2 = new MatrixTransform();
                matrixTransform2.Matrix = matrix;
                base.AssociatedObject.RenderTransform = matrixTransform2;
                return;
            }

            if (t != null)
            {
                t.X = 0;
                t.Y = 0;
            }
        }

        int _lastOpacity = -1;
        void OpacityValueChanged(object sender, EventArgs e)
        {
            if (PopupWindow != null && PopupWindow.Tag is IWindowShadow)
            {
                int val = (int)((sender as FrameworkElement).Opacity * 6);
                if (val != _lastOpacity)
                {
                    _lastOpacity = val;
                    (PopupWindow.Tag as IWindowShadow).SetShadowSize(val);
                }
            }
        }

        /// <summary>
        /// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        /// <remarks>Override this to unhook functionality from the AssociatedObject.</remarks>
        protected override void OnDetaching()
        {
            if (AssociatedObject == null) return;
            AssociatedObject.IsVisibleChanged -= AssociatedObject_IsVisibleChanged;
            TypeDescriptor.GetProperties(AssociatedObject)["Opacity"].RemoveValueChanged(AssociatedObject, OpacityValueChanged);
            base.OnDetaching();

            var app = Application.Current as App;
            app.InitializationSequence.Remove(this);

        }

        #endregion


    }   // class

}   // namespace
