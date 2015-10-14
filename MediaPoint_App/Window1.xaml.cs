using System;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MediaPoint.App.AttachedProperties;
using MediaPoint.Common.DirectShow.MediaPlayers;
using MediaPoint.Common.TaskbarNotification;
using MediaPoint.VM.ViewInterfaces;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MediaPoint.App.Extensions;
using MediaPoint.VM;
using MediaPoint.Controls;
using MediaPoint.MVVM.Services;
using Microsoft.WindowsAPICodePack.Taskbar;
using NAudio.CoreAudioApi;
using WPFSoundVisualizationLib;
using MediaPoint.App.Audio;
using MediaPoint.Common.Helpers;
using System.Windows.Controls.Primitives;
using System.Collections.Generic;
using MediaPoint.VM.Config;
using System.IO;
using System.Windows.Interactivity;
using MediaPoint.App.Behaviors;
using MediaPoint.Common.Services;
using MediaPoint.VM.Services.Model;

namespace MediaPoint.App
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : IMainView, ISpectrumPlayer, ISpectrumVisualizer, IInputTeller, IFramePictureProvider, IMainWindow
    {
        private double _skewX;
        private double _skewY;
        private double _scaleX = 1;
        private double _scaleY = 1;
        private double _rotation;
        private readonly List<FrameworkElement> _elementsToRefresh = new List<FrameworkElement>();
        private string _startFile;
        private DateTime _timeToDelayReShowing = DateTime.Now;
        private Point _lastpoint;
        private IntPtr _hwnd;
        private bool _childWindowsFollow = true;

        public string StartupFile
        {
            get { return _startFile; }
            set
            {
                _startFile = value;

                Action<string> load = (s =>
                {
                    if (s != null)
                    {
                        var main = DataContext as Main;
                        if (main != null && main.Player != null)
                        {
                            main.Player.OpenCommand.Execute(s);
                        }
                    }
                });

                var b = new BackgroundWorker();
                b.DoWork += (sender, args) => { while (!_onceDone) Thread.Sleep(200); args.Result = args.Argument; };
                b.RunWorkerCompleted += (sender, args) => load((string)args.Result);
                b.RunWorkerAsync(_startFile);
            }
        }

        public enum HookType : int
        {
            WH_JOURNALRECORD = 0,
            WH_JOURNALPLAYBACK = 1,
            WH_KEYBOARD = 2,
            WH_GETMESSAGE = 3,
            WH_CALLWNDPROC = 4,
            WH_CBT = 5,
            WH_SYSMSGFILTER = 6,
            WH_MOUSE = 7,
            WH_HARDWARE = 8,
            WH_DEBUG = 9,
            WH_SHELL = 10,
            WH_FOREGROUNDIDLE = 11,
            WH_CALLWNDPROCRET = 12,
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14
        }

        delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
        private HookProc myCallbackDelegate = null;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(HookType code, HookProc func, IntPtr hInstance, int threadID);

        [DllImport("user32.dll")]
        static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr MyCallbackFunction(int code, IntPtr wParam, IntPtr lParam)
        {
            Debug.WriteLine("Hook " + code + " wP: " + (int)wParam + " lP: " + (int)lParam);
            switch ((int)code)
            {
                case HSHELL_APPCOMMAND:
                    if ((int)lParam == APPCOMMAND_LAUNCH_MEDIA_SELECT || (int)lParam == APPCOMMAND_LAUNCH_MEDIA_SELECT2)
                    {
                        return IntPtr.Zero;
                    }
                    break;
            }
            return new IntPtr(CallNextHookEx(IntPtr.Zero, code, wParam, lParam));
        }

        public Window1()
        {
            InitializeComponent();

            AddHandler(FrameworkElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(On_MouseLeftButtonDown), true);
            
            this.myCallbackDelegate = new HookProc(this.MyCallbackFunction);

            // setup a keyboard hook
            //SetWindowsHookEx(HookType.WH_SHELL, this.myCallbackDelegate, IntPtr.Zero, AppDomain.GetCurrentThreadId());

            LocationChanged += Window1_LocationChanged;
            Visibility = Visibility.Collapsed;

            var ih = new WindowInteropHelper(this);
            ih.EnsureHandle();
            _hwnd = ih.Handle;
            _shadower = WindowShadow.CreateNew().Shadower;
            IntPtr hinstance = Marshal.GetHINSTANCE(this.GetType().Module);
            int hr = _shadower.Init(hinstance);
            hr = _shadower.CreateForWindow(_hwnd);
            _shadower.SetShadowSize(6);
                    
            var timer = new DispatcherTimer(DispatcherPriority.Background);
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 3);
            timer.Start();

            PreviewGotKeyboardFocus += Window1_PreviewGotKeyboardFocus;

            Layout.PreviewDragEnter += Window1_PreviewDragEnter;
            Layout.PreviewDragLeave += Window1_PreviewDragLeave;
            Layout.PreviewDragOver += Layout_PreviewDragOver;
            Layout.PreviewDrop += Window1_PreviewDrop;

            //MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            
            //foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
            //{
            //    Console.WriteLine("*** {0}, {1}, {2}", device.FriendlyName, device.DeviceFriendlyName, device.State);
            //    if (device.State == DeviceState.Active) Console.WriteLine("   {0}", device.AudioEndpointVolume.Channels.Count);
            //}
        }

        private void On_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.Captured != null)
            {
                Debug.WriteLine("Captured: " + Mouse.Captured.GetType().Name);
            }
        }

        void Window1_LocationChanged(object sender, EventArgs e)
        {
            ParentWndMove(this, this.OwnedWindows.OfType<Window>().ToArray());
        }

        Point parentWindowPosition;
        public void ParentWndMove(Window parentWindow, Window[] windowsToMove)
        {
            if (_childWindowsFollow) for (int i = 0; i < windowsToMove.Length; i++)
                {
                    if (windowsToMove[i] != null)
                    {
                        windowsToMove[i].Top += -(parentWindowPosition.Y - (parentWindow.ActualHeight / 2) - parentWindow.Top);
                        windowsToMove[i].Left += -(parentWindowPosition.X - (parentWindow.ActualWidth / 2) - parentWindow.Left);
                    }
                }

            parentWindowPosition.X = parentWindow.Left + (parentWindow.ActualWidth / 2);
            parentWindowPosition.Y = parentWindow.Top + (parentWindow.ActualHeight / 2);
        }

        void Layout_PreviewDragOver(object sender, DragEventArgs e)
        {
            CheckDragDrop(e);
        }

        void Window1_PreviewDrop(object sender, DragEventArgs e)
        {
            var dc = DataContext as Main;
            if (dc == null) return;

            e.Handled = true;
            var data = e.Data.GetData("FileDrop") as string[];
            if (data != null)
            {
                foreach (var file in data)
                {
                    string extension = Path.GetExtension(file);
                    if (extension != null)
                    {
                        var ext = extension.Substring(1).ToLowerInvariant();
                        if (!SupportedFiles.All.ContainsKey(ext))
                        {
                            return;
                        }
                    }
                }

                foreach (var file in data)
                {
                    dc.Playlist.AddTrackIfNotExisting(new Uri(file, UriKind.Absolute));
                }

                if (dc.Playlist.Tracks.Count > 1)
                {
                    dc.ShowPlaylist = true;
                }

                if (data.Length > 0 && !dc.Player.IsPlaying && !dc.Player.IsPaused)
                {
                    StartupFile = data[0];
                }
            }
            else
            {
                bool isText = e.Data.GetDataPresent(DataFormats.Text);

                if (isText)
                {
                    string text = (string)e.Data.GetData(DataFormats.Text);
                    if (Uri.IsWellFormedUriString(text, UriKind.Absolute))
                    {
                        var uri = new Uri(text, UriKind.Absolute);

                        dc.Playlist.AddTrackIfNotExisting(uri);

                        if (dc.Playlist.Tracks.Count > 1)
                        {
                            dc.ShowPlaylist = true;
                        }
                        else if (dc.Playlist.Tracks.Count == 1)
                        {
                            dc.Player.Open(uri);
                        }
                    }
                }
            }
        }

        void Window1_PreviewDragLeave(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        void Window1_PreviewDragEnter(object sender, DragEventArgs e)
        {
            CheckDragDrop(e);
        }

        private static void CheckDragDrop(DragEventArgs e)
        {
            bool isText = e.Data.GetDataPresent(DataFormats.Text);

            if (isText)
            {
                string text = (string)e.Data.GetData(DataFormats.Text);
                if (Uri.IsWellFormedUriString(text, UriKind.Absolute))
                {
                    e.Effects = DragDropEffects.Link;
                    e.Handled = true;
                    return;
                }
            }

            if (!e.Data.GetDataPresent("FileDrop"))
            {
                e.Effects = DragDropEffects.None;
            }
            else
            {
                var data = e.Data.GetData("FileDrop") as string[];
                if (data != null)
                    foreach (var file in data)
                    {
                        string extension = Path.GetExtension(file);
                        if (extension != null && extension != string.Empty)
                        {
                            var ext = extension.Substring(1).ToLowerInvariant();
                            if (!SupportedFiles.All.ContainsKey(ext))
                            {
                                e.Effects = DragDropEffects.None;
                                e.Handled = true;
                                return;
                            }
                        }
                    }
                e.Effects = DragDropEffects.Link;
            }
            e.Handled = true;
        }

        void Window1_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.OldFocus == null) return;

            Debug.WriteLine("Focus " + e.NewFocus.GetType().Name + ": " + ((e.OldFocus is FrameworkElement) ? (e.OldFocus as FrameworkElement).Name : ""));
        }

        void OnStateChanged(object sender, EventArgs args)
        {
            if (WindowState == WindowState.Minimized)
            {
                Visibility = Visibility.Hidden;
                Hide();
                if (trayIcon != null)
                    trayIcon.ShowBalloonTip("Bye", "MediaPoint is minimizing. Use the system tray to access it.", BalloonIcon.Info);
            }

            if (WindowState != WindowState.Maximized)
            {
                AnimationExtensions.AnimatePropertyTo(rotation, s => s.Angle, 0, 0.3);
                AnimationExtensions.AnimatePropertyTo(scale, s => s.ScaleX, 1, 0.3);
                AnimationExtensions.AnimatePropertyTo(scale, s => s.ScaleY, 1, 0.3);
                AnimationExtensions.AnimatePropertyTo(skew, s => s.AngleX, 0, 0.3);
                AnimationExtensions.AnimatePropertyTo(skew, s => s.AngleY, 0, 0.3);
            }
            else if (WindowState == WindowState.Maximized)
            {
                AnimationExtensions.AnimatePropertyTo(rotation, s => s.Angle, _rotation, 0.3);
                AnimationExtensions.AnimatePropertyTo(scale, s => s.ScaleX, _scaleX, 0.3);
                AnimationExtensions.AnimatePropertyTo(scale, s => s.ScaleY, _scaleY, 0.3);
                AnimationExtensions.AnimatePropertyTo(skew, s => s.AngleX, _skewX, 0.3);
                AnimationExtensions.AnimatePropertyTo(skew, s => s.AngleY, _skewY, 0.3);
            }
        }

        [DllImport("User32.dll")]
        public static extern IntPtr SendMessage(
             IntPtr hWnd, UInt32 Msg, Int32 wParam, Int32 lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (this.Visibility == Visibility.Visible)
            {
                try
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        RefreshBorderlessBehavior();
                        if (_shadower != null)
                        {
                            _shadower.SetShadowSize(0);
                            _shadower.SetShadowSize(6);
                            var hwnd = new WindowInteropHelper(this).Handle;
                            int fail = _shadower.Show(hwnd);
                        }
                    }), DispatcherPriority.ApplicationIdle);
                }
                catch { }
            }
            CheckTrayIcon();
        }

        void CheckTrayIcon()
        {
            ShowTrayIcon(!IsVisible);
        }

        void ShowTrayIcon(bool show)
        {
            if (trayIcon != null)
                trayIcon.Visibility = show ? Visibility.Visible : Visibility.Hidden;
        }

        private Size _lastMinSize;
        public bool ExecuteCommand(MainViewCommand command, object parameter = null)
        {
            bool didSomething = false;
            WindowInteropHelper winHelp = new WindowInteropHelper(this);
            Window window = this;
            switch (command)
            {
                case MainViewCommand.Close:
                    didSomething = true;
                    if (Application.Current != null) Application.Current.Shutdown();
                    break;
                case MainViewCommand.Minimize:
                    didSomething = true;
                    _lastMinSize = new Size(MinWidth, MinHeight);
                    MinWidth = 0;
                    MinHeight = 0;
                    ShowWindow(winHelp.Handle, (uint)WindowShowStyle.Hide);
                    ShowInTaskbar = false;
                    WindowState = WindowState.Minimized;
                    Visibility = Visibility.Collapsed;
                    Hide();
                    break;
                case MainViewCommand.Maximize:
                    didSomething = true;
                    window.WindowStyle = WindowStyle.None;
                    window.Topmost = true;
                    window.MaxHeight = Int32.MaxValue;
                    window.MaxWidth = Int32.MaxValue;
                    window.WindowState = WindowState.Maximized;
                    if (window.DataContext is Main)
                    {
                        (window.DataContext as Main).IsMaximized = true;
                    }
                    break;
                case MainViewCommand.Restore:
                    didSomething = true;
                    MinWidth = _lastMinSize.Width;
                    MinHeight = _lastMinSize.Height;
                    window.Topmost = false;
                    window.WindowStyle = (WindowStyle)window.Tag;
                    window.WindowState = WindowState.Normal;
                    window.Left = window.RestoreBounds.Left;
                    window.Top = window.RestoreBounds.Top;
                    window.Width = window.RestoreBounds.Width;
                    window.Height = window.RestoreBounds.Height;
                    window.Visibility = System.Windows.Visibility.Visible;
                    ShowInTaskbar = true;
                    ShowWindow(winHelp.Handle, 1);

                    if (window.DataContext is Main)
                    {
                        (window.DataContext as Main).IsMaximized = false;
                    }

                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        foreach (Window w in OwnedWindows)
                        {
                            if (w.IsLoaded && w.Content != null)
                            {
                                //w.Hide();
                                w.Show();
                            }
                        }
                    }), DispatcherPriority.ApplicationIdle);

                    break;
                case MainViewCommand.DecreasePanScan:
                    Dispatcher.BeginInvoke((Action)(()=>
                    {
                        PanScan(-1 * _scaleX / 10);
                    }), DispatcherPriority.ApplicationIdle);
                    break;
                case MainViewCommand.IncreasePanScan:
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        PanScan(_scaleX / 10);
                    }), DispatcherPriority.ApplicationIdle);
                    break;
            }

            return didSomething;
        }

        void PanScan(double delta)
        {
            if (scale.ScaleX + delta > 0.2 && scale.ScaleX + delta < 3)
            {
                _scaleX = scale.ScaleX + delta;
                _scaleY = scale.ScaleY + delta;
                scale.AnimatePropertyTo(s => s.ScaleX, scale.ScaleX + delta, 0.1);
                scale.AnimatePropertyTo(s => s.ScaleY, scale.ScaleY + delta, 0.1);
            }
        }

        /// <summary>Enumeration of the different ways of showing a window using
        /// ShowWindow</summary>
        private enum WindowShowStyle : uint
        {
            /// <summary>Hides the window and activates another window.</summary>
            /// <remarks>See SW_HIDE</remarks>
            Hide = 0,
            ShowMaximized = 3,
            /// <summary>Activates and displays the window. If the window is
            /// minimized or maximized, the system restores it to its original size
            /// and position. An application should specify this flag when restoring
            /// a minimized window.</summary>
            /// <remarks>See SW_RESTORE</remarks>
            Restore = 9,
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool IntersectRect(out RECT lprcDst, [In] ref RECT lprcSrc1,
           [In] ref RECT lprcSrc2);

        void HideUi()
        {
            this.Focus();

            bool hideControls = true;
            bool hideOverlay = true;
            if (WindowState == WindowState.Normal)
            {
                hideControls = Autohide.GetNormalMode(mediaControls);
                hideOverlay = Autohide.GetNormalMode(windowControls);
            }
            else if (WindowState == WindowState.Maximized)
            {
                hideControls = Autohide.GetFullScreen(mediaControls);
                hideOverlay = Autohide.GetFullScreen(windowControls);
            }

            if (hideOverlay) FadeTo(windowControls, 0);
            IsUiVisible = false;

            if (hideControls)
            {
                var va = mediaControls.FindCommonVisualAncestor(this);
                if (WindowState == System.Windows.WindowState.Maximized || va != null)
                {
                    IsControlsVisible = false;
                    FadeTo(mediaControls, 0);
                }
                else
                {
                    Window w2 = mediaControls.TryFindParent<Window>();
                    RECT r1;
                    GetWindowRect(new HandleRef(this, _hwnd), out r1);
                    RECT r2;
                    GetWindowRect(new HandleRef(this, new WindowInteropHelper(w2).Handle), out r2);
                    RECT intersection;
                    if (IntersectRect(out intersection, ref r1, ref r2))
                    {
                        var width = intersection.Right - intersection.Left;
                        var height = intersection.Bottom - intersection.Top;
                        if (width > 0 &&
                            height > 0)
                        {
                            IsControlsVisible = false;
                            FadeTo(mediaControls, 0);
                        }
                    }
                }
            }
        }

        void ShowUi()
        {
            FadeTo(mediaControls, 1);
            FadeTo(windowControls, 1);
            IsControlsVisible = true;
            IsUiVisible = true;
        }

        public void UnregisterEventsOnControls()
        {
            playlist.MouseEnter -= MediaControlsOnMouseEnter;
            playlist.MouseLeave -= MediaControlsOnMouseLeave;

            mediaControls.MouseEnter -= MediaControlsOnMouseEnter;
            mediaControls.MouseLeave -= MediaControlsOnMouseLeave;

            windowControls.MouseEnter -= MediaControlsOnMouseEnter;
            windowControls.MouseLeave -= MediaControlsOnMouseLeave;

            imdbOverlay.MouseEnter -= MediaControlsOnMouseEnter;
            imdbOverlay.MouseLeave -= MediaControlsOnMouseLeave;

            onlineSubs.MouseEnter -= MediaControlsOnMouseEnter;
            onlineSubs.MouseLeave -= MediaControlsOnMouseLeave;

            equalizer.MouseEnter -= MediaControlsOnMouseEnter;
            equalizer.MouseLeave -= MediaControlsOnMouseLeave;

            options.MouseEnter -= MediaControlsOnMouseEnter;
            options.MouseLeave -= MediaControlsOnMouseLeave;
        }

        public void RegisterEventsOnControls()
        {
            playlist.MouseEnter += MediaControlsOnMouseEnter;
            playlist.MouseLeave += MediaControlsOnMouseLeave;
            playlist.IsVisibleChanged += uiPanel_IsVisibleChanged;

            mediaControls.MouseEnter += MediaControlsOnMouseEnter;
            mediaControls.MouseLeave += MediaControlsOnMouseLeave;

            windowControls.MouseEnter += MediaControlsOnMouseEnter;
            windowControls.MouseLeave += MediaControlsOnMouseLeave;
            windowControls.IsVisibleChanged += uiPanel_IsVisibleChanged;

            imdbOverlay.MouseEnter += MediaControlsOnMouseEnter;
            imdbOverlay.MouseLeave += MediaControlsOnMouseLeave;
            imdbOverlay.IsVisibleChanged += uiPanel_IsVisibleChanged;

            onlineSubs.MouseEnter += MediaControlsOnMouseEnter;
            onlineSubs.MouseLeave += MediaControlsOnMouseLeave;
            onlineSubs.IsVisibleChanged += uiPanel_IsVisibleChanged;

            equalizer.MouseEnter += MediaControlsOnMouseEnter;
            equalizer.MouseLeave += MediaControlsOnMouseLeave;
            equalizer.IsVisibleChanged += uiPanel_IsVisibleChanged;

            options.MouseEnter += MediaControlsOnMouseEnter;
            options.MouseLeave += MediaControlsOnMouseLeave;
            options.IsVisibleChanged += uiPanel_IsVisibleChanged;
        }

        void uiPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //if ((bool)e.NewValue != true) mediaControls.Focus();
        }

        public override void OnApplyTemplate()
        {
            UnregisterEventsOnControls();
            base.OnApplyTemplate();
            RegisterEventsOnControls();
            FindElementsToAutoRefresh();
        }

        public void RefreshUIElements()
        {
            if (_elementsToRefresh.Count == 0)
            {
                FindElementsToAutoRefresh();
            }

            foreach (var item in _elementsToRefresh)
            {
                var ex = item.GetBindingExpression(ToggleButton.IsCheckedProperty);
                ex.UpdateTarget();
            }
        }

        private void FindElementsToAutoRefresh()
        {
            var el = VisualHelper.FindChildren<ToggleButton>(this);
            _elementsToRefresh.Clear();
            foreach (var element in el)
            {
                _elementsToRefresh.Add(element);
            }

            foreach (var wnd in this.OwnedWindows.OfType<Window>())
            {
                var el2 = VisualHelper.FindChildren<ToggleButton>(wnd);
                foreach (var element in el2)
                {
                    _elementsToRefresh.Add(element);
                }                
            }

        }

        private void MediaControlsOnMouseLeave(object sender, MouseEventArgs e)
        {
            _timeToDelayReShowing = DateTime.Now + TimeSpan.FromMilliseconds(800);
            _isOverUiControl = false;
            if (DataContext == null) return;
            var dc = DataContext as Main;
            if (dc != null && dc.Player.HasVideo && !dc.IsOptionsVisible) HideUi();
        }

        private void MediaControlsOnMouseEnter(object sender, MouseEventArgs mouseEventArgs)
        {
            _isOverUiControl = true;
            if (Cursor == Cursors.None) Cursor = Cursors.Arrow;
            ShowUi();
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            var diff = DateTime.Now - LastMouseMove;

            if (DataContext == null) return;

            var dc = DataContext as Main;

            if (diff >= TimeoutToHide && (IsUiVisible || Cursor != Cursors.None))
            {
                if (dc != null && (!_isOverUiControl && dc.Player.HasVideo && !dc.IsOptionsVisible))
                {
                    Cursor = Cursors.None;
                    HideUi();
                }
            }
        }

        bool _isOverUiControl;

        public DateTime LastMouseMove { get; set; }

        public bool IsUiVisible
        {
            get { return (bool)GetValue(IsUiVisibleProperty); }
            set { SetValue(IsUiVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsUIVIsible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsUiVisibleProperty =
            DependencyProperty.Register("IsUiVisible", typeof(bool), typeof(Window1), new PropertyMetadata(true));

        public bool IsControlsVisible
        {
            get { return (bool)GetValue(IsControlsVisibleProperty); }
            set { SetValue(IsControlsVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsUIVIsible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsControlsVisibleProperty =
            DependencyProperty.Register("IsControlsVisible", typeof(bool), typeof(Window1), new PropertyMetadata(true));


        private TimeSpan TimeoutToHide
        {
            get { return TimeSpan.FromSeconds(3); }
        }

        private void FadeTo(UIElement element, double value)
        {
            if (value > 0)
            {
                element.Visibility = Visibility.Visible;
            }

            var da = new DoubleAnimation
                     {
                         From = element.Opacity,
                         To = value,
                         Duration = new Duration(TimeSpan.FromSeconds(1)),
                         AutoReverse = false,
                         FillBehavior = FillBehavior.HoldEnd
                     };
            element.BeginAnimation(OpacityProperty, da, HandoffBehavior.SnapshotAndReplace);
        }

        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(this);

            var delta = Math.Sqrt(2) * (Math.Abs(_lastpoint.X - p.X) + Math.Abs(_lastpoint.Y - p.Y));
            _lastpoint = p;

            if (DateTime.Now < _timeToDelayReShowing)
            {
                return;
            }

            if (delta < 6 || DateTime.Now < LastMouseMove) return;
            CommandManager.InvalidateRequerySuggested();
            LastMouseMove = DateTime.Now;
            if (!IsUiVisible)
            {
                Cursor = Cursors.Arrow;
                ShowUi();
            }

        }

        private void MediaPlayer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var delta = (double)e.Delta / 1200;
            if (((Keyboard.GetKeyStates(Key.LeftCtrl) == KeyStates.Down ||
                Keyboard.GetKeyStates(Key.RightCtrl) == KeyStates.Down)
                && (Keyboard.GetKeyStates(Key.LeftShift) != KeyStates.Down && Keyboard.GetKeyStates(Key.RightShift) != KeyStates.Down)) ||
                (e.LeftButton != MouseButtonState.Pressed && e.RightButton == MouseButtonState.Pressed && Keyboard.GetKeyStates(Key.LeftShift) != KeyStates.Down && Keyboard.GetKeyStates(Key.RightShift) != KeyStates.Down))
            {
                if (WindowState != WindowState.Maximized) return;
                _rotation = rotation.Angle + delta * 30;
                rotation.AnimatePropertyTo(s => s.Angle, rotation.Angle + delta * 30, 0.3);
            }
            else if ((Keyboard.GetKeyStates(Key.LeftShift) == KeyStates.Down ||
                            Keyboard.GetKeyStates(Key.RightShift) == KeyStates.Down) &&
                            (e.RightButton == MouseButtonState.Pressed))
            {
                if (WindowState != WindowState.Maximized) return;
                _skewX = skew.AngleX + delta * 30;
                skew.AnimatePropertyTo(s => s.AngleX, skew.AngleX + delta * 30, 0.3);
            }
            else if ((Keyboard.GetKeyStates(Key.LeftShift) == KeyStates.Down ||
                            Keyboard.GetKeyStates(Key.RightShift) == KeyStates.Down) &&
                            (e.RightButton != MouseButtonState.Pressed))
            {
                if (WindowState != WindowState.Maximized) return;
                _skewY = skew.AngleY + delta * 30;
                skew.AnimatePropertyTo(s => s.AngleY, skew.AngleY + delta * 30, 0.3);
            }
            else
            {
                if (delta > 0)
                    ServiceLocator.GetService<IActionExecutor>().ExecuteAction(PlayerActionEnum.IncreaseVolume);
                else
                    ServiceLocator.GetService<IActionExecutor>().ExecuteAction(PlayerActionEnum.DecreaseVolume);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            trayIcon.Dispose();
            base.OnClosing(e);
            Application.Current.Shutdown();
        }

        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            OnPropertyChanged("IsPlaying");
        }

        private void MediaPlayer_MediaClosed(object sender, RoutedEventArgs e)
        {
            OnPropertyChanged("IsPlaying");
            if (DataContext is Main)
            {
                ((Main)DataContext).Player.OnMediaEnded();
            }
        }

        private void OnPropertyChanged(string p)
        {
            var pc = PropertyChanged;
            if (pc != null)
                pc(this, new PropertyChangedEventArgs(p));
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            OnPropertyChanged("IsPlaying");
            if (DataContext is Main)
            {
                ((Main)DataContext).Player.OnMediaEnded();
            }
        }

        private void MediaPlayer_MediaFailed(object sender, MediaFailedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (e.Exception is COMException)
                {
                    switch (((COMException)e.Exception).ErrorCode)
                    {
                        case -2147220877:
                            MessageBox.Show(@"It seems that your graphic card does not have the required capabilities to play movies using DirectShow.
Ensure that the latest graphic drivers and DirectX are installed and try again.

If you are not running a 'virtual machine' (which is unsupported) ensure that you have at least Windows Vista and a graphics card fully capable of DirectX 9.0c.", "Media failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                    }
                    MessageBox.Show(@"Com exception", "Media failed - error code: " + ((COMException)e.Exception).ErrorCode, MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (DataContext is Main)
                {
                    (DataContext as Main).ShowOsdMessage("Media failed: " + e.Exception.Message);
                }
            }));
        }

        private bool _animating;
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ParentWndMove(this, this.OwnedWindows.OfType<Window>().ToArray());

            var mc = FindName("mediaControls") as MediaControls;
            if (mc != null && (!(mc.RenderTransform is MatrixTransform) && !(mc.RenderTransform is TranslateTransform))) return;
            if (_animating) return;
            _animating = true;
            if (mc != null)
            {
                Transform oTransform = mc.RenderTransform;
                double x;
                double y;
                var transform = oTransform as MatrixTransform;
                if (transform != null)
                {
                    x = transform.Matrix.OffsetX;
                    y = transform.Matrix.OffsetY;
                }
                else
                {
                    x = ((TranslateTransform)oTransform).X;
                    y = ((TranslateTransform)oTransform).Y;
                }
                var dbXZero = new DoubleAnimation(x, 0, new Duration(TimeSpan.FromMilliseconds(300)));
                var dbYZero = new DoubleAnimation(y, 0, new Duration(TimeSpan.FromMilliseconds(300)));
                mc.RenderTransform = new TranslateTransform(x, y);
                var storyboard = new Storyboard();
                storyboard.Children.Add(dbXZero);
                storyboard.Children.Add(dbYZero);
                Storyboard.SetTarget(dbXZero, mc);
                Storyboard.SetTargetProperty(dbXZero, new PropertyPath("RenderTransform.X"));
                Storyboard.SetTarget(dbYZero, mc);
                Storyboard.SetTargetProperty(dbYZero, new PropertyPath("RenderTransform.Y"));
                storyboard.FillBehavior = FillBehavior.Stop;
                storyboard.AutoReverse = false;
                storyboard.Completed += (o, args) =>
                                        {
                                            mc.RenderTransform = new MatrixTransform(Matrix.Identity);
                                            _animating = false;
                                        };
                storyboard.Begin();
            }


            if (Mouse.Captured != null)
            {
                Mouse.Captured.ReleaseMouseCapture();
            }

            //Dispatcher.BeginInvoke((Action)(() => {
            //    if (_shadower != null) _shadower.SetShadowSize(6);
            //}), DispatcherPriority.Normal);
        }

        private bool OneClick { get; set; }
        private bool TwoClick { get; set; }

        private void MediaPlayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (OneClick)
            {
                // a slow single click is pause/play, but a fast double click is maximize
                TwoClick = true;
                return;
            }

            OneClick = true;
            TwoClick = false;

            var t = new Thread(() =>
            {
                Thread.Sleep(200);
                OneClick = false;

                if (TwoClick)
                {
                    // a slow single click is pause/play, but a fast double click is maximize
                    TwoClick = false;
                    return;
                }

                TwoClick = false;

                var now = DateTime.Now;
                if (now - LastDragTime() > TimeSpan.FromMilliseconds(300))
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (mediaPlayer.IsPlaying)
                            mediaPlayer.Pause();
                        else
                        {
                            mediaPlayer.Play();
                        }
                    }));
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void MediaPlayer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext == null) return;
            var dc = DataContext as Main;
            if (dc != null && dc.Player.HasVideo && !dc.IsOptionsVisible) HideUi();
        }

        private void MediaPlayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (OneClick)
            {
                // a slow single click is pause/play, but a fast double click is maximize
                TwoClick = true;
            }
        }

        public void Invoke(Action action)
        {
            Dispatcher.Invoke(action, DispatcherPriority.Send);
        }

        public void DelayedInvoke(Action action, int millisenconds = 100)
        {
            //Timer t = new Timer((o) =>
            //{
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, action);
            //}, null, millisenconds, 0);
        }

        private ThumbnailToolBarButton[] _buttons;
        private bool _once;
        private bool _onceDone;
        private unsafe void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ServiceLocator.RegisterOverrideService(this as ISpectrumVisualizer);

            if (!_once)
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    _once = true;
                    _originalStyle = WindowStyle;
                    Tag = _originalStyle;
                    var ih = new WindowInteropHelper(this);
                    IntPtr hwnd = ih.Handle;
                    TaskbarManager.Instance.ThumbnailToolBars.AddButtons(hwnd, _buttons);
                    _lastMinSize = new Size(MinWidth, MinHeight);
                    HwndSource source = HwndSource.FromVisual(this) as HwndSource;

                    if (source != null)
                    {
                        var _hook = new HwndSourceHook(WndProc);
                        source.AddHook(_hook);
                    }

                    Visibility = Visibility.Visible;


                    _onceDone = true;
                }), DispatcherPriority.ApplicationIdle);

            RefreshBorderlessBehavior();
            RefreshChildWindows();
        }

        unsafe private void RefreshChildWindows()
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                foreach (Window w in OwnedWindows)
                {
                    if (w.IsLoaded && w.IsInitialized)
                    {
                        w.Activate();
                        w.Focus();
                    }
                }
                Activate();
            }), DispatcherPriority.ApplicationIdle);
        }

        public void RefreshBorderlessBehavior()
        {
            bool isLoading = false;
            BehaviorCollection itemBehaviors = Interaction.GetBehaviors(this);
            if (itemBehaviors.Any(b => b is BorderlessWindowBehavior) == true)
            {
                try
                {
                    itemBehaviors.Remove(itemBehaviors.First(b => b is BorderlessWindowBehavior));
                }
                catch
                {
                    isLoading = true;
                }
            }
            if (!isLoading)
            {
                var bwb = new BorderlessWindowBehavior();
                itemBehaviors.Add(bwb);
                bwb.ResizeWithGrip = (WindowStyle == System.Windows.WindowStyle.None);
            }
        }

        private const Int32 WM_EXITSIZEMOVE = 0x0232;
        private const Int32 WM_SIZING = 0x0214;
        private const Int32 WM_SIZE = 0x0005;

        private const Int32 SIZE_RESTORED = 0x0000;
        private const Int32 SIZE_MINIMIZED = 0x0001;
        private const Int32 SIZE_MAXIMIZED = 0x0002;
        private const Int32 SIZE_MAXSHOW = 0x0003;
        private const Int32 SIZE_MAXHIDE = 0x0004;
        private const Int32 WM_APPCOMMAND = 0x0319;
        private const Int32 HSHELL_APPCOMMAND = 12;
        private const Int32 APPCOMMAND_LAUNCH_MEDIA_SELECT = 17;
        private const Int32 APPCOMMAND_LAUNCH_MEDIA_SELECT2 = 1048576;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr DefWindowProc(
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam);

        private IntPtr WndProc(IntPtr hwnd, Int32 msg, IntPtr wParam, IntPtr lParam, ref Boolean handled)
        {
            IntPtr result = IntPtr.Zero;

            switch (msg)
            {
                case WM_SIZING:             // sizing gets interactive resize
                    //OnResizing();
                    //if (_shadower != null) _shadower.SetShadowSize(0);
                    break;
                case WM_APPCOMMAND:
                    if ((int)lParam == APPCOMMAND_LAUNCH_MEDIA_SELECT || (int)lParam == APPCOMMAND_LAUNCH_MEDIA_SELECT2)
                    {
                        handled = true;
                    }
                    break;
                case WM_SIZE:               // size gets minimize/maximize as well as final size
                    {
                        int param = wParam.ToInt32();

                        switch (param)
                        {
                            case SIZE_RESTORED:
                                //OnRestored();
                                break;
                            case SIZE_MINIMIZED:
                                //OnMinimized();
                                break;
                            case SIZE_MAXIMIZED:
                                //OnMaximized();
                                break;
                            case SIZE_MAXSHOW:
                                break;
                            case SIZE_MAXHIDE:
                                break;
                        }
                    }
                    break;

                case WM_EXITSIZEMOVE:
                    //OnResized();
                    //if (_shadower != null) _shadower.SetShadowSize(6);
                    break;
            }

            return result;
        }

        IWindowShadow _shadower;

        private unsafe void Window_Initialized(object sender, EventArgs e)
        {
            ServiceLocator.RegisterOverrideService(mediaPlayer as IPlayerView);

            var bOpen = new ThumbnailToolBarButton(Properties.Resources.eject, "Open");
            var bPlay = new ThumbnailToolBarButton(Properties.Resources.play, "Play/Pause");
            var bStop = new ThumbnailToolBarButton(Properties.Resources.stop, "Stop");
            var bPrev = new ThumbnailToolBarButton(Properties.Resources.prev, "Previous");
            var bNext = new ThumbnailToolBarButton(Properties.Resources.next, "Next");
            var bVolup = new ThumbnailToolBarButton(Properties.Resources.volup, "Increase volume");
            var bVolDown = new ThumbnailToolBarButton(Properties.Resources.voldown, "Decrease volume");

            _buttons = new[] { bOpen, bPrev, bPlay, bStop, bNext, bVolDown, bVolup };

            bPlay.Click += (o, args) => ButtonClicked("play");
            bOpen.Click += (o, args) => ButtonClicked("open");
            bPrev.Click += (o, args) => ButtonClicked("prev");
            bStop.Click += (o, args) => ButtonClicked("stop");
            bNext.Click += (o, args) => ButtonClicked("next");
            bVolup.Click += (o, args) => ButtonClicked("volumeup");
            bVolDown.Click += (o, args) => ButtonClicked("volumedown");
        }

        public void UpdateTaskbarButtons()
        {
            var dc = DataContext as Main;
            if (dc == null || dc.Player == null) return;
            var p = dc.Player;

            _buttons[0].Enabled = true;
            _buttons[1].Enabled = false;
            _buttons[2].Enabled = p.Source != null && (!p.IsStopped);
            _buttons[3].Enabled = p.Source != null && (p.IsPlaying || p.IsPaused);
            _buttons[4].Enabled = false;
            _buttons[5].Enabled = p.Volume > 0;
            _buttons[6].Enabled = p.Volume < 1;
        }

        private void ButtonClicked(string button)
        {
            var dc = DataContext as Main;
            if (dc == null || dc.Player == null) return;

            switch (button)
            {
                case "play":
                    dc.Player.PlayCommand.Execute(null);
                    break;
                case "open":
                    dc.Player.OpenCommand.Execute(null);
                    break;
                case "stop":
                    dc.Player.StopCommand.Execute(null);
                    break;
                case "volumeup":
                    dc.Player.Volume += 0.1;
                    break;
                case "volumedown":
                    dc.Player.Volume -= 0.1;
                    break;
                case "prev":
                    dc.Playlist.PreviousCommand.Execute(null);
                    break;
                case "next":
                    dc.Playlist.NextCommand.Execute(null);
                    break;
            }
        }

        public Window GetWindow()
        {
            return this;
        }

        void DoPreEmphesis(float[] data, float value)
        {
            int i;
            float f, ac0;

            f = value * data[0];
            for (i = 1; i < data.Length; i++)
            {
                ac0 = value * data[i];
                data[i] = data[i] - f;
                f = ac0;
            }
        }

        //void DoLogarithmic(float[] data, float minY, float maxY)
        //{
        //    float range = maxY - minY;

        //    for (int i = 1; i < data.Length; i++)
        //    {
        //        data[i] = (float)Math.Sqrt(data[i] * range);
        //        data[i] = data[i] + minY;
        //    }

        //}

        public bool GetFFTData(float[] fftDataBuffer)
        {
            if (visualizations.Visibility != System.Windows.Visibility.Visible) return false;

            _sampleAggregator.GetFFTResults(fftDataBuffer);
            DoPreEmphesis(fftDataBuffer, 90f / 100);
            //DoLogarithmic(fftDataBuffer, 0.0f, 1f);
            return IsPlaying;
        }

        readonly SampleAggregator _sampleAggregator = new SampleAggregator((int)FFTDataSize.FFT2048);

        public int GetFFTFrequencyIndex(int frequency)
        {
            double maxFrequency;
            if (_frequency > 0)
                maxFrequency = _frequency / 2.0d;
            else
                maxFrequency = 22050; // Assume a default 44.1 kHz sample rate./2
            return (int)((frequency / maxFrequency) * ((int)FFTDataSize.FFT2048 / 2)); // only real
        }

        public bool IsPlaying
        {
            get
            {
                return mediaPlayer.IsPlaying;
            }
        }

        int _numSamples;
        public void SetNumSamples(int num)
        {
            _numSamples = num;
            _sampleAggregator.Clear();
        }

        float[] _data;
        public void DisplayFFTData(float[] data)
        {
            if (visualizations.Visibility != System.Windows.Visibility.Visible) return;

            OnPropertyChanged("IsPlaying");
            _data = data;
            if (data == null) return;
            for (int i = 0; i < data.Length; i++)
                _sampleAggregator.Add(data[i], data[i]);
        }

        int _channels, _bits, _frequency;
        public void SetStreamInfo(int channels, int bits, int frequency)
        {
            _channels = channels;
            _bits = bits;
            _frequency = frequency;
            var sa = visualizations.TryFindSpectrumAnalyzer();
            foreach (var s in sa) s.RegisterSoundPlayer(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private System.Windows.WindowStyle _originalStyle;

        Window v;
        private void window_Activated(object sender, EventArgs e)
        {
            //if (v == null)
            //{
            //    v = new Plates();
            //    v.DataContext = Application.Current;
            //    v.Owner = this;
            //    v.Show();
            //}

            if (Mouse.Captured != null)
            {
                Mouse.Captured.ReleaseMouseCapture();
            }
            parentWindowPosition.X = Left + (ActualWidth / 2);
            parentWindowPosition.Y = Top + (ActualHeight / 2);
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();

        public bool IsInInputControl
        {
            get
            {
                IntPtr active = GetActiveWindow();

                var activeWindow = Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(window => new WindowInteropHelper(window).Handle == active);

                if (activeWindow == null) return false;

                var focusedControl = FocusManager.GetFocusedElement(activeWindow);

                if (focusedControl == null) return false;

                if (IsInInput(focusedControl))
                {
                    return true;
                }

                return false;
            }
        }

        private bool IsInInput(IInputElement e)
        {
            return (e is System.Windows.Controls.TextBox ||
            e is System.Windows.Controls.ComboBox ||
            e is System.Windows.Controls.ListBox ||
            e is System.Windows.Controls.ListView) && e != mediaControls;
        }

        public System.Windows.Media.Imaging.BitmapSource GetBitmapOfVideoElement()
        {
            return mediaPlayer.GetBitmapOfVideoElement();
        }

        public void SetChildWindowsFollow(bool value)
        {
            _childWindowsFollow = value;
            RefreshChildWindows();
        }

        public IntPtr GetWindowHandle()
        {
            return _hwnd;
        }

        private void window_Closed(object sender, CancelEventArgs e)
        {
            foreach (Window w in OwnedWindows)
            {
                w.Close();
            }
        }

        DateTime _lastDrag = DateTime.Now;
        public void NotifyDragged()
        {
            _lastDrag = DateTime.Now;
        }

        public DateTime LastDragTime()
        {
            return _lastDrag;
        }
    }
}
