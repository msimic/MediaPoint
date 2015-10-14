using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MediaPoint.Common.DirectShow.MediaPlayers;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.ObjectModel;
using MediaPoint.Common.MediaFoundation.Interop;
using MediaPoint.Subtitles;
using MediaPoint.VM;
using MediaPoint.VM.ViewInterfaces;
using MediaPoint.Controls.Extensions;
using MediaPoint.MVVM.Services;
using Application = System.Windows.Application;
using System.Windows.Media.Imaging;
using MediaPoint.Common.Services;

namespace MediaPoint.Controls
{
	/// <summary>
	/// The MediaElementBase is the base WPF control for
	/// making custom media players.  The MediaElement uses the
	/// D3DRenderer class for rendering video
	/// </summary>
	public abstract class MediaElementBase : D3DRenderer, INotifyPropertyChanged, IPlayerView, IEqualizer
	{
		private Window _currentWindow;
		private bool _windowHooked;
	    private DispatcherTimer _adapterTimer;
	    private Point _lastScreenPoint;
	    private IntPtr _hwnd = IntPtr.Zero;
	    private System.Windows.Forms.Screen _lastScreen = null;

		#region Routed Events
		#region MediaOpened

		public static readonly RoutedEvent MediaOpenedEvent = EventManager.RegisterRoutedEvent("MediaOpened",
																							   RoutingStrategy.Bubble,
																							   typeof(RoutedEventHandler
																								   ),
																							   typeof(MediaElementBase));

		/// <summary>
		/// Fires when media has successfully been opened
		/// </summary>
		public event RoutedEventHandler MediaOpened
		{
			add { AddHandler(MediaOpenedEvent, value); }
			remove { RemoveHandler(MediaOpenedEvent, value); }
		}

		#endregion

		#region MediaClosed

		public static readonly RoutedEvent MediaClosedEvent = EventManager.RegisterRoutedEvent("MediaClosed",
																							   RoutingStrategy.Bubble,
																							   typeof(RoutedEventHandler),
																							   typeof(MediaElementBase));

		/// <summary>
		/// Fires when media has been closed
		/// </summary>
		public event RoutedEventHandler MediaClosed
		{
			add { AddHandler(MediaClosedEvent, value); }
			remove { RemoveHandler(MediaClosedEvent, value); }
		}

		#endregion

		#region MediaEnded

		public static readonly RoutedEvent MediaEndedEvent = EventManager.RegisterRoutedEvent("MediaEnded",
																							  RoutingStrategy.Bubble,
																							  typeof(RoutedEventHandler),
																							  typeof(MediaElementBase));

		/// <summary>
		/// Fires when media has completed playing
		/// </summary>
		public event RoutedEventHandler MediaEnded
		{
			add { AddHandler(MediaEndedEvent, value); }
			remove { RemoveHandler(MediaEndedEvent, value); }
		}

		#endregion
		#endregion

		#region Dependency Properties
		#region UnloadedBehavior

		public static readonly DependencyProperty UnloadedBehaviorProperty =
			DependencyProperty.Register("UnloadedBehavior", typeof(MediaState), typeof(MediaElementBase),
										new FrameworkPropertyMetadata(MediaState.Close));

		/// <summary>
		/// Defines the behavior of the control when it is unloaded
		/// </summary>
		public MediaState UnloadedBehavior
		{
			get { return (MediaState)GetValue(UnloadedBehaviorProperty); }
			set { SetValue(UnloadedBehaviorProperty, value); }
		}

		#endregion

		#region LoadedBehavior

		public static readonly DependencyProperty LoadedBehaviorProperty =
			DependencyProperty.Register("LoadedBehavior", typeof(MediaState), typeof(MediaElementBase),
										new FrameworkPropertyMetadata(MediaState.Play));

		/// <summary>
		/// Defines the behavior of the control when it is loaded
		/// </summary>
		public MediaState LoadedBehavior
		{
			get { return (MediaState)GetValue(LoadedBehaviorProperty); }
			set { SetValue(LoadedBehaviorProperty, value); }
		}

		#endregion

		public static readonly DependencyProperty AutoSizeProperty =
			DependencyProperty.Register("AutoSize", typeof(bool), typeof(MediaElementBase),
				new FrameworkPropertyMetadata(true));

		public bool AutoSize
		{
			get { return (bool)GetValue(AutoSizeProperty); }
			set { SetValue(AutoSizeProperty, value); }
		}

		#region Volume

		public static readonly DependencyProperty VolumeProperty =
			DependencyProperty.Register("Volume", typeof(double), typeof(MediaElementBase),
				new FrameworkPropertyMetadata(1.0d,
					new PropertyChangedCallback(OnVolumeChanged)));

		/// <summary>
		/// Gets or sets the audio volume.  Specifies the volume, as a 
		/// number from 0 to 1.  Full volume is 1, and 0 is silence.
		/// </summary>
		public double Volume
		{
			get { return (double)GetValue(VolumeProperty); }
			set { SetValue(VolumeProperty, value); }
		}

		private static void OnVolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((MediaElementBase)d).OnVolumeChanged(e);
		}

