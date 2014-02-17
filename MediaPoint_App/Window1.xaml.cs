using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MediaPoint.App.AttachedProperties;
using MediaPoint.Common.TaskbarNotification;
using MediaPoint.Controls.Extensions;
using MediaPoint.VM.ViewInterfaces;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MediaPoint.App.Extensions;
using MediaPoint.VM;
using MediaPoint.Controls;
using MediaPoint.MVVM.Services;
using Microsoft.WindowsAPICodePack.Taskbar;
using WPFSoundVisualizationLib;
using MediaPoint.App.Audio;
using MediaPoint.Common.Helpers;
using System.Windows.Controls.Primitives;
using System.Collections.Generic;
using MediaPoint.VM.Config;
using System.IO;

namespace MediaPoint.App
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : IMainView, ISpectrumPlayer, ISpectrumVisualizer
    {
        private WindowState m_storedWindowState = WindowState.Normal;
        private double _skewX = 0;
        private double _skewY = 0;
        private double _scaleX = 1;
        private double _scaleY = 1;
        private double _rotation = 0;
        private readonly System.Drawing.Icon _icon = new System.Drawing.Icon(Properties.Resources.app, new System.Drawing.Size(32, 32));
        private SynchronizationContext _sync = SynchronizationContext.Current;

        private string _startFile;
        public string StartupFile
        {
            get { return _startFile; }
            set {
                _startFile = value;

                Action<string> load = ((s) =>
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

        public Window1()
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed;
            DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);
            timer.Tick += new System.EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 3);
            timer.Start();

            PreviewGotKeyboardFocus += Window1_PreviewGotKeyboardFocus;

            Layout.PreviewDragEnter += Window1_PreviewDragEnter;
            Layout.PreviewDragLeave += Window1_PreviewDragLeave;
            Layout.PreviewDragOver += Layout_PreviewDragOver;
            Layout.PreviewDrop += Window1_PreviewDrop;
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
            foreach (var file in data)
            {
                var ext = Path.GetExtension(file).Substring(1).ToLowerInvariant();
                if (!SupportedFiles.All.ContainsKey(ext))
                {
                    return;
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
            if (!e.Data.GetDataPresent("FileDrop"))
            {
                e.Effects = DragDropEffects.None;
            }
            else
            {
                var data = e.Data.GetData("FileDrop") as string[];
                foreach (var file in data)
                {
                    var ext = Path.GetExtension(file).Substring(1).ToLowerInvariant();
                    if (!SupportedFiles.All.ContainsKey(ext))
                    {
                        e.Effects = DragDropEffects.None;
                        e.Handled = true;
                        return;
                    }
                }
                e.Effects = DragDropEffects.Link;
            }
            e.Handled = true;
        }

        void Window1_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Debug.WriteLine("Focus " + e.NewFocus.GetType().Name + ": " + ((e.NewFocus is FrameworkElement) ? (e.NewFocus as FrameworkElement).Name : ""));
        }

        void m_notifyIcon_Click(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
            Show();
            WindowState = m_storedWindowState;
            Cursor = Cursors.Arrow;
            ShowUI();
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
            else
                m_storedWindowState = WindowState;

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
        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
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

        private Point _restorePos;
        private Size _lastMinSize;
        public bool ExecuteCommand(MainViewCommand command, object parameter = null)
        {
            bool didSomething = false;
            WindowInteropHelper winHelp = new WindowInteropHelper(this);

            switch (command)
            {
                case MainViewCommand.Close:
                    didSomething = true;
                    Application.Current.Shutdown();
                    break;
                case MainViewCommand.Minimize:
                    didSomething = true;
                    _restorePos = new Point(Left, Top);
                    _lastMinSize = new Size(MinWidth, MinHeight);
                    MinWidth = 0;
                    MinHeight = 0;
                    ShowWindow(winHelp.Handle, (uint)WindowShowStyle.Hide);
                    this.ShowInTaskbar = false;
                    this.WindowState = WindowState.Minimized;
                    this.Visibility = Visibility.Collapsed;
                    Hide();
                    break;
                case MainViewCommand.Maximize:
                    didSomething = true;
                    this.ShowInTaskbar = true;
                    this.Visibility = Visibility.Visible;
                    MaxWidth = Int32.MaxValue;
                    MaxHeight = Int32.MaxValue;
                    this.WindowState = System.Windows.WindowState.Maximized;
                    SetForegroundWindow(winHelp.Handle);
                    break;
                case MainViewCommand.Restore:
                    didSomething = true;
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    MinWidth = _lastMinSize.Width;
                    MinHeight = _lastMinSize.Height;
                    SetForegroundWindow(winHelp.Handle);
                    ShowWindow(winHelp.Handle, (uint)WindowShowStyle.Restore);
                    this.WindowState = WindowState.Normal;
                    this.ShowInTaskbar = true;
                    this.Show();

                    break;
            }

            return didSomething;
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        /// <summary>Enumeration of the different ways of showing a window using
        /// ShowWindow</summary>
        private enum WindowShowStyle : uint
        {
            /// <summary>Hides the window and activates another window.</summary>
            /// <remarks>See SW_HIDE</remarks>
            Hide = 0,
            /// <summary>Activates and displays a window. If the window is minimized
            /// or maximized, the system restores it to its original size and
            /// position. An application should specify this flag when displaying
            /// the window for the first time.</summary>
            /// <remarks>See SW_SHOWNORMAL</remarks>
            ShowNormal = 1,
            /// <summary>Activates the window and displays it as a minimized window.</summary>
            /// <remarks>See SW_SHOWMINIMIZED</remarks>
            ShowMinimized = 2,
            /// <summary>Activates the window and displays it as a maximized window.</summary>
            /// <remarks>See SW_SHOWMAXIMIZED</remarks>
            ShowMaximized = 3,
            /// <summary>Maximizes the specified window.</summary>
            /// <remarks>See SW_MAXIMIZE</remarks>
            Maximize = 3,
            /// <summary>Displays a window in its most recent size and position.
            /// This value is similar to "ShowNormal", except the window is not
            /// actived.</summary>
            /// <remarks>See SW_SHOWNOACTIVATE</remarks>
            ShowNormalNoActivate = 4,
            /// <summary>Activates the window and displays it in its current size
            /// and position.</summary>
            /// <remarks>See SW_SHOW</remarks>
            Show = 5,
            /// <summary>Minimizes the specified window and activates the next
            /// top-level window in the Z order.</summary>
            /// <remarks>See SW_MINIMIZE</remarks>
            Minimize = 6,
            /// <summary>Displays the window as a minimized window. This value is
            /// similar to "ShowMinimized", except the window is not activated.</summary>
            /// <remarks>See SW_SHOWMINNOACTIVE</remarks>
            ShowMinNoActivate = 7,
            /// <summary>Displays the window in its current size and position. This
            /// value is similar to "Show", except the window is not activated.</summary>
            /// <remarks>See SW_SHOWNA</remarks>
            ShowNoActivate = 8,
            /// <summary>Activates and displays the window. If the window is
            /// minimized or maximized, the system restores it to its original size
            /// and position. An application should specify this flag when restoring
            /// a minimized window.</summary>
            /// <remarks>See SW_RESTORE</remarks>
            Restore = 9,
            /// <summary>Sets the show state based on the SW_ value specified in the
            /// STARTUPINFO structure passed to the CreateProcess function by the
            /// program that started the application.</summary>
            /// <remarks>See SW_SHOWDEFAULT</remarks>
            ShowDefault = 10,
            /// <summary>Windows 2000/XP: Minimizes a window, even if the thread
            /// that owns the window is hung. This flag should only be used when
            /// minimizing windows from a different thread.</summary>
            /// <remarks>See SW_FORCEMINIMIZE</remarks>
            ForceMinimized = 11
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        static readonly IntPtr HWND_TOP = new IntPtr(0);
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        /// <summary>
        /// Window handles (HWND) used for hWndInsertAfter
        /// </summary>
        public static class HWND
        {
            public static IntPtr
            NoTopMost = new IntPtr(-2),
            TopMost = new IntPtr(-1),
            Top = new IntPtr(0),
            Bottom = new IntPtr(1);
        }

        [Flags]
        public enum SetWindowPosFlags : uint
        {
            // ReSharper disable InconsistentNaming

            /// <summary>
            ///     If the calling thread and the thread that owns the window are attached to different input queues, the system posts the request to the thread that owns the window. This prevents the calling thread from blocking its execution while other threads process the request.
            /// </summary>
            SWP_ASYNCWINDOWPOS = 0x4000,

            /// <summary>
            ///     Prevents generation of the WM_SYNCPAINT message.
            /// </summary>
            SWP_DEFERERASE = 0x2000,

            /// <summary>
            ///     Draws a frame (defined in the window's class description) around the window.
            /// </summary>
            SWP_DRAWFRAME = 0x0020,

            /// <summary>
            ///     Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's size is being changed.
            /// </summary>
            SWP_FRAMECHANGED = 0x0020,

            /// <summary>
            ///     Hides the window.
            /// </summary>
            SWP_HIDEWINDOW = 0x0080,

            /// <summary>
            ///     Does not activate the window. If this flag is not set, the window is activated and moved to the top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
            /// </summary>
            SWP_NOACTIVATE = 0x0010,

            /// <summary>
            ///     Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client area are saved and copied back into the client area after the window is sized or repositioned.
            /// </summary>
            SWP_NOCOPYBITS = 0x0100,

            /// <summary>
            ///     Retains the current position (ignores X and Y parameters).
            /// </summary>
            SWP_NOMOVE = 0x0002,

            /// <summary>
            ///     Does not change the owner window's position in the Z order.
            /// </summary>
            SWP_NOOWNERZORDER = 0x0200,

            /// <summary>
            ///     Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered as a result of the window being moved. When this flag is set, the application must explicitly invalidate or redraw any parts of the window and parent window that need redrawing.
            /// </summary>
            SWP_NOREDRAW = 0x0008,

            /// <summary>
            ///     Same as the SWP_NOOWNERZORDER flag.
            /// </summary>
            SWP_NOREPOSITION = 0x0200,

            /// <summary>
            ///     Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
            /// </summary>
            SWP_NOSENDCHANGING = 0x0400,

            /// <summary>
            ///     Retains the current size (ignores the cx and cy parameters).
            /// </summary>
            SWP_NOSIZE = 0x0001,

            /// <summary>
            ///     Retains the current Z order (ignores the hWndInsertAfter parameter).
            /// </summary>
            SWP_NOZORDER = 0x0004,

            /// <summary>
            ///     Displays the window.
            /// </summary>
            SWP_SHOWWINDOW = 0x0040,

            // ReSharper restore InconsistentNaming
        }

        /// <summary>
        /// SetWindowPos Flags
        /// </summary>
        public static class SWP
        {
            public static readonly int
            NOSIZE = 0x0001,
            NOMOVE = 0x0002,
            NOZORDER = 0x0004,
            NOREDRAW = 0x0008,
            NOACTIVATE = 0x0010,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            SHOWWINDOW = 0x0040,
            HIDEWINDOW = 0x0080,
            NOCOPYBITS = 0x0100,
            NOOWNERZORDER = 0x0200,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            DEFERERASE = 0x2000,
            ASYNCWINDOWPOS = 0x4000;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        void HideUI()
        {
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

            if (hideControls) FadeTo(mediaControls, 0);
            if (hideOverlay) FadeTo(windowControls, 0);
            IsHidden = true;
        }

        void ShowUI()
        {
            FadeTo(mediaControls, 1);
            FadeTo(windowControls, 1);
            IsHidden = false;
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

            visualizations.MouseEnter -= MediaControlsOnMouseEnter;
            visualizations.MouseLeave -= MediaControlsOnMouseLeave;

            equalizer.MouseEnter -= MediaControlsOnMouseEnter;
            equalizer.MouseLeave -= MediaControlsOnMouseLeave;

            options.MouseEnter -= MediaControlsOnMouseEnter;
            options.MouseLeave -= MediaControlsOnMouseLeave;
        }

        public void RegisterEventsOnControls()
        {
            playlist.MouseEnter += MediaControlsOnMouseEnter;
            playlist.MouseLeave += MediaControlsOnMouseLeave;

            mediaControls.MouseEnter += MediaControlsOnMouseEnter;
            mediaControls.MouseLeave += MediaControlsOnMouseLeave;

            windowControls.MouseEnter += MediaControlsOnMouseEnter;
            windowControls.MouseLeave += MediaControlsOnMouseLeave;

            imdbOverlay.MouseEnter += MediaControlsOnMouseEnter;
            imdbOverlay.MouseLeave += MediaControlsOnMouseLeave;

            onlineSubs.MouseEnter += MediaControlsOnMouseEnter;
            onlineSubs.MouseLeave += MediaControlsOnMouseLeave;

            visualizations.MouseEnter += MediaControlsOnMouseEnter;
            visualizations.MouseLeave += MediaControlsOnMouseLeave;

            equalizer.MouseEnter += MediaControlsOnMouseEnter;
            equalizer.MouseLeave += MediaControlsOnMouseLeave;

            options.MouseEnter += MediaControlsOnMouseEnter;
            options.MouseLeave += MediaControlsOnMouseLeave;
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

        private List<FrameworkElement> _elementsToRefresh = new List<FrameworkElement>();
        private void FindElementsToAutoRefresh()
        {
            var el = VisualHelper.FindChildren<ToggleButton>(this);
            _elementsToRefresh.Clear();
            foreach (var element in el)
            {
                _elementsToRefresh.Add(element);
            }
        }

        private void MediaControlsOnMouseLeave(object sender, MouseEventArgs e)
        {
            _timeToDelayReShowing = DateTime.Now + TimeSpan.FromMilliseconds(800);
            _isOverUIControl = false;
            if (DataContext == null) return;
            var dc = DataContext as Main;
            if (dc.Player.HasVideo) HideUI();
        }

        private void MediaControlsOnMouseEnter(object sender, MouseEventArgs mouseEventArgs)
        {
            _isOverUIControl = true;
            if (Cursor == Cursors.None) Cursor = Cursors.Arrow;
            ShowUI();
        }

        void timer_Tick(object sender, System.EventArgs e)
        {
            var diff = DateTime.Now - LastMouseMove;

            if (DataContext == null) return;

            var dc = DataContext as Main;

            if (diff >= TimeoutToHide && (!IsHidden || Cursor != Cursors.None))
            {
                if (!_isOverUIControl && dc.Player.HasVideo)
                {
                    Cursor = Cursors.None;
                    HideUI();
                }
            }
        }

        bool _isOverUIControl;

        public DateTime LastMouseMove { get; set; }
        public bool IsHidden { get; set; }
        private DateTime _timeToDelayReShowing = DateTime.Now;
        private TimeSpan TimeoutToHide
        {
            get { return TimeSpan.FromSeconds(3); }
        }
        Point lastpoint;

        private void FadeTo(UIElement element, double value)
        {
            if (value > 0)
            {
                element.Visibility = System.Windows.Visibility.Visible;
            }

            DoubleAnimation da = new DoubleAnimation();
            da.From = element.Opacity;
            da.To = value;
            da.Duration = new Duration(TimeSpan.FromSeconds(1));
            da.AutoReverse = false;

            if (value == 0)
            {
                // attach autodetaching eventhandler
                System.EventHandler ev = null;
                ev = (object o, EventArgs e) =>
                {
                    Clock clock = (Clock)o;
                    if (clock.CurrentState != ClockState.Active)
                    {
                        var val = value;
                        if (val <= 0)
                            element.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    da.CurrentStateInvalidated -= ev;
                };
                da.CurrentStateInvalidated += ev;
            }

            element.BeginAnimation(OpacityProperty, da, HandoffBehavior.SnapshotAndReplace);
        }

        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(this);

            var delta = Math.Sqrt(2) * (Math.Abs(lastpoint.X - p.X) + Math.Abs(lastpoint.Y - p.Y));
            lastpoint = p;
            
            if (DateTime.Now < _timeToDelayReShowing)
            {
                return;
            }

            if (delta < 6 || DateTime.Now < LastMouseMove) return;
            CommandManager.InvalidateRequerySuggested();
            LastMouseMove = DateTime.Now;
            if (IsHidden)
            {
                Cursor = Cursors.Arrow;
                ShowUI();
            }

        }

        private void mediaPlayer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (WindowState != WindowState.Maximized) return;
            var delta = (double)e.Delta / 1200;
            if (((Keyboard.GetKeyStates(Key.LeftCtrl) == KeyStates.Down ||
                Keyboard.GetKeyStates(Key.RightCtrl) == KeyStates.Down)
                && (Keyboard.GetKeyStates(Key.LeftShift) != KeyStates.Down && Keyboard.GetKeyStates(Key.RightShift) != KeyStates.Down)) ||
                (e.LeftButton != MouseButtonState.Pressed && e.RightButton == MouseButtonState.Pressed && Keyboard.GetKeyStates(Key.LeftShift) != KeyStates.Down && Keyboard.GetKeyStates(Key.RightShift) != KeyStates.Down))
            {
                _rotation = rotation.Angle + delta * 30;
                AnimationExtensions.AnimatePropertyTo(rotation, s => s.Angle, rotation.Angle + delta * 30, 0.3);
            }
            else if ((Keyboard.GetKeyStates(Key.LeftShift) == KeyStates.Down ||
                            Keyboard.GetKeyStates(Key.RightShift) == KeyStates.Down) &&
                            (e.RightButton == MouseButtonState.Pressed))
            {
                _skewX = skew.AngleX + delta * 30;
                AnimationExtensions.AnimatePropertyTo(skew, s => s.AngleX, skew.AngleX + delta * 30, 0.3);
            }
            else if ((Keyboard.GetKeyStates(Key.LeftShift) == KeyStates.Down ||
                            Keyboard.GetKeyStates(Key.RightShift) == KeyStates.Down) &&
                            (e.RightButton != MouseButtonState.Pressed))
            {
                _skewY = skew.AngleY + delta * 30;
                AnimationExtensions.AnimatePropertyTo(skew, s => s.AngleY, skew.AngleY + delta * 30, 0.3);
            }
            else
            {
                if (scale.ScaleX + delta > 0.2 && scale.ScaleX + delta < 3)
                {
                    _scaleX = scale.ScaleX + delta;
                    _scaleY = scale.ScaleY + delta;
                    AnimationExtensions.AnimatePropertyTo(scale, s => s.ScaleX, scale.ScaleX + delta, 0.3);
                    AnimationExtensions.AnimatePropertyTo(scale, s => s.ScaleY, scale.ScaleY + delta, 0.3);
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            trayIcon.Dispose();
            base.OnClosing(e);
            Application.Current.Shutdown();
        }

        private void mediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            OnPropertyChanged("IsPlaying");
        }

        private void mediaPlayer_MediaClosed(object sender, RoutedEventArgs e)
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

        private void mediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            OnPropertyChanged("IsPlaying");
            if (DataContext is Main)
            {
                ((Main)DataContext).Player.OnMediaEnded();
            }
        }

        private void mediaPlayer_MediaFailed(object sender, Common.DirectShow.MediaPlayers.MediaFailedEventArgs e)
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
            MessageBox.Show(String.Format("Media failed: {0}\r\nStacktrace:\r\n {1}", e.Exception.Message, e.Exception.StackTrace));
        }

        private bool _animating;
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var mc = FindName("mediaControls") as MediaControls;
            if (!(mc.RenderTransform is MatrixTransform) && !(mc.RenderTransform is TranslateTransform)) return;
            if (_animating) return;
            _animating = true;
            Transform oTransform = mc.RenderTransform;
            double X = 0;
            double Y = 0;
            if (oTransform is MatrixTransform)
            {
                X = ((MatrixTransform)oTransform).Matrix.OffsetX;
                Y = ((MatrixTransform)oTransform).Matrix.OffsetY;
            }
            else
            {
                X = ((TranslateTransform)oTransform).X;
                Y = ((TranslateTransform)oTransform).Y;
            }
            DoubleAnimation dbXZero = new DoubleAnimation(X, 0, new Duration(TimeSpan.FromMilliseconds(300)));
            DoubleAnimation dbYZero = new DoubleAnimation(Y, 0, new Duration(TimeSpan.FromMilliseconds(300)));
            mc.RenderTransform = oTransform = new TranslateTransform(X, Y);
            Storyboard storyboard = new Storyboard();
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

        private bool OneClick { get; set; }
        private bool TwoClick { get; set; }

        private void mediaPlayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (OneClick)
            {
                // a slow single click is pause/play, but a fast double click is maximize
                TwoClick = true;
                return;
            }

            OneClick = true;
            TwoClick = false;

            Thread t = new Thread(() =>
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
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (mediaPlayer.IsPlaying)
                        mediaPlayer.Pause();
                    else
                    {
                        mediaPlayer.Play();
                    }
                }));
            });
            t.IsBackground = true;
            t.Start();
        }

        private void mediaPlayer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext == null) return;
            var dc = DataContext as Main;
            if (dc.Player.HasVideo) HideUI();
        }

        private void mediaPlayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            ServiceLocator.RegisterOverrideService(this as ISpectrumVisualizer);

            if (!_once)
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    _once = true;
                    var ih = new WindowInteropHelper(this);
                    IntPtr hwnd = ih.Handle;
                    TaskbarManager.Instance.ThumbnailToolBars.AddButtons(hwnd, _buttons);
                    _lastMinSize = new Size(MinWidth, MinHeight);
                    Visibility = Visibility.Visible;
                    _onceDone = true;
                }), DispatcherPriority.ApplicationIdle);
        }

        private void window_Initialized(object sender, EventArgs e)
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
                    dc.Player.PreviousCommand.Execute(null);
                    break;
                case "next":
                    dc.Player.NextCommand.Execute(null);
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
            for (i = 1; i < data.Length; i++ )
            {
                ac0 = value * data[i];
                data[i] = data[i] - f;
                f = ac0;
            }
        }

        void DoLogarithmic(float[] data, float minY, float maxY)
        {
            float range = maxY - minY;

            for (int i = 1; i < data.Length; i++)
            {
                data[i] = (float)Math.Sqrt(data[i] * range);
                data[i] = data[i] + minY;
            }

        }

        public bool GetFFTData(float[] fftDataBuffer)
        {
            _sampleAggregator.GetFFTResults(fftDataBuffer);
            DoPreEmphesis(fftDataBuffer, 90f / 100);
            //DoLogarithmic(fftDataBuffer, 0.0f, 1f);
            return IsPlaying;
        }

        SampleAggregator _sampleAggregator = new SampleAggregator((int)FFTDataSize.FFT2048);

        public int GetFFTFrequencyIndex(int frequency)
        {
            double maxFrequency;
            if (_frequency > 0)
                maxFrequency = _frequency / 2.0d;
            else
                maxFrequency = 22050; // Assume a default 44.1 kHz sample rate./2
            return (int)((frequency / maxFrequency) * ((int)FFTDataSize.FFT2048/2)); // only real
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
            foreach(var s in sa) s.RegisterSoundPlayer(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
