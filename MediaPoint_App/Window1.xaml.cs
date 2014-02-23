using System;
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

namespace MediaPoint.App
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : IMainView, ISpectrumPlayer, ISpectrumVisualizer
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

        public string StartupFile
        {
            get { return _startFile; }
            set {
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

        public Window1()
        {
            InitializeComponent();
            Visibility = Visibility.Collapsed;
            var timer = new DispatcherTimer(DispatcherPriority.Background);
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 3);
            timer.Start();

            PreviewGotKeyboardFocus += Window1_PreviewGotKeyboardFocus;

            Layout.PreviewDragEnter += Window1_PreviewDragEnter;
            Layout.PreviewDragLeave += Window1_PreviewDragLeave;
            Layout.PreviewDragOver += Layout_PreviewDragOver;
            Layout.PreviewDrop += Window1_PreviewDrop;

            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            
            foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
            {
                Console.WriteLine("*** {0}, {1}, {2}", device.FriendlyName, device.DeviceFriendlyName, device.State);
                if (device.State == DeviceState.Active) Console.WriteLine("   {0}", device.AudioEndpointVolume.Channels.Count);
            }
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
                if (data != null)
                    foreach (var file in data)
                    {
                        string extension = Path.GetExtension(file);
                        if (extension != null)
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
            Debug.WriteLine("Focus " + e.NewFocus.GetType().Name + ": " + ((e.NewFocus is FrameworkElement) ? (e.NewFocus as FrameworkElement).Name : ""));
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
                    ShowInTaskbar = true;
                    Visibility = Visibility.Visible;
                    MaxWidth = Int32.MaxValue;
                    MaxHeight = Int32.MaxValue;
                    WindowState = WindowState.Maximized;
                    SetForegroundWindow(winHelp.Handle);
                    break;
                case MainViewCommand.Restore:
                    didSomething = true;
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    MinWidth = _lastMinSize.Width;
                    MinHeight = _lastMinSize.Height;
                    SetForegroundWindow(winHelp.Handle);
                    ShowWindow(winHelp.Handle, (uint)WindowShowStyle.Restore);
                    WindowState = WindowState.Normal;
                    ShowInTaskbar = true;
                    Show();

                    break;
            }

            return didSomething;
        }

        /// <summary>Enumeration of the different ways of showing a window using
        /// ShowWindow</summary>
        private enum WindowShowStyle : uint
        {
            /// <summary>Hides the window and activates another window.</summary>
            /// <remarks>See SW_HIDE</remarks>
            Hide = 0,
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

        void HideUi()
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
            IsUiVisible = false;
        }

        void ShowUi()
        {
            FadeTo(mediaControls, 1);
            FadeTo(windowControls, 1);
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

            mediaControls.MouseEnter += MediaControlsOnMouseEnter;
            mediaControls.MouseLeave += MediaControlsOnMouseLeave;

            windowControls.MouseEnter += MediaControlsOnMouseEnter;
            windowControls.MouseLeave += MediaControlsOnMouseLeave;

            imdbOverlay.MouseEnter += MediaControlsOnMouseEnter;
            imdbOverlay.MouseLeave += MediaControlsOnMouseLeave;

            onlineSubs.MouseEnter += MediaControlsOnMouseEnter;
            onlineSubs.MouseLeave += MediaControlsOnMouseLeave;

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
            _isOverUiControl = false;
            if (DataContext == null) return;
            var dc = DataContext as Main;
            if (dc != null && dc.Player.HasVideo) HideUi();
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
                if (dc != null && (!_isOverUiControl && dc.Player.HasVideo))
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
            if (WindowState != WindowState.Maximized) return;
            var delta = (double)e.Delta / 1200;
            if (((Keyboard.GetKeyStates(Key.LeftCtrl) == KeyStates.Down ||
                Keyboard.GetKeyStates(Key.RightCtrl) == KeyStates.Down)
                && (Keyboard.GetKeyStates(Key.LeftShift) != KeyStates.Down && Keyboard.GetKeyStates(Key.RightShift) != KeyStates.Down)) ||
                (e.LeftButton != MouseButtonState.Pressed && e.RightButton == MouseButtonState.Pressed && Keyboard.GetKeyStates(Key.LeftShift) != KeyStates.Down && Keyboard.GetKeyStates(Key.RightShift) != KeyStates.Down))
            {
                _rotation = rotation.Angle + delta * 30;
                rotation.AnimatePropertyTo(s => s.Angle, rotation.Angle + delta * 30, 0.3);
            }
            else if ((Keyboard.GetKeyStates(Key.LeftShift) == KeyStates.Down ||
                            Keyboard.GetKeyStates(Key.RightShift) == KeyStates.Down) &&
                            (e.RightButton == MouseButtonState.Pressed))
            {
                _skewX = skew.AngleX + delta * 30;
                skew.AnimatePropertyTo(s => s.AngleX, skew.AngleX + delta * 30, 0.3);
            }
            else if ((Keyboard.GetKeyStates(Key.LeftShift) == KeyStates.Down ||
                            Keyboard.GetKeyStates(Key.RightShift) == KeyStates.Down) &&
                            (e.RightButton != MouseButtonState.Pressed))
            {
                _skewY = skew.AngleY + delta * 30;
                skew.AnimatePropertyTo(s => s.AngleY, skew.AngleY + delta * 30, 0.3);
            }
            else
            {
                if (scale.ScaleX + delta > 0.2 && scale.ScaleX + delta < 3)
                {
                    _scaleX = scale.ScaleX + delta;
                    _scaleY = scale.ScaleY + delta;
                    scale.AnimatePropertyTo(s => s.ScaleX, scale.ScaleX + delta, 0.3);
                    scale.AnimatePropertyTo(s => s.ScaleY, scale.ScaleY + delta, 0.3);
                }
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

        private void MediaPlayer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext == null) return;
            var dc = DataContext as Main;
            if (dc != null && dc.Player.HasVideo) HideUi();
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
        private void Window_Loaded(object sender, RoutedEventArgs e)
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

        private void Window_Initialized(object sender, EventArgs e)
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
            for (i = 1; i < data.Length; i++ )
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