		protected virtual void OnVolumeChanged(DependencyPropertyChangedEventArgs e)
		{
			if (HasInitialized)
				MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
				{
					MediaPlayerBase.Volume = (double)e.NewValue;
				});
		}

		#endregion

		#region Balance

		public static readonly DependencyProperty BalanceProperty =
			DependencyProperty.Register("Balance", typeof(double), typeof(MediaElementBase),
				new FrameworkPropertyMetadata(0d,
					new PropertyChangedCallback(OnBalanceChanged)));

		/// <summary>
		/// Gets or sets the balance on the audio.
		/// The value can range from -1 to 1. The value -1 means the right channel is attenuated by 100 dB 
		/// and is effectively silent. The value 1 means the left channel is silent. The neutral value is 0, 
		/// which means that both channels are at full volume. When one channel is attenuated, the other 
		/// remains at full volume.
		/// </summary>
		public double Balance
		{
			get { return (double)GetValue(BalanceProperty); }
			set { SetValue(BalanceProperty, value); }
		}

		private static void OnBalanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((MediaElementBase)d).OnBalanceChanged(e);
		}

		protected virtual void OnBalanceChanged(DependencyPropertyChangedEventArgs e)
		{
			if (HasInitialized)
				MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
				{
					MediaPlayerBase.Balance = (double)e.NewValue;
				});
		}

		#endregion


		#region VideoRenderer

		#region Source

		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.Register("Source", typeof(Uri), typeof(MediaElementBase),
				new FrameworkPropertyMetadata(null,
					new PropertyChangedCallback(OnSourceChanged)));

		/// <summary>
		/// The Uri source to the media.  This can be a file path or a
		/// URL source
		/// </summary>
		public Uri Source
		{
			get { return (Uri)GetValue(SourceProperty); }
			set { SetValue(SourceProperty, value); }
		}

		private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
            ((MediaUriElement) d).InvalidateVideoImage();
			((MediaUriElement)d).OnSourceChanged(e);
		}

		protected void OnSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			if (IsInDesignMode) return;

			if (HasInitialized)
				PlayerSetSource();
		}

		protected void PlayerSetSource()
		{
			bool designTime = DesignerProperties.GetIsInDesignMode(new DependencyObject());
			if (designTime) return;

			Uri src = Source;

            var mp = MediaPlayerBase;
            if (GetBindingExpression(SubtitleFontProperty) != null) GetBindingExpression(SubtitleFontProperty).UpdateTarget();
            if (GetBindingExpression(SubtitleCharsetProperty) != null) GetBindingExpression(SubtitleCharsetProperty).UpdateTarget();
            if (GetBindingExpression(SubtitleSizeProperty) != null) GetBindingExpression(SubtitleSizeProperty).UpdateTarget();
            if (GetBindingExpression(SubtitleColorProperty) != null) GetBindingExpression(SubtitleColorProperty).UpdateTarget();
            if (GetBindingExpression(SubtitleProperty) != null) GetBindingExpression(SubtitleProperty).UpdateTarget();
            if (GetBindingExpression(SubtitleDelayProperty) != null) GetBindingExpression(SubtitleDelayProperty).UpdateTarget();
            
            string sf = null;
            FontCharSet sc = FontCharSet.Default;
            int ss = 0;
		    Color sco = Colors.White;
		    SubtitleItem sub = null;
            bool bold;

            if (SubtitleFont != null)
            {
                sf = SubtitleFont.Source;
            }
            if (Subtitle != null)
            {
                sub = Subtitle;
            }
            else
            {
                sub = null;
            }
            sc = SubtitleCharset;
            ss = SubtitleSize;
		    sco = SubtitleColor;
            bold = SubtitleBold;
            int delay = SubtitleDelay;

			MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
			{
                if (sf != null) mp.SubtitleSettings.FontFamily = sf;
                if (sc != FontCharSet.Default) mp.SubtitleSettings.Charset = sc;
                if (ss != 0) mp.SubtitleSettings.Size = ss;
                mp.SubtitleSettings.Subtitle = sub;
                mp.SubtitleSettings.Color = sco;
                (MediaPlayerBase as MediaUriPlayer).Source = src;
                mp.SubtitleSettings.Delay = delay;
                mp.SubtitleSettings.Bold = bold;
                Dispatcher.BeginInvoke((Action)delegate
				{
					if (IsLoaded)
						ExecuteMediaState(LoadedBehavior);
					//else
					//    ExecuteMediaState(UnloadedBehavior);
					InvalidateVisual();
				});
			});
		}
		#endregion

		public bool ExecuteCommand(PlayerCommand command, object parameter = null)
		{
			bool didSomething = false;

			this.Dispatcher.BeginInvoke((Action)delegate()
			{
				switch (command)
				{
					case PlayerCommand.Open:
						didSomething = true;
						this.Source = null;
						this.Source = (Uri)parameter;
						this.Play();
						//HideUI();
						break;
					case PlayerCommand.Play:
						didSomething = true;
						this.Play();
						//HideUI();
						break;
					case PlayerCommand.Pause:
						didSomething = true;
						this.Pause();
						break;
					case PlayerCommand.Stop:
						didSomething = true;
						this.Stop();
						break;
					case PlayerCommand.Dispose:
						this.Close();
						break;
				}
			});
			return didSomething;
		}

        public RenderTargetBitmap GetBitmapOfVideoElement()
        {
            DrawingVisual visual = new DrawingVisual();
            DrawingContext context = visual.RenderOpen();
            int w = D3DImage.PixelWidth;
            int h = D3DImage.PixelHeight;

            if (w == 0 || h == 0) return null;

            context.DrawImage(D3DImage, new Rect(0, 0, w, h));
            context.Close();

            RenderTargetBitmap bitmap = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Default);
            bitmap.Render(visual);

            return bitmap;
        }

		public static readonly DependencyProperty VideoRendererProperty =
			DependencyProperty.Register("VideoRenderer", typeof(VideoRendererType), typeof(MediaElementBase),
				new FrameworkPropertyMetadata(VideoRendererType.VideoMixingRenderer9,
					new PropertyChangedCallback(OnVideoRendererChanged)));

		public VideoRendererType VideoRenderer
		{
			get { return (VideoRendererType)GetValue(VideoRendererProperty); }
			set { SetValue(VideoRendererProperty, value); }
		}

		private static void OnVideoRendererChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((MediaUriElement)d).OnVideoRendererChanged(e);
		}

		protected virtual void OnVideoRendererChanged(DependencyPropertyChangedEventArgs e)
		{
			if (HasInitialized)
				PlayerSetVideoRenderer();
		}

		protected void PlayerSetVideoRenderer()
		{
			bool designTime = DesignerProperties.GetIsInDesignMode(new DependencyObject());
			if (designTime) return;
			
			var videoRendererType = VideoRenderer;
			MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
			{
				(MediaPlayerBase as MediaUriPlayer).VideoRenderer = videoRendererType;
			});
		}

		#endregion

		#region AudioRenderer

		public ObservableCollection<string> AudioRenderers
		{
			get { return (ObservableCollection<string>)GetValue(AudioRenderersProperty); }
			set { SetValue(AudioRenderersProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AudioRenderers.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AudioRenderersProperty =
			DependencyProperty.Register("AudioRenderers", typeof(ObservableCollection<string>), typeof(MediaElementBase), new UIPropertyMetadata(null));


		public static readonly DependencyProperty AudioRendererProperty =
			DependencyProperty.Register("AudioRenderer", typeof(string), typeof(MediaElementBase),
                new FrameworkPropertyMetadata(SharpDX.DirectSound.DirectSound.GetDevices().Count == 0 ? "" : SharpDX.DirectSound.DirectSound.GetDevices()[0].Description,
					new PropertyChangedCallback(OnAudioRendererChanged)));

		/// <summary>
		/// The name of the audio renderer device to use
		/// </summary>
		public string AudioRenderer
		{
			get { return (string)GetValue(AudioRendererProperty); }
			set { SetValue(AudioRendererProperty, value); }
		}

		private static void OnAudioRendererChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((MediaUriElement)d).OnAudioRendererChanged(e);
		}

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register("Subtitle", typeof(SubtitleItem), typeof(MediaElementBase),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnSubtitleChanged)));

        public SubtitleItem Subtitle
        {
            get { return (SubtitleItem)GetValue(SubtitleProperty); }
            set { SetValue(SubtitleProperty, value); }
        }

        private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MediaUriElement)d).OnSubtitleChanged(e);
        }

        protected virtual void OnSubtitleChanged(DependencyPropertyChangedEventArgs e)
        {
            if (HasInitialized && MediaPlayerBase.SubtitleSettings.Subtitle != e.NewValue)
            {
                MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
                {
                    MediaPlayerBase.SubtitleSettings.Subtitle = e.NewValue as SubtitleItem;
                });
            }
        }

        public static readonly DependencyProperty SubtitleFontProperty =
            DependencyProperty.Register("SubtitleFont", typeof(FontFamily), typeof(MediaElementBase),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnSubtitleFontChanged)));

        public FontFamily SubtitleFont
        {
            get { return (FontFamily)GetValue(SubtitleFontProperty); }
            set { SetValue(SubtitleFontProperty, value); }
        }

        private static void OnSubtitleFontChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MediaUriElement)d).OnSubtitleFontChanged(e);
        }

        protected virtual void OnSubtitleFontChanged(DependencyPropertyChangedEventArgs e)
        {
            if (HasInitialized && e.NewValue != null && MediaPlayerBase.SubtitleSettings.FontFamily != ((FontFamily)e.NewValue).Source)
            {
                MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
                {
                    MediaPlayerBase.SubtitleSettings.FontFamily = ((FontFamily)e.NewValue).Source;
                });
            }
        }

        public static readonly DependencyProperty SubtitleSizeProperty =
            DependencyProperty.Register("SubtitleSize", typeof(int), typeof(MediaElementBase),
                new FrameworkPropertyMetadata(0,
                    new PropertyChangedCallback(OnSubtitleSizeChanged)));

        public int SubtitleSize
        {
            get { return (int)GetValue(SubtitleSizeProperty); }
            set { SetValue(SubtitleSizeProperty, value); }
        }

        private static void OnSubtitleSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MediaUriElement)d).OnSubtitleSizeChanged(e);
        }

        protected virtual void OnSubtitleSizeChanged(DependencyPropertyChangedEventArgs e)
        {
            if (HasInitialized && e.NewValue != null && MediaPlayerBase.SubtitleSettings.Size != ((int)e.NewValue))
            {
                MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
                {
                    MediaPlayerBase.SubtitleSettings.Size = ((int)e.NewValue);
                });
            }
        }

        public static readonly DependencyProperty SubtitleBoldProperty =
            DependencyProperty.Register("SubtitleBold", typeof(bool), typeof(MediaElementBase),
                new FrameworkPropertyMetadata(false,
                    new PropertyChangedCallback(OnSubtitleBoldChanged)));

        public bool SubtitleBold
        {
            get { return (bool)GetValue(SubtitleBoldProperty); }
            set { SetValue(SubtitleBoldProperty, value); }
        }

        private static void OnSubtitleBoldChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MediaUriElement)d).OnSubtitleBoldChanged(e);
        }

        protected virtual void OnSubtitleBoldChanged(DependencyPropertyChangedEventArgs e)
        {
            if (HasInitialized && e.NewValue != null && MediaPlayerBase.SubtitleSettings.Bold != ((bool)e.NewValue))
            {
                MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
                {
                    MediaPlayerBase.SubtitleSettings.Bold = ((bool)e.NewValue);
                });
            }
        }

        public static readonly DependencyProperty SubtitleDelayProperty =
            DependencyProperty.Register("SubtitleDelay", typeof(int), typeof(MediaElementBase),
                new FrameworkPropertyMetadata(0,
                    new PropertyChangedCallback(OnSubtitleDelayChanged)));

        public int SubtitleDelay
        {
            get { return (int)GetValue(SubtitleDelayProperty); }
            set { SetValue(SubtitleDelayProperty, value); }
        }

        private static void OnSubtitleDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MediaUriElement)d).OnSubtitleDelayChanged(e);
        }

        protected virtual void OnSubtitleDelayChanged(DependencyPropertyChangedEventArgs e)
        {
            if (HasInitialized && e.NewValue != null && MediaPlayerBase.SubtitleSettings.Delay != ((int)e.NewValue))
            {
                MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
                {
                    MediaPlayerBase.SubtitleSettings.Delay = ((int)e.NewValue);
                });
            }
        }

        public static readonly DependencyProperty SubtitleCharsetProperty =
            DependencyProperty.Register("SubtitleCharset", typeof(FontCharSet), typeof(MediaElementBase),
                new FrameworkPropertyMetadata(FontCharSet.Default,
                    new PropertyChangedCallback(OnSubtitleCharsetChanged)));

        public FontCharSet SubtitleCharset
        {
            get { return (FontCharSet)GetValue(SubtitleCharsetProperty); }
            set { SetValue(SubtitleCharsetProperty, value); }
        }

        private static void OnSubtitleCharsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MediaUriElement)d).OnSubtitleCharsetChanged(e);
        }

        protected virtual void OnSubtitleCharsetChanged(DependencyPropertyChangedEventArgs e)
        {
            if (HasInitialized && e.NewValue != null && MediaPlayerBase.SubtitleSettings.Charset != ((FontCharSet)e.NewValue))
            {
                MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
                {
                    MediaPlayerBase.SubtitleSettings.Charset = ((FontCharSet)e.NewValue);
                });
            }
        }

        public static readonly DependencyProperty SubtitleColorProperty =
            DependencyProperty.Register("SubtitleColor", typeof(Color), typeof(MediaElementBase),
                new FrameworkPropertyMetadata(Colors.White,
                    new PropertyChangedCallback(OnSubtitleColorChanged)));

        public Color SubtitleColor
        {
            get { return (Color)GetValue(SubtitleColorProperty); }
            set { SetValue(SubtitleColorProperty, value); }
        }

        private static void OnSubtitleColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MediaUriElement)d).OnSubtitleColorChanged(e);
        }

        protected virtual void OnSubtitleColorChanged(DependencyPropertyChangedEventArgs e)
        {
            if (HasInitialized && e.NewValue != null && MediaPlayerBase.SubtitleSettings.Color != ((Color)e.NewValue))
            {
                MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
                {
                    MediaPlayerBase.SubtitleSettings.Color = ((Color)e.NewValue);
                });
            }
        }

		protected virtual void OnAudioRendererChanged(DependencyPropertyChangedEventArgs e)
		{
			if (HasInitialized)
				PlayerSetAudioRenderer();
		}

		protected void PlayerSetAudioRenderer()
		{
			bool designTime = DesignerProperties.GetIsInDesignMode(new DependencyObject());
			if (designTime) return;

			var audioDevice = AudioRenderer;

			MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
			{
				/* Sets the audio device to use with the player */
				(MediaPlayerBase as MediaUriPlayer).AudioRenderer = audioDevice;
			});
		}

		#endregion

		#region IsPlaying

		//private static readonly DependencyPropertyKey IsPlayingPropertyKey
		//    = DependencyProperty.RegisterReadOnly("IsPlaying", typeof(bool), typeof(MediaElementBase),
		//        new FrameworkPropertyMetadata(false));

		public static readonly DependencyProperty IsPlayingProperty
			= DependencyProperty.Register("IsPlaying", typeof(bool), typeof(MediaElementBase), new UIPropertyMetadata(false));

		public bool IsPlaying
		{
			get { return (bool)GetValue(IsPlayingProperty); }
			set
			{
				SetValue(IsPlayingProperty, value);
			}
		}

		protected void SetIsPlaying(bool value)
		{
			AudioRenderers = new ObservableCollection<string>(MediaPlayerBase.AudioRenderers);
				
			if (value)
			{
				this.Dispatcher.BeginInvoke((Action)delegate
				{
					SubtitlesStreams = new ObservableCollection<string>(MediaPlayerBase.SubtitleStreams);
					VideoStreams = new ObservableCollection<string>(MediaPlayerBase.VideoStreams);
					AudioStreams = new ObservableCollection<string>(MediaPlayerBase.AudioStreams);
				});
			}
			else
			{
				this.Dispatcher.BeginInvoke((Action)delegate
				{
					SubtitlesStreams = new ObservableCollection<string>();
					VideoStreams = new ObservableCollection<string>();
					AudioStreams = new ObservableCollection<string>();
				});
			}

			SetValue(IsPlayingProperty, value);
		}
		#endregion

		#endregion

		#region Commands
		public static readonly RoutedCommand PlayerStateCommand = new RoutedCommand();
		public static readonly RoutedCommand TogglePlayPauseCommand = new RoutedCommand();
		public static readonly RoutedCommand StopCommand = new RoutedCommand();

		protected virtual void OnPlayerStateCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Parameter is MediaState == false)
				return;

			var state = (MediaState)e.Parameter;

			ExecuteMediaState(state);
		}

		protected virtual void OnCanExecutePlayerStateCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		protected virtual void OnTogglePlayPauseCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (IsPlaying)
				Pause();
			else
				Play();
		}

		protected virtual void OnCanExecuteTogglePlayPauseCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		protected virtual void OnStopCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			Stop();
		}

		protected virtual void OnCanExecuteStopCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}
		#endregion

		/// <summary>
		/// Notifies when the media has failed and produced an exception
		/// </summary>
		public event EventHandler<MediaFailedEventArgs> MediaFailed;

		protected MediaElementBase()
		{
			DefaultApartmentState = ApartmentState.MTA;

			bool designTime = System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject());
			if (designTime) return;
			
			InitializeMediaPlayerPrivate();

			Loaded += MediaElementBaseLoaded;
			Unloaded += MediaElementBaseUnloaded;

			DataContextChanged += (sender, args) =>
			                      	{
			                      		if (args.NewValue is Player) (args.NewValue as Player).View = this;
			                      	};

			CommandBindings.Add(new CommandBinding(PlayerStateCommand,
												   OnPlayerStateCommandExecuted,
												   OnCanExecutePlayerStateCommand));

			CommandBindings.Add(new CommandBinding(TogglePlayPauseCommand,
												   OnTogglePlayPauseCommandExecuted,
												   OnCanExecuteTogglePlayPauseCommand));
			CommandBindings.Add(new CommandBinding(StopCommand,
												   OnStopCommandExecuted,
												   OnCanExecuteStopCommand));

            NaturalSizeChanged += OnNaturalSizeChanged;

            _adapterTimer = new DispatcherTimer();
            _adapterTimer.Tick += AdapterTimer_Tick;
            _adapterTimer.Interval = new TimeSpan(0, 0, 0, 0, 1500);
            _adapterTimer.Start();
		}

        void AdapterTimer_Tick(object sender, EventArgs e)
        {
            if (!IsLoaded) return;

            Point p = _currentWindow.PointToScreen(new Point(_currentWindow.ActualWidth / 2, _currentWindow.ActualHeight / 2));

            if (p != _lastScreenPoint && _hwnd != IntPtr.Zero)
            {
                _lastScreenPoint = p;

                var screen = System.Windows.Forms.Screen.FromHandle(_hwnd);

                if (!screen.Equals(_lastScreen))
                {
                    _lastScreen = screen;
                    if (!MediaPlayerBase.Dispatcher.Shutdown && !MediaPlayerBase.Dispatcher.ShuttingDown)
                        MediaPlayerBase.Dispatcher.BeginInvoke((Action) (delegate
                                                                         {
                                                                             MediaPlayerBase.SetAdapter(p, _hwnd);
                                                                         }));

                }
            }
        }

	    private void OnNaturalSizeChanged(object sender, EventArgs eventArgs)
	    {
	        var nvw = NaturalVideoWidth;
	        var nvh = NaturalVideoHeight;
            if (nvh > 0 && nvw > 0 && !MediaPlayerBase.Dispatcher.Shutdown && !MediaPlayerBase.Dispatcher.ShuttingDown)
					MediaPlayerBase.Dispatcher.BeginInvoke((Action) (delegate
					                                                     {
					                                                         MediaPlayerBase.SetNativePixelSizes(new SIZE(nvw, nvh));
                                                                             AutoSizeControl();
                                                                         }));
            
	    }

        bool _isInitialized = false;
	    private void InitializeMediaPlayerPrivate()
		{
            lock (this)
            {
                if (!_isInitialized)
                {
                    _isInitialized = true;
                    InitializeMediaPlayer();
                    PlayerSetAudioRenderer();
                    PlayerSetVideoRenderer();
                }
            }
		}

		public MediaPlayerBase MediaPlayerBase
		{
			get;
			set;
		}

		protected ApartmentState DefaultApartmentState { get; set; }

		protected void EnsurePlayerThread()
		{
			MediaPlayerBase.EnsureThread(DefaultApartmentState);
		}

		/// <summary>
		/// Initializes the media player, hooking into events
		/// and other general setup.
		/// </summary>
		protected virtual void InitializeMediaPlayer()
		{
			if (MediaPlayerBase != null)
			{
				var mpo = MediaPlayerBase;
				if (!MediaPlayerBase.Dispatcher.Shutdown || !MediaPlayerBase.Dispatcher.ShuttingDown)
					MediaPlayerBase.Dispatcher.BeginInvoke((Action) (delegate
					                                                 	{
					                                                 		mpo.Close();
					                                                 		mpo.Dispose();
					                                                 	}));
				MediaPlayerBase = null;
			}

			MediaPlayerBase = OnRequestMediaPlayer();
			OnPropertyChanged("MediaPlayerBase");

		    if (MediaPlayerBase == null)
			{
				throw new Exception("OnRequestMediaPlayer cannot return null");
			}

			EnsurePlayerThread();
            
            /* Hook into the normal .NET events */
			MediaPlayerBase.MediaOpened += OnMediaPlayerOpenedPrivate;
			MediaPlayerBase.MediaClosed += OnMediaPlayerClosedPrivate;
			MediaPlayerBase.MediaFailed += OnMediaPlayerFailedPrivate;
			MediaPlayerBase.MediaEnded += OnMediaPlayerEndedPrivate;
            MediaPlayerBase.NewFFTData += OnMediaPlayerBaseNewFFTData;
            MediaPlayerBase.NewSamplesNumber += OnMediaPlayerBaseNewSamplesNumber;
            MediaPlayerBase.NewAudioStream += OnMediaPlayerBaseNewAudioStream;
            MediaPlayerBase.NoSubtitleLoaded += OnNoSubtitleLoadedPrivate;

			/* These events fire when we get new D3Dsurfaces or frames */
			MediaPlayerBase.NewAllocatorFrame += OnMediaPlayerNewAllocatorFramePrivate;
			MediaPlayerBase.NewAllocatorSurface += OnMediaPlayerNewAllocatorSurfacePrivate;
            MediaPlayerBase.PlateFound += MediaPlayerBase_PlateFound;
			AudioRenderers = new ObservableCollection<string>(MediaPlayerBase.AudioRenderers);
		}

        void MediaPlayerBase_PlateFound(object sender, string text, int left, int top, int right, int bottom, double angle, int confidence, string nattext, int natconf, string natplate)
        {
#if ALPR
            var pp = ServiceLocator.GetService<MediaPoint.Interfaces.IPlateProcessor>();
            if (pp != null)
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    pp.ProcessPlate(text, left, top, right, bottom, angle, confidence, nattext, natconf, natplate);
                }));
            }
#endif
        }

        void OnMediaPlayerBaseNewAudioStream()
        {
            var data = MediaPlayerBase.AudioStreamInfo;

            Dispatcher.BeginInvoke((Action)(() =>
            {
                ServiceLocator.GetService<ISpectrumVisualizer>().SetStreamInfo(data.Channels, data.Bits, data.Frequency);
            }));
        }

        void OnMediaPlayerBaseNewSamplesNumber()
        {
            var data = MediaPlayerBase.NumSamples;

            Dispatcher.BeginInvoke((Action)(() =>
            {
                ServiceLocator.GetService<ISpectrumVisualizer>().SetNumSamples(data);
            }));
        }

        void OnMediaPlayerBaseNewFFTData()
        {
            var data = MediaPlayerBase.FFTData;

            Dispatcher.BeginInvoke((Action)(() =>
            {
                ServiceLocator.GetService<ISpectrumVisualizer>().DisplayFFTData(data);
            }));
        }

        private void OnNoSubtitleLoadedPrivate(object sender, EventArgs e)
        {
            OnNoSubtitleLoaded(e);
        }

	    #region Private Event Handlers
		private void OnMediaPlayerFailedPrivate(object sender, MediaFailedEventArgs e)
		{
			OnMediaPlayerFailed(e);
		}

		private void OnMediaPlayerNewAllocatorSurfacePrivate(object sender, IntPtr pSurface)
		{
			OnMediaPlayerNewAllocatorSurface(pSurface);
		}

		private void OnMediaPlayerNewAllocatorFramePrivate()
		{
			OnMediaPlayerNewAllocatorFrame();
		}

		private void OnMediaPlayerClosedPrivate()
		{
			OnMediaPlayerClosed();
		}

		private void OnMediaPlayerEndedPrivate()
		{
			OnMediaPlayerEnded();
		}

		private void OnMediaPlayerOpenedPrivate()
		{
			OnMediaPlayerOpened();
		}
		#endregion

		/// <summary>
		/// Fires the MediaFailed event
		/// </summary>
		/// <param name="e">The failed media arguments</param>
		protected void InvokeMediaFailed(MediaFailedEventArgs e)
		{
			EventHandler<MediaFailedEventArgs> mediaFailedHandler = MediaFailed;
			if (mediaFailedHandler != null) mediaFailedHandler(this, e);
		}

        protected void OnNoSubtitleLoaded(EventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                var dc = DataContext as Player;
                if (dc != null)
                {
                    if (dc.NeedSubtitlesCommand != null)
                    {
                        dc.NeedSubtitlesCommand.Execute(null);
                    }
                }
            }));
        }

		/// <summary>
		/// Executes when a media operation failed
		/// </summary>
		/// <param name="e">The failed event arguments</param>
		protected virtual void OnMediaPlayerFailed(MediaFailedEventArgs e)
		{
			Dispatcher.BeginInvoke((Action)(() => SetIsPlaying(false)));
			InvokeMediaFailed(e);
		}

		/// <summary>
		/// Is executes when a new D3D surfaces has been allocated
		/// </summary>
		/// <param name="pSurface">The pointer to the D3D surface</param>
		protected virtual void OnMediaPlayerNewAllocatorSurface(IntPtr pSurface)
		{
			SetBackBuffer(pSurface);
		}

		/// <summary>
		/// Called for every frame in media that has video
		/// </summary>
		protected virtual void OnMediaPlayerNewAllocatorFrame()
		{
			InvalidateVideoImage();
		}

		/// <summary>
		/// Called when the media has been closed
		/// </summary>
		protected virtual void OnMediaPlayerClosed()
		{
			Dispatcher.BeginInvoke((Action)(() => SetIsPlaying(false)));
			Dispatcher.BeginInvoke((Action)(() => RaiseEvent(new RoutedEventArgs(MediaClosedEvent))));
            Dispatcher.BeginInvoke((Action)(() => HasVideo = false));           
		}

		/// <summary>
		/// Called when the media has ended
		/// </summary>
		protected virtual void OnMediaPlayerEnded()
		{
			Dispatcher.BeginInvoke((Action)(() => SetIsPlaying(false)));
			Dispatcher.BeginInvoke((Action)(() => RaiseEvent(new RoutedEventArgs(MediaEndedEvent))));
            Dispatcher.BeginInvoke((Action)(() => HasVideo = false));
		}

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		/// <summary>
		/// Executed when media has successfully been opened.
		/// </summary>
		protected virtual void OnMediaPlayerOpened()
		{
			/* Safely grab out our values */
			bool hasVideo = MediaPlayerBase.HasVideo;
			int videoWidth = MediaPlayerBase.NaturalVideoWidth;
			int videoHeight = MediaPlayerBase.NaturalVideoHeight;
			double volume;
			double balance;

			Dispatcher.BeginInvoke((Action)delegate
			{
				/* If we have no video just black out the video
				 * area by releasing the D3D surface */
				if (!hasVideo)
				{
					SetBackBuffer(IntPtr.Zero);
				}

				SetNaturalVideoWidth(videoWidth);
				SetNaturalVideoHeight(videoHeight);

				/* Set our dp values to match the media player */
				//SetHasVideo(hasVideo);

				/* Get our DP values */
				volume = Volume;
				balance = Balance;

				/* Make sure our volume and balances are set */
				MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
				{
					MediaPlayerBase.Volume = volume;
					MediaPlayerBase.Balance = balance;
				});

				AutoSizeControl();

				SetIsPlaying(true);
				RaiseEvent(new RoutedEventArgs(MediaOpenedEvent));
                HasVideo = hasVideo;
			});
		}

	    protected void AutoSizeControl()
	    {
	        Dispatcher.BeginInvoke((Action) (() =>
	        {
	            if (AutoSize && _currentWindow != null)
	            {
                    var mainw = ServiceLocator.GetService<IMainWindow>();
                    mainw.SetChildWindowsFollow(false);

	                InvalidateMeasure();
	                InvalidateArrange();
	                InvalidateVisual();

                    if (_currentWindow.WindowState == WindowState.Normal &&
                        MediaPlayerBase.NaturalVideoWidth > 0 &&
                        MediaPlayerBase.NaturalVideoHeight > 0 &&
                        MediaPlayerBase.HasVideo)
	                {
	                    var source = PresentationSource.FromVisual(_currentWindow);
	                    Matrix transformFromDevice =
	                        source.CompositionTarget.TransformFromDevice;

                        Vector monitorPosition;

	                    var ms = WindowExtensions.MonitorSize(ref _currentWindow,
	                                                        transformFromDevice, out monitorPosition);

	                    float dpiX, dpiY;
	                    Common.TaskbarNotification.Interop.WinApi.GetDPI(out dpiX, out dpiY);

                        double maxW = ms.Width * 0.7;
                        double maxH = ms.Height * 0.7;
                        double minW = _currentWindow.MinWidth * (dpiX / 96.0);
                        double minH = _currentWindow.MinHeight * (dpiX / 96.0);
                        double w = MediaPlayerBase.NaturalVideoWidth;
                        double h = MediaPlayerBase.NaturalVideoHeight;
                        double r = w / h;

                        var wih = new WindowInteropHelper(_currentWindow);
                        IntPtr hWnd = wih.Handle;

                        if (maxW > w && w > minW &&
                            maxH > h && h > minH)
                        {
                            w *= (dpiX / 96.0);
                            h *= (dpiY / 96.0);

                            double newW = w;
                            double newH = h;

                            SetWindowPos(hWnd, IntPtr.Zero, (int)((ms.Width * (dpiX / 96.0) - newW) / 2 + (monitorPosition.X * (dpiX / 96.0))),
                                    (int)((ms.Height * (dpiY / 96.0) - newH) / 2 + (monitorPosition.Y * (dpiY / 96.0))), (int)(newW), (int)(newH), 0);
                        }
                        else
                        {
                            double newW = maxW;
                            double newH = newW / r;

                            if (newH > maxH)
                            {
                                newH = maxH;
                                newW = newH * r;
                            }

                            newW *= (dpiX / 96.0);
                            newH *= (dpiY / 96.0);

                            SetWindowPos(hWnd, IntPtr.Zero, (int)((ms.Width * (dpiX / 96.0) - newW) / 2 + (monitorPosition.X * (dpiX / 96.0))),
                                    (int)((ms.Height * (dpiY / 96.0) - newH) / 2 + (monitorPosition.Y * (dpiY / 96.0))), (int)(newW), (int)(newH), 0);
                        }
                        //double mw = ms.Width*0.7*(dpiX/96.0);
                        //double mh = ms.Height*0.7*(dpiY/96.0);
                        //double w = MediaPlayerBase.NaturalVideoWidth;
                        //double h = MediaPlayerBase.NaturalVideoHeight;
                        //double r = w/h;

                        //if (w < _currentWindow.MinWidth)
                        //{
                        //    w = _currentWindow.MinWidth;
                        //    h = w/r;
                        //}
                        //if (h < _currentWindow.MinHeight)
                        //{
                        //    h = _currentWindow.MinHeight;
                        //    w = h*r;
                        //}

                        //if (w > mw)
                        //{
                        //    w = mw;
                        //    h = w/r;
                        //}
                        //if (h > mh)
                        //{
                        //    h = mh;
                        //    w = h*r;
                        //}

                        //var wih = new WindowInteropHelper(_currentWindow);
                        //IntPtr hWnd = wih.Handle;

                        //double minW = _currentWindow.MinWidth;
                        //double minH = _currentWindow.MinHeight;
                        //_currentWindow.MinWidth = 0;
                        //_currentWindow.MinHeight = 0;
                        //_currentWindow.Left = (int) ((ms.Width - w)/2);
                        //_currentWindow.Top = (int) ((ms.Height - h)/2);
                        //Size wpfSize = GetWPFSize(_currentWindow, new Size(w, h));
                        //_currentWindow.Width = wpfSize.Width;
                        //_currentWindow.Height = wpfSize.Height;
                        //SetWindowPos(hWnd, IntPtr.Zero, (int) _currentWindow.Left,
                        //            (int) _currentWindow.Top, (int) w, (int) h, 0);
                        //_currentWindow.MinWidth = minW;
                        //_currentWindow.MinHeight = minH;
	                }

                    mainw.SetChildWindowsFollow(true);
	            }
	        }), DispatcherPriority.ContextIdle);
	    }

	    public Size GetWPFSize(UIElement element, Size winSize)
        {
            Matrix transformToDevice;
            var source = PresentationSource.FromVisual(element);
            if (source != null)
                transformToDevice = source.CompositionTarget.TransformFromDevice;
            else
                using (var source2 = new HwndSource(new HwndSourceParameters()))
                    transformToDevice = source2.CompositionTarget.TransformFromDevice;

            return (Size)transformToDevice.Transform((Vector)winSize);
        }

        public Size GetPixelSize(UIElement element, Size wpfSize)
        {
            Matrix transformToDevice;
            var source = PresentationSource.FromVisual(element);
            if (source != null)
                transformToDevice = source.CompositionTarget.TransformToDevice;
            else
                using (var source2 = new HwndSource(new HwndSourceParameters()))
                    transformToDevice = source2.CompositionTarget.TransformToDevice;

            return (Size)transformToDevice.Transform((Vector)wpfSize);
        }

		/// <summary>
		/// Fires when the owner window is closed.  Nothing will happen
		/// if the visual does not belong to the visual tree with a root
		/// of a WPF window
		/// </summary>
		private void WindowOwnerClosed(object sender, EventArgs e)
		{
			ExecuteMediaState(UnloadedBehavior);
		}

		/// <summary>
		/// Local handler for the Loaded event
		/// </summary>
		private void MediaElementBaseUnloaded(object sender, RoutedEventArgs e)
		{
			/* Make sure we call our virtual method every time! */
			OnUnloadedOverride();

			if (Application.Current == null)
				return;

			_windowHooked = false;

			if (_currentWindow == null)
				return;

			_currentWindow.Closed -= WindowOwnerClosed;
			_currentWindow = null;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (MediaPlayerBase != null && MediaPlayerBase.HasVideo && MediaPlayerBase.NaturalVideoWidth != 0 && MediaPlayerBase.NaturalVideoHeight != 0)
			{
				var newSize = availableSize;
				var wnd = _currentWindow;
				if (wnd != null && wnd.WindowState == WindowState.Normal && wnd.SizeToContent == SizeToContent.WidthAndHeight)
				{
					var vw = MediaPlayerBase.NaturalVideoWidth;
					var vh = MediaPlayerBase.NaturalVideoHeight;
					var sz = wnd.ComputeNewVideoSize(this, new Size(vw, vh));
					if (sz.Width <= availableSize.Width && sz.Height <= availableSize.Height)
						newSize = sz;
				}
				else if (wnd != null && wnd.WindowState == WindowState.Maximized)
				{
					var vr = (double) MediaPlayerBase.NaturalVideoWidth/((double) MediaPlayerBase.NaturalVideoHeight);
					var ar = availableSize.Width/availableSize.Height;

					if (ar < vr)
					{
						newSize = new Size(availableSize.Width, availableSize.Width / vr);
					}
					else if (ar == vr)
					{
						newSize = availableSize;
					}
					else
					{
						newSize = new Size(availableSize.Height * vr, availableSize.Height);
					}
				}
				var c = GetVisualChild(0) as UIElement;
				c.Measure(newSize);
				return newSize;	
			}
			else
			{
				return base.MeasureOverride(availableSize);				
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var c = GetVisualChild(0) as UIElement;
			c.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
			return finalSize;
		}

		/// <summary>
		/// Local handler for the Unloaded event
		/// </summary>
		private void MediaElementBaseLoaded(object sender, RoutedEventArgs e)
		{
			_currentWindow = Window.GetWindow(this);

			if (_currentWindow != null && !_windowHooked)
			{
                var wih = new WindowInteropHelper(_currentWindow);
                _hwnd = wih.Handle;
				_currentWindow.Closed += WindowOwnerClosed;
				_windowHooked = true;
			}

			OnLoadedOverride();

            ServiceLocator.RegisterOverrideService((IEqualizer)this);
		}

		/// <summary>
		/// Runs when the Loaded event is fired and executes
		/// the LoadedBehavior
		/// </summary>
		protected virtual void OnLoadedOverride()
		{
			Dispatcher.BeginInvoke((Action)(()=>{
				InitializeMediaPlayerPrivate();
				ExecuteMediaState(LoadedBehavior);
			}));
		}

		/// <summary>
		/// Runs when the Unloaded event is fired and executes
		/// the UnloadedBehavior
		/// </summary>
		protected virtual void OnUnloadedOverride()
		{
			ExecuteMediaState(UnloadedBehavior);
		}

		public ObservableCollection<string> SubtitlesStreams
		{
			get { return (ObservableCollection<string>)GetValue(SubtitlesStreamsProperty); }
			set { SetValue(SubtitlesStreamsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Subtitles.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SubtitlesStreamsProperty =
			DependencyProperty.Register("SubtitlesStreams", typeof(ObservableCollection<string>), typeof(MediaElementBase), new UIPropertyMetadata(null));



		public ObservableCollection<string> AudioStreams
		{
			get { return (ObservableCollection<string>)GetValue(AudioStreamsProperty); }
			set { SetValue(AudioStreamsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AudioStreams.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AudioStreamsProperty =
			DependencyProperty.Register("AudioStreams", typeof(ObservableCollection<string>), typeof(MediaElementBase), new UIPropertyMetadata(null));



		public ObservableCollection<string> VideoStreams
		{
			get { return (ObservableCollection<string>)GetValue(VideoStreamsProperty); }
			set { SetValue(VideoStreamsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for VideoStreams.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty VideoStreamsProperty =
			DependencyProperty.Register("VideoStreams", typeof(ObservableCollection<string>), typeof(MediaElementBase), new UIPropertyMetadata(null));


        public bool HasVideo
        {
            get { return (bool)GetValue(HasVideoProperty); }
            set { SetValue(HasVideoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VideoStreams.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasVideoProperty =
            DependencyProperty.Register("HasVideo", typeof(bool), typeof(MediaElementBase), new UIPropertyMetadata(false));


		/// <summary>
		/// Executes the actions associated to a MediaState
		/// </summary>
		/// <param name="state">The MediaState to execute</param>
		protected void ExecuteMediaState(MediaState state)
		{
			switch (state)
			{
				case MediaState.Manual:
					break;
				case MediaState.Play:
					Play();
					break;
				case MediaState.Stop:
					Stop();
					break;
				case MediaState.Close:
					//Close();
					break;
				case MediaState.Pause:
					Pause();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public override void BeginInit()
		{
			bool designTime = DesignerProperties.GetIsInDesignMode(new DependencyObject());
			if (designTime) return;

			HasInitialized = false;
			base.BeginInit();
		}

		public override void EndInit()
		{
			bool designTime = DesignerProperties.GetIsInDesignMode(new DependencyObject());
			if (designTime) return;

			double balance = Balance;
			double volume = Volume;

			MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
			{
				MediaPlayerBase.Balance = balance;
				MediaPlayerBase.Volume = volume;
			});

			HasInitialized = true;
			base.EndInit();
		}

		public bool HasInitialized
		{
			get;
			protected set;
		}

		IntPtr _lastBuffer;

		/// <summary>
		/// Plays the media
		/// </summary>
		public virtual void Play()
		{
			//if (_lastBuffer != IntPtr.Zero) SetBackBuffer(_lastBuffer);
			MediaPlayerBase.EnsureThread(DefaultApartmentState);
			MediaPlayerBase.Dispatcher.BeginInvoke((Action)(delegate
			{
				MediaPlayerBase.Play();
				Dispatcher.BeginInvoke(((Action)(() => SetIsPlaying(true))));
			}));

		}

		/// <summary>
		/// Pauses the media
		/// </summary>
		public virtual void Pause()
		{
			MediaPlayerBase.EnsureThread(DefaultApartmentState);
			MediaPlayerBase.Dispatcher.BeginInvoke((Action)(() => MediaPlayerBase.Pause()));
			SetIsPlaying(false);
		}

		/// <summary>
		/// Closes the media
		/// </summary>
		public virtual void Close()
		{
			SetBackBuffer(IntPtr.Zero);
			InvalidateVideoImage();

			if (!MediaPlayerBase.Dispatcher.Shutdown || !MediaPlayerBase.Dispatcher.ShuttingDown)
				MediaPlayerBase.Dispatcher.BeginInvoke((Action)(delegate
				{
						MediaPlayerBase.Close();
						MediaPlayerBase.Dispose();
				}));

			SetIsPlaying(false);
		}

		/// <summary>
		/// Stops the media
		/// </summary>
		public virtual void Stop()
		{
			if (!MediaPlayerBase.Dispatcher.Shutdown || !MediaPlayerBase.Dispatcher.ShuttingDown)
				MediaPlayerBase.Dispatcher.BeginInvoke((Action)(() => MediaPlayerBase.Stop()));

			_lastBuffer = m_pBackBuffer;
			SetBackBuffer(IntPtr.Zero);
			InvalidateVideoImage();

			SetIsPlaying(false);
		}

		/// <summary>
		/// Called when a MediaPlayerBase is required.
		/// </summary>
		/// <returns>This method must return a valid (not null) MediaPlayerBase</returns>
		protected virtual MediaPlayerBase OnRequestMediaPlayer()
		{
			return null;
		}

		protected bool IsInDesignMode
		{
			get
			{
				if (DesignerProperties.GetIsInDesignMode(this))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string property)
		{
			var pc = PropertyChanged;
			if (pc != null)
			{
				pc(this, new PropertyChangedEventArgs(property));
			}
		}

        public void SetBand(int channel, int band, sbyte value)
        {
            if (!MediaPlayerBase.Dispatcher.Shutdown || !MediaPlayerBase.Dispatcher.ShuttingDown)
                MediaPlayerBase.Dispatcher.BeginInvoke((Action)(() => MediaPlayerBase.SetBand(channel, band, value)));
        }
    }
}