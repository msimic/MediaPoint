#region Includes
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using DirectShowLib;
using MediaPoint.Common.MediaFoundation;
using MediaPoint.Common.MediaFoundation.Interop;
using MediaPoint.Common.Threading;
using MediaPoint.Subtitles;
using Size=System.Windows.Size;
using MediaPoint.Common.Interfaces;
using System.Text;
#endregion

namespace MediaPoint.Common.DirectShow.MediaPlayers
{
    public enum MediaState
    {
        Manual,
        Play,
        Stop,
        Close,
        Pause
    }

    /// <summary>
    /// The types of position formats that
    /// are available for seeking media
    /// </summary>
    public enum MediaPositionFormat
    {
        MediaTime,
        Frame,
        Byte,
        Field,
        Sample,
        None
    }

    public class SubtitleSettings
    {
        private SubtitleItem _subtitle;
        public SubtitleItem Subtitle
        {
            get { return _subtitle; }
            set
            {
                _subtitle = value;
                Owner.LoadCurrentSub();
            }
        }

        private string _fontFamily;
        public string FontFamily
        {
            get { return _fontFamily; }
            set
            {
                _fontFamily = value;
                Owner.InitSubSettings();
            }
        }

        private FontCharSet _fontCharset;
        public FontCharSet Charset
        {
            get { return _fontCharset; }
            set
            {
                _fontCharset = value;
                Owner.InitSubSettings();
            }
        }

        private int _size;
        public int Size
        {
            get { return _size; }
            set {
                _size = value;
                Owner.InitSubSettings();
            }
        }
        
        private bool _bold;
        public bool Bold
        {
            get { return _bold; }
            set {
                _bold = value;
                Owner.InitSubSettings();
            }
        }

        private System.Windows.Media.Color _color;
        public System.Windows.Media.Color Color
        {
            get { return _color; }
            set {
                _color = value;
                Owner.InitSubSettings();
            }
        }

        private bool _shadow;
        public bool Shadow
        {
            get { return _shadow; }
            set {
                _shadow = value;
                Owner.InitSubSettings();
            }
        }

        private bool _outline;
        public bool Outline
        {
            get { return _outline; }
            set {
                _outline = value;
                Owner.InitSubSettings();
            }
        }

        public MediaPlayerBase Owner { get; set; }
    }

    /// <summary>
    /// Delegate signature to notify of a new surface
    /// </summary>
    /// <param name="sender">The sender of the event</param>
    /// <param name="pSurface">The pointer to the D3D surface</param>
    public delegate void NewAllocatorSurfaceDelegate(object sender, IntPtr pSurface);

    /// <summary>
    /// The arguments that store information about a failed media attempt
    /// </summary>
    public class MediaFailedEventArgs : EventArgs
    {
        public MediaFailedEventArgs(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }

        public Exception Exception { get; protected set; }
        public string Message { get; protected set; }
    }

    /// <summary>
    /// The custom allocator interface.  All custom allocators need
    /// to implement this interface.
    /// </summary>
    public interface ICustomAllocator : IDisposable
    {
        /// <summary>
        /// Invokes when a new frame has been allocated
        /// to a surface
        /// </summary>
        event Action NewAllocatorFrame;

        /// <summary>
        /// Invokes when a new surface has been allocated
        /// </summary>
        event NewAllocatorSurfaceDelegate NewAllocatorSurface;
    }

    [ComImport, Guid("FA10746C-9B63-4b6c-BC49-FC300EA5F256")]
    internal class EnhancedVideoRenderer
    {
    }

	[ComImport, Guid("E1A8B82A-32CE-4B0D-BE0D-AA68C772E423")]
	internal class MadVrRenderer
	{
	}

    /// <summary>
    /// A low level window class that is used to provide interop with libraries
    /// that require an hWnd 
    /// </summary>
    public class HiddenWindow : NativeWindow
    {
        public delegate IntPtr WndProcHookDelegate(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);

        readonly List<WndProcHookDelegate> m_handlerlist = new List<WndProcHookDelegate>();

        public void AddHook(WndProcHookDelegate method)
        {
            if (m_handlerlist.Contains(method))
                return;

            lock (((System.Collections.ICollection)m_handlerlist).SyncRoot)
                m_handlerlist.Add(method);
        }

        public void RemoveHook(WndProcHookDelegate method)
        {
            lock (((System.Collections.ICollection)m_handlerlist).SyncRoot)
                m_handlerlist.Remove(method);
        }

        /// <summary>
        /// Invokes the windows procedure associated to this window
        /// </summary>
        /// <param name="m">The window message to send to window</param>
        protected override void WndProc(ref Message m)
        {
            bool isHandled = false;

            lock (((System.Collections.ICollection)m_handlerlist).SyncRoot)
            {
                foreach (WndProcHookDelegate method in m_handlerlist)
                {
                    method.Invoke(m.HWnd, m.Msg, m.WParam, m.LParam, ref isHandled);
                    if (isHandled)
                        break;
                }
            }

            base.WndProc(ref m);
        }
    }

    /// <summary>
    /// Specifies different types of DirectShow
    /// Video Renderers
    /// </summary>
    public enum VideoRendererType
    {
        VideoMixingRenderer9 = 0,
        EnhancedVideoRenderer,
		MadVr
    }

    /// <summary>
    /// The MediaPlayerBase is a base class to build raw, DirectShow based players.
    /// It inherits from DispatcherObject to allow easy communication with COM objects
    /// from different apartment thread models.
    /// </summary>
    public abstract class MediaPlayerBase : WorkDispatcherObject, INotifyPropertyChanged, IEqualizer
    {
        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetDesktopWindow();

        /// <summary>
        /// A static value to hold a count for all graphs.  Each graph
        /// has it's own value that it uses and is updated by the
        /// GraphInstanceCookie property in the get method
        /// </summary>
        private static int m_graphInstances;

        /// <summary>
        /// The custom windows message constant for graph events
        /// </summary>
        private const int WM_GRAPH_NOTIFY = 0x0400 + 13;

        /// <summary>
        /// One second in 100ns units
        /// </summary>
        protected const long DSHOW_ONE_SECOND_UNIT = 10000000;

        /// <summary>
        /// The IBasicAudio volume value for silence
        /// </summary>
        private const int DSHOW_VOLUME_SILENCE = -10000;

        /// <summary>
        /// The IBasicAudio volume value for full volume
        /// </summary>
        private const int DSHOW_VOLUME_MAX = 0;

        /// <summary>
        /// The IBasicAudio balance max absolute value
        /// </summary>
        private const int DSHOW_BALACE_MAX_ABS = 10000;

        /// <summary>
        /// Rate which our DispatcherTimer polls the graph
        /// </summary>
        private const int DSHOW_TIMER_POLL_MS = 33;

        /// <summary>
        /// UserId value for the VMR9 Allocator - Not entirely useful
        /// for this application of the VMR
        /// </summary>
        private readonly IntPtr m_userId = new IntPtr(unchecked((int)0xDEADBEEF));

        /// <summary>
        /// Static lock.  Seems multiple EVR controls instantiated at the same time crash
        /// </summary>
        private static readonly object m_videoRendererInitLock = new object();

        /// <summary>
        /// DirectShow interface for controlling audio
        /// functions such as volume and balance
        /// </summary>
        private IBasicAudio m_basicAudio;

        /// <summary>
        /// The custom DirectShow allocator
        /// </summary>
        private ICustomAllocator m_customAllocator;

        /// <summary>
        /// Flag for the Dispose pattern
        /// </summary>
        private bool m_disposed;

        /// <summary>
        /// The DirectShow filter graph reference
        /// </summary>
        protected IGraphBuilder m_graph;

        /// <summary>
        /// The hWnd pointer we use for D3D stuffs
        /// </summary>
        private HiddenWindow m_window;

        /// <summary>
        /// The DirectShow interface for controlling the
        /// filter graph.  This provides, Play, Pause, Stop, etc
        /// functionality.
        /// </summary>
        private IMediaControl m_mediaControl;

        /// <summary>
        /// The DirectShow interface for getting events
        /// that occur in the FilterGraph.
        /// </summary>
        private IMediaEventEx m_mediaEvent;

        /// <summary>
        /// Flag for if our media has video
        /// </summary>
        private bool m_hasVideo;

        /// <summary>
        /// Flag for if our media has audio
        /// </summary>
        private bool m_hasAudio;

        /// <summary>
        /// The natural video pixel height, if applicable
        /// </summary>
        private int m_naturalVideoHeight;

        /// <summary>
        /// The natural video pixel width, if applicable
        /// </summary>
        private int m_naturalVideoWidth;

        /// <summary>
        /// Our Win32 timer to poll the DirectShow graph
        /// </summary>
        private System.Timers.Timer m_timer;

        protected IMFVideoDisplayControl _displayControl;
        protected IVideoWindow _displayControlVMR;

        protected IBaseFilter _renderer;
        protected IFileSourceFilter _splitter;
        protected IBaseFilter _video;
        protected IBaseFilter _audio;
        protected IBaseFilter _audioRenderer;
        protected IDCEqualizer _equalizer;
        protected IDCDSPFilterInterface _dspFilter;
        protected IDCDownMix _downmix;
        protected IDCAmplify _amplify;
        protected ILAVAudioStatus _audioStatus;

        protected MediaPlayerBase()
        {
            if (_vobsub != null) Marshal.ReleaseComObject(_vobsub);
            if (_video != null) Marshal.ReleaseComObject(_video);
            if (_audio != null) Marshal.ReleaseComObject(_audio);
            if (_splitter != null) Marshal.ReleaseComObject(_splitter);
            if (_renderer != null) Marshal.ReleaseComObject(_renderer);

            _vobsub = null;
            _video = null;
            _audio = null;
            _splitter = null;
            _renderer = null;

            _subsettings = new SubtitleSettings {Owner = this};
            _subsettings.Size = 24;
            _subsettings.Charset = FontCharSet.Default;
            _subsettings.FontFamily = "Impact";
            _subsettings.Bold = false;
            _subsettings.Color = System.Windows.Media.Colors.White;
            _subsettings.Outline = true;
            _subsettings.Shadow = true;
        }

        /// <summary>
        /// This objects last stand
        /// </summary>
        ~MediaPlayerBase()
        {
            Dispose();
        }

        /// <summary>
        /// The global instance Id of the graph.  We use this
        /// for the WndProc callback method.
        /// </summary>
        private int? m_graphInstanceId;

        /// <summary>
        /// The globally unqiue identifier of the graph
        /// </summary>
        protected int GraphInstanceId
        {
            get
            {
                if (m_graphInstanceId != null)
                    return m_graphInstanceId.Value;

                /* Increment our static value and store the current
                 * instance id of our player graph */
                m_graphInstanceId = Interlocked.Increment(ref m_graphInstances);

                return m_graphInstanceId.Value;
            }
        }


		static DsDevice[] audioDevs = DsDevice.GetDevicesOfCat(FilterCategory.AudioRendererCategory);
		public static string[] AudioRenderers
		{
			get
			{
				return (from m in audioDevs select m.Name).ToArray();
			}
		}

        private SubtitleSettings _subsettings;
        public SubtitleSettings SubtitleSettings
        {
            get { return _subsettings; }
        }

        private int _numSamples;
        public int NumSamples
        {
            get { return _numSamples; }
            set
            {
                _numSamples = value;
                InvokeNewSamplesNumber();
            }
        }

        private TDSStream _audioStreamInfo;
        public TDSStream AudioStreamInfo
        {
            get { return _audioStreamInfo; }
            set
            {
                var oldStream = _audioStreamInfo;

                _audioStreamInfo = value;

                if (oldStream.Channels != value.Channels ||
                    oldStream.Bits != value.Bits ||
                    oldStream.Frequency != value.Frequency ||
                    oldStream._Float != value._Float)
                {
                    InvokeNewAudioStream();
                }
            }
        }

        private float[] _FFTData;
        public float[] FFTData
        {
            get { return _FFTData; }
            set
            {
                _FFTData = value;
                InvokeNewFFT();
            }
        }

        private static uint MakeCOLORREF(byte r, byte g, byte b)
        {
            return (uint)(((uint)r) | (((uint)g) << 8) | (((uint)b) << 16));
        }

        public void LoadCurrentSub()
        {
            if (!HasVideo) return;
            //Dispatcher.BeginInvoke((Action) (() =>
            //{
                if (_vobsub != null && SubtitleSettings.Subtitle != null &&
                    SubtitleSettings.Subtitle.Type ==
                    SubtitleItem.SubtitleType.File)
                {
                    int ret = ((IDirectVobSub3) _vobsub).OpenSubtitles(SubtitleSettings.Subtitle.Path);
                    //if (ret != 0) MessageBox.Show(
                    //    "Failed to load subtitle",
                    //    "MediaPoint", MessageBoxButtons.OK,
                    //    MessageBoxIcon.Information);
                }
                else if (_vobsub != null && SubtitleSettings.Subtitle != null &&
                        SubtitleSettings.Subtitle.Type ==
                        SubtitleItem.SubtitleType.Embedded)
                {
                    //_splitterSettings.SetAdvancedSubtitleConfig()
                }
            //}));
        }

        public void InitSubSettings()
        {
            if (_vobsub == null || SubtitleSettings.FontFamily == null) return;

            LOGFONT lf = new LOGFONT();
            lf.lfCharSet = (byte)(int)SubtitleSettings.Charset;
            lf.lfFaceName = SubtitleSettings.FontFamily.Substring(0, Math.Min(SubtitleSettings.FontFamily.Length, 32));
            lf.lfHeight = SubtitleSettings.Size * -1;
            lf.lfWeight = SubtitleSettings.Bold ? 700 : 400;

            _vobsub.put_TextSettings(lf, 92, MakeCOLORREF(SubtitleSettings.Color.R, SubtitleSettings.Color.G, SubtitleSettings.Color.B), SubtitleSettings.Shadow, SubtitleSettings.Outline, true);
        }

		private List<string> _subtitles = new List<string>();
		public List<string> SubtitleStreams
		{
			get
			{
				return _subtitles;
			}
		}

		private List<string> _audios = new List<string>();
		public List<string> AudioStreams
		{
			get
			{
				return _audios;
			}
		}

		private List<string> _videos = new List<string>();
		public List<string> VideoStreams
		{
			get
			{
				return _videos;
			}
		}

        /// <summary>
        /// Helper function to get a valid hWnd to
        /// use with DirectShow and Direct3D
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void GetMainWindowHwndHelper()
        {
            if (m_window == null)
                m_window = new HiddenWindow();
            else
                return;

            if (m_window.Handle == IntPtr.Zero)
            {
                lock(m_window)
                {
                    m_window.CreateHandle(new CreateParams());
                }
            }
        }

        protected virtual HiddenWindow HwndHelper
        {
            get
            {
                if (m_window != null)
                    return m_window;

                GetMainWindowHwndHelper();

                return m_window;
            }
        }

        /// <summary>
        /// Is true if the media contains renderable audio
        /// </summary>
        public virtual bool HasAudio
        {
            get
            {
                return m_hasAudio;
            }
            protected set
            {
                m_hasAudio = value;
            }
        }

        /// <summary>
        /// Is true if the media contains renderable video
        /// </summary>
        public virtual bool HasVideo
        {
            get
            {
                return m_hasVideo;
            }
            protected set
            {
                m_hasVideo = value;
            }
        }

        /// <summary>
        /// Gets the natural pixel width of the current media.
        /// The value will be 0 if there is no video in the media.
        /// </summary>
        public virtual int NaturalVideoWidth
        {
            get
            {
                //VerifyAccess();
                return m_naturalVideoWidth;
            }
            protected set
            {
                VerifyAccess();
                m_naturalVideoWidth = value;
            }
        }

        /// <summary>
        /// Gets the natural pixel height of the current media.  
        /// The value will be 0 if there is no video in the media.
        /// </summary>
        public virtual int NaturalVideoHeight
        {
            get
            {
                //VerifyAccess();
                return m_naturalVideoHeight;
            }
            protected set
            {
                VerifyAccess();
                m_naturalVideoHeight = value;
            }
        }

        protected static IDirectVobSub _vobsub;
        public static IDirectVobSub VobSubSettings
        {
            get
            {
                return _vobsub;
            }
        }

		protected static IEVRPresenterSettings _settings;
		public static IEVRPresenterSettings EvrSettings
		{
			get
			{
				return _settings;
			}
		}


		protected ILAVSplitterSettings _splitterSettings;
		public ILAVSplitterSettings SplitterSettings
		{
			get
			{
				return _splitterSettings;
			}
		}

        /// <summary>
        /// Gets or sets the audio volume.  Specifies the volume, as a 
        /// number from 0 to 1.  Full volume is 1, and 0 is silence.
        /// </summary>
        public virtual double Volume
        {
            get
            {
                VerifyAccess();

                /* Check if we even have an 
                 * audio interface */
                if (m_basicAudio == null)
                    return 0;

                int dShowVolume;

                /* Get the current volume value from the interface */
                m_basicAudio.get_Volume(out dShowVolume);

                /* Do calulations to convert to a base of 0 for silence */
                dShowVolume -= DSHOW_VOLUME_SILENCE;
                return (double)dShowVolume / -DSHOW_VOLUME_SILENCE;
            }
            set
            {
                VerifyAccess();

                /* Check if we even have an
                 * audio interface */
                if (m_basicAudio == null)
                    return;

                if (value <= 0) /* Value should not be negative or else we treat as silence */
                    m_basicAudio.put_Volume(DSHOW_VOLUME_SILENCE);
                else if (value >= 1)/* Value should not be greater than one or else we treat as maximum volume */
                    m_basicAudio.put_Volume(DSHOW_VOLUME_MAX);
                else
                {
                    /* With the IBasicAudio interface, sound is DSHOW_VOLUME_SILENCE
                     * for silence and DSHOW_VOLUME_MAX for full volume
                     * so we calculate that here based off an input of 0 of silence and 1.0
                     * for full audio */
                    int dShowVolume = (int)((1 - value) * DSHOW_VOLUME_SILENCE);
                    m_basicAudio.put_Volume(dShowVolume);
                }
            }
        }

        /// <summary>
        /// Gets or sets the balance on the audio.
        /// The value can range from -1 to 1. The value -1 means the right channel is attenuated by 100 dB 
        /// and is effectively silent. The value 1 means the left channel is silent. The neutral value is 0, 
        /// which means that both channels are at full volume. When one channel is attenuated, the other 
        /// remains at full volume.
        /// </summary>
        public virtual double Balance
        {
            get
            {
                VerifyAccess();

                /* Check if we even have an 
                 * audio interface */
                if (m_basicAudio == null)
                    return 0;

                int balance;

                /* Get the interface supplied balance value */
                m_basicAudio.get_Balance(out balance);

                /* Calc and return the balance based on 0 == silence */
                return (double)balance / DSHOW_BALACE_MAX_ABS;
            }
            set
            {
                VerifyAccess();

                /* Check if we even have an 
                 * audio interface */
                if (m_basicAudio == null)
                    return;

                /* Calc the dshow balance value */
                int balance = (int)value * DSHOW_BALACE_MAX_ABS;

                m_basicAudio.put_Balance(balance);
            }
        }

        /// <summary>
        /// Event notifies when there is a new video frame
        /// to be rendered
        /// </summary>
        public event Action NewAllocatorFrame;

        /// <summary>
        /// Event notifies when there is a new surface allocated
        /// </summary>
        public event NewAllocatorSurfaceDelegate NewAllocatorSurface;

        /// <summary>
        /// Frees any remaining memory
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            //GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Part of the dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            //if (m_disposed)
            //    return;

            if(!disposing) 
                return;

            if (m_window != null)
            {
                m_window.RemoveHook(WndProcHook);
                m_window.DestroyHandle();
                m_window = null;
            }

            if (m_timer != null)
                m_timer.Dispose();

            m_timer = null;
               
            if(CheckAccess())
            {
                FreeResources();
                Dispatcher.BeginInvokeShutdown();
            }
            else
            {
                Dispatcher.BeginInvoke((Action)delegate
                {
                    FreeResources();
                    Dispatcher.BeginInvokeShutdown();
                });
            }

			var tmp = m_disposed;
            m_disposed = true;
        }

        /// <summary>
        /// Polls the graph for various data about the media that is playing
        /// </summary>
        protected virtual void OnGraphTimerTick()
        {
        }

        /// <summary>
        /// Is called when a new media event code occurs on the graph
        /// </summary>
        /// <param name="code">The event code that occured</param>
        /// <param name="param1">The first parameter sent by the graph</param>
        /// <param name="param2">The second parameter sent by the graph</param>
        protected virtual void OnMediaEvent(EventCode code, IntPtr param1, IntPtr param2)
        {
            switch (code)
            {
                case EventCode.Complete:
                    InvokeMediaEnded(null);
                    StopGraphPollTimer();
                    break;
                case EventCode.Paused:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Starts the graph polling timer to update possibly needed
        /// things like the media position
        /// </summary>
        protected void StartGraphPollTimer()
        {
            if (m_timer == null)
            {
                m_timer = new System.Timers.Timer();
                m_timer.Interval = DSHOW_TIMER_POLL_MS;
                m_timer.Elapsed += TimerElapsed;
            }

            m_timer.Enabled = true;

            /* Make sure we get windows messages */
            AddWndProcHook();
        }

        private void ProcessGraphEvents()
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                if (m_mediaEvent != null)
                {
                    IntPtr param1;
                    IntPtr param2;
                    EventCode code;

                    /* Get all the queued events from the interface */
                    while (m_mediaEvent.GetEvent(out code, out param1, out param2, 0) == 0)
                    {
                        /* Handle anything for this event code */
                        OnMediaEvent(code, param1, param2);

                        /* Free everything..we only need the code */
                        m_mediaEvent.FreeEventParams(code, param1, param2);
                    }
                }
            });
        }

        private void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                ProcessGraphEvents();
                OnGraphTimerTick();
            });
        }

        /// <summary>
        /// Stops the graph polling timer
        /// </summary>
        protected void StopGraphPollTimer()
        {
            if (m_timer != null)
            {
                m_timer.Stop();
                m_timer.Dispose();
                m_timer = null;
            }

            /* Stop listening to windows messages */
            RemoveWndProcHook();
        }

        /// <summary>
        /// Removes our hook that listens to windows messages
        /// </summary>
        private void RemoveWndProcHook()
        {
            /* Make sure to stop our IMediaEventEx also */
            UnsetMediaEventExNotifyWindow();
            //HwndHelper.RemoveHook(WndProcHook);
        }

        /// <summary>
        /// Adds a hook that listens to windows messages
        /// </summary>
        private void AddWndProcHook()
        {
           HwndHelper.AddHook(WndProcHook);
        }

        /// <summary>
        /// Receives windows messages.  This is primarily used to get
        /// events that happen on our graph
        /// </summary>
        /// <param name="hwnd">The window handle</param>
        /// <param name="msg">The message Id</param>
        /// <param name="wParam">The message's wParam value</param>
        /// <param name="lParam">The message's lParam value</param>
        /// <param name="handled">A value that indicates whether the message was handled. Set the value to true if the message was handled; otherwise, false. </param>
        private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            ProcessGraphEvents();

            return IntPtr.Zero;
        }

        /// <summary>
        /// Unhooks the IMediaEventEx from the notification hWnd
        /// </summary>
        private void UnsetMediaEventExNotifyWindow()
        {
            if (m_mediaEvent == null)
                return;

            /* Setting the notify window to IntPtr.Zero unsubscribes the events */
            //int hr = m_mediaEvent.SetNotifyWindow(IntPtr.Zero, WM_GRAPH_NOTIFY, (IntPtr)GraphInstanceId);
        }

        /// <summary>
        /// Sets the MediaEventEx interface
        /// </summary>
        private void SetMediaEventExInterface(IMediaEventEx mediaEventEx)
        {
            m_mediaEvent = mediaEventEx;

            //int hr = m_mediaEvent.SetNotifyWindow(HwndHelper.Handle, WM_GRAPH_NOTIFY, (IntPtr)GraphInstanceId);
        }

        /// <summary>
        /// Configures all general DirectShow interfaces that the
        /// FilterGraph supplies.
        /// </summary>
        /// <param name="graph">The FilterGraph to setup</param>
        protected virtual void SetupFilterGraph(IFilterGraph graph)
        {
            m_graph = (IGraphBuilder)graph;

            /* Setup the interfaces and query basic information
             * on the graph that is passed */
            SetBasicAudioInterface(m_graph as IBasicAudio);
            SetMediaControlInterface(m_graph as IMediaControl);
            SetMediaEventExInterface(m_graph as IMediaEventEx);
        }

        /// <summary>
        /// Sets the MediaControl interface
        /// </summary>
        private void SetMediaControlInterface(IMediaControl mediaControl)
        {
            m_mediaControl = mediaControl;
        }

        /// <summary>
        /// Sets the basic audio interface for controlling
        /// volume and balance
        /// </summary>
        protected void SetBasicAudioInterface(IBasicAudio basicAudio)
        {
            m_basicAudio = basicAudio;
        }

        /// <summary>
        /// Notifies when the media has successfully been opened
        /// </summary>
        public event Action MediaOpened;

        /// <summary>
        /// Notifies when the media has been closed
        /// </summary>
        public event Action MediaClosed;

        /// <summary>
        /// Notifies when we have new fft data
        /// </summary>
        public event Action NewFFTData;

        /// <summary>
        /// Notifies when we have new fft samples
        /// </summary>
        public event Action NewSamplesNumber;

        /// <summary>
        /// Notifies when we have NewAudioStream
        /// </summary>
        public event Action NewAudioStream;

        /// <summary>
        /// Notifies when the media has failed and produced an exception
        /// </summary>
        public event EventHandler<MediaFailedEventArgs> MediaFailed;

        /// <summary>
        /// Notifies when the media has found no subtitles
        /// </summary>
        public event EventHandler<EventArgs> NoSubtitleLoaded;

        /// <summary>
        /// Notifies when the media has completed
        /// </summary>
        public event Action MediaEnded;

        /// <summary>
        /// Registers the custom allocator and hooks into it's supplied events
        /// </summary>
        protected void RegisterCustomAllocator(ICustomAllocator allocator)
        {
            FreeCustomAllocator();

            if (allocator == null)
                return;

            m_customAllocator = allocator;

            m_customAllocator.NewAllocatorFrame += CustomAllocatorNewAllocatorFrame;
            m_customAllocator.NewAllocatorSurface += CustomAllocatorNewAllocatorSurface;
        }

        /// <summary>
        /// Local event handler for the custom allocator's new surface event
        /// </summary>
        private void CustomAllocatorNewAllocatorSurface(object sender, IntPtr pSurface)
        {
            InvokeNewAllocatorSurface(pSurface);
        }

        /// <summary>
        /// Local event handler for the custom allocator's new frame event
        /// </summary>
        private void CustomAllocatorNewAllocatorFrame()
        {
            InvokeNewAllocatorFrame();
        }

        /// <summary>
        /// Disposes of the current allocator
        /// </summary>
        protected void FreeCustomAllocator()
        {
            if (m_customAllocator == null)
                return;

            m_customAllocator.Dispose();

            m_customAllocator.NewAllocatorFrame -= CustomAllocatorNewAllocatorFrame;
            m_customAllocator.NewAllocatorSurface -= CustomAllocatorNewAllocatorSurface;

            if(Marshal.IsComObject(m_customAllocator))
                Marshal.ReleaseComObject(m_customAllocator);
            
            m_customAllocator = null;
        }

        /// <summary>
        /// Resets the local graph resources to their
        /// default settings
        /// </summary>
        private void ResetLocalGraphResources()
        {
            m_graph = null;

            if (m_basicAudio != null)
                Marshal.ReleaseComObject(m_basicAudio);
            m_basicAudio = null;

            if (m_mediaControl != null)
                Marshal.ReleaseComObject(m_mediaControl);
            m_mediaControl = null;

            if(m_mediaEvent != null)
                Marshal.ReleaseComObject(m_mediaEvent);
            m_mediaEvent = null;
        }

        /// <summary>
        /// Frees any allocated or unmanaged resources
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected virtual void FreeResources()
        {
            StopGraphPollTimer();
            ResetLocalGraphResources();
            FreeCustomAllocator();
        }

        /// <summary>
        /// Creates a new renderer and configures it with a custom allocator
        /// </summary>
        /// <param name="rendererType">The type of renderer we wish to choose</param>
        /// <param name="graph">The DirectShow graph to add the renderer to</param>
        /// <param name="streamCount">Number of input pins for the renderer</param>
        /// <returns>An initialized DirectShow renderer</returns>
        protected IBaseFilter InsertVideoRenderer(VideoRendererType rendererType, IGraphBuilder graph, int streamCount)
        {
            IBaseFilter renderer;

            switch (rendererType)
            {
                case VideoRendererType.VideoMixingRenderer9:
                    renderer = CreateVideoMixingRenderer9(graph, streamCount);
                    break;
                case VideoRendererType.EnhancedVideoRenderer:
                    renderer = CreateEnhancedVideoRenderer(graph, streamCount);
                    break;
				case VideoRendererType.MadVr:
            		renderer = CreateMadVrRenderer(graph, streamCount);
            		break;
                default:
                    throw new ArgumentOutOfRangeException("rendererType");
            }

            return renderer;
        }

        /// <summary>
        /// Creates a new renderer and configures it with a custom allocator
        /// </summary>
        /// <param name="rendererType">The type of renderer we wish to choose</param>
        /// <param name="graph">The DirectShow graph to add the renderer to</param>
        /// <returns>An initialized DirectShow renderer</returns>
        protected IBaseFilter CreateVideoRenderer(VideoRendererType rendererType, IGraphBuilder graph)
        {
            return InsertVideoRenderer(rendererType, graph, 1);
        }

		/// <summary>
		/// Creates and instance of MadVr
		/// </summary>
		private IBaseFilter CreateMadVrRenderer(IGraphBuilder graph, int streamCount)
		{
			IBaseFilter filter;
			EvrPresenter presenter;

			lock (m_videoRendererInitLock)
			{
				var madVr = new MadVrRenderer();
				filter = madVr as IBaseFilter;

				int hr = graph.AddFilter(filter, string.Format("Renderer: {0}", VideoRendererType.MadVr));
				DsError.ThrowExceptionForHR(hr);
				var videoRenderer = filter as IMadVRDirect3D9Manager;

				if (videoRenderer == null)
					throw new Exception("Could not QueryInterface for the IMadVRDirect3D9Manager");

				/* Create a new EVR presenter */
				presenter = EvrPresenter.CreateNew();

				var presenterSettings = presenter.VideoPresenter as IEVRPresenterSettings;
				if (presenterSettings == null)
					throw new Exception("Could not QueryInterface for the IEVRPresenterSettings");

				presenterSettings.SetBufferCount(3);

				/* Initialize the MadVr renderer with the custom video presenter */
				//IntPtr d3D9Dev;
				//hr = presenterSettings.GetDirect3DDevice(out d3D9Dev);
				//DsError.ThrowExceptionForHR(hr);
				//hr = videoRenderer.UseTheseDevices(d3D9Dev, d3D9Dev, d3D9Dev);
				//DsError.ThrowExceptionForHR(hr);

				_settings = presenterSettings;

				/* Use our interop hWnd */
				IntPtr handle = GetDesktopWindow();//HwndHelper.Handle;

				/* QueryInterface the IMFVideoDisplayControl */
				var displayControl = presenter.VideoPresenter as IMFVideoDisplayControl;

				if (displayControl == null)
					throw new Exception("Could not QueryInterface the IMFVideoDisplayControl");

				/* Configure the presenter with our hWnd */
				hr = displayControl.SetVideoWindow(handle);
				DsError.ThrowExceptionForHR(hr);

				var filterConfig = filter as IEVRFilterConfig;

				if (filterConfig != null)
					filterConfig.SetNumberOfStreams(streamCount);
			}


			RegisterCustomAllocator(presenter);
			return filter;
		}

        /// <summary>
        /// Creates an instance of the EVR
        /// </summary>
        private IBaseFilter CreateEnhancedVideoRenderer(IGraphBuilder graph, int streamCount)
        {
            EvrPresenter presenter;
            IBaseFilter filter;

            lock(m_videoRendererInitLock)
            {
                var evr = new EnhancedVideoRenderer();
                filter = evr as IBaseFilter;

                int hr = graph.AddFilter(filter, string.Format("Renderer: {0}", VideoRendererType.EnhancedVideoRenderer));
                DsError.ThrowExceptionForHR(hr);

                /* QueryInterface for the IMFVideoRenderer */
                var videoRenderer = filter as IMFVideoRenderer;

                if (videoRenderer == null)
                    throw new Exception("Could not QueryInterface for the IMFVideoRenderer");

                /* Create a new EVR presenter */
                presenter = EvrPresenter.CreateNew();

                /* Initialize the EVR renderer with the custom video presenter */
                hr = videoRenderer.InitializeRenderer(null, presenter.VideoPresenter);
                DsError.ThrowExceptionForHR(hr);

                var presenterSettings = presenter.VideoPresenter as IEVRPresenterSettings;
                if (presenterSettings == null)
                    throw new Exception("Could not QueryInterface for the IEVRPresenterSettings");

                presenterSettings.SetBufferCount(3);

				_settings = presenterSettings;

                /* Use our interop hWnd */
                IntPtr handle = GetDesktopWindow();//HwndHelper.Handle;

                /* QueryInterface the IMFVideoDisplayControl */
                if (_displayControl != null) Marshal.ReleaseComObject(_displayControl);
                _displayControl = presenter.VideoPresenter as IMFVideoDisplayControl;

                if (_displayControl == null)
                    throw new Exception("Could not QueryInterface the IMFVideoDisplayControl");

                /* Configure the presenter with our hWnd */
                hr = _displayControl.SetVideoWindow(handle);
                DsError.ThrowExceptionForHR(hr);

            	hr = _settings.SetPixelShader(Resources.ToonShader);
				DsError.ThrowExceptionForHR(hr);

                var filterConfig = filter as IEVRFilterConfig;

                if (filterConfig != null)
                    filterConfig.SetNumberOfStreams(streamCount);
            }
            
            
            RegisterCustomAllocator(presenter);

            return filter;
        }

        /// <summary>
        /// Creates a new VMR9 renderer and configures it with an allocator
        /// </summary>
        /// <returns>An initialized DirectShow VMR9 renderer</returns>
        private IBaseFilter CreateVideoMixingRenderer9(IGraphBuilder graph, int streamCount)
        {
            var vmr9 = new VideoMixingRenderer9() as IBaseFilter;

            var filterConfig = vmr9 as IVMRFilterConfig9;

            if (filterConfig == null)
                throw new Exception("Could not query filter configuration.");

            /* We will only have one video stream connected to the filter */
            int hr = filterConfig.SetNumberOfStreams(streamCount);
            DsError.ThrowExceptionForHR(hr);

            if (_displayControlVMR != null) Marshal.ReleaseComObject(_displayControlVMR);
            _displayControlVMR = (IVideoWindow)vmr9;

            /* Setting the renderer to "Renderless" mode
             * sounds counter productive, but its what we
             * need to do for setting up a custom allocator */
            hr = filterConfig.SetRenderingMode(VMR9Mode.Renderless);
            DsError.ThrowExceptionForHR(hr);

            /* Query the allocator interface */
            var vmrSurfAllocNotify = vmr9 as IVMRSurfaceAllocatorNotify9;

            if (vmrSurfAllocNotify == null)
                throw new Exception("Could not query the VMR surface allocator.");

            var allocator = new Vmr9Allocator();

            /* We supply our custom allocator to the renderer */
            hr = vmrSurfAllocNotify.AdviseSurfaceAllocator(m_userId, allocator);
            DsError.ThrowExceptionForHR(hr);

            hr = allocator.AdviseNotify(vmrSurfAllocNotify);
            DsError.ThrowExceptionForHR(hr);

            RegisterCustomAllocator(allocator);

            hr = graph.AddFilter(vmr9, 
                                 string.Format("Renderer: {0}", VideoRendererType.VideoMixingRenderer9));

            DsError.ThrowExceptionForHR(hr);

            return vmr9;
        }

        /// <summary>
        /// Plays the media
        /// </summary>
        public virtual void Play()
        {
            VerifyAccess();

			//if (m_basicAudio != null)
			//{
			//    Balance = Balance;
			//    Volume = Volume;
			//}

			if (m_mediaControl != null)
			    m_mediaControl.Run();
        }

        /// <summary>
        /// Stops the media
        /// </summary>
        public virtual void Stop()
        {
            VerifyAccess();

            StopInternal();
        }

        /// <summary>
        /// Stops the media, but does not VerifyAccess() on
        /// the Dispatcher.  This can be used by destructors
        /// because it happens on another thread and our 
        /// DirectShow graph and COM run in MTA
        /// </summary>
        protected void StopInternal()
        {
            if (m_mediaControl != null)
            {
                m_mediaControl.Stop();
                FilterState filterState;
                m_mediaControl.GetState(0, out filterState);

                while (filterState != FilterState.Stopped)
                    m_mediaControl.GetState(0, out filterState);
            }
        }

        /// <summary>
        /// Closes the media and frees its resources
        /// </summary>
        public virtual void Close()
        {
            VerifyAccess();
            StopInternal();
            FreeResources();
        }

        /// <summary>
        /// Pauses the media
        /// </summary>
        public virtual void Pause()
        {
            VerifyAccess();

            if (m_mediaControl != null)
            {
                m_mediaControl.Pause();
            }
        }

        #region Event Invokes

        /// <summary>
        /// Invokes the MediaEnded event, notifying any subscriber that
        /// media has reached the end
        /// </summary>
        protected void InvokeMediaEnded(EventArgs e)
        {
            var mediaEndedHandler = MediaEnded;
            if (mediaEndedHandler != null)
                mediaEndedHandler();
        }

        /// <summary>
        /// Invokes the MediaOpened event, notifying any subscriber that
        /// media has successfully been opened
        /// </summary>
        protected void InvokeMediaOpened()
        {
            /* This is generally a good place to start
             * our polling timer */
            StartGraphPollTimer();

            var mediaOpenedHandler = MediaOpened;
            if (mediaOpenedHandler != null)
                mediaOpenedHandler();
        }

        /// <summary>
        /// Invokes the MediaClosed event, notifying any subscriber that
        /// the opened media has been closed
        /// </summary>
        protected void InvokeMediaClosed(EventArgs e)
        {
            StopGraphPollTimer();
        	HasVideo = false;
            var mediaClosedHandler = MediaClosed;
            if (mediaClosedHandler != null)
                mediaClosedHandler();
        }

        /// <summary>
        /// Invokes the FFTData event
        /// </summary>
        protected void InvokeNewFFT()
        {
            var ev = NewFFTData;
            if (ev != null)
                ev();
        }

        /// <summary>
        /// Invokes the NewAudioStream event
        /// </summary>
        protected void InvokeNewAudioStream()
        {
            var ev = NewAudioStream;
            if (ev != null)
                ev();
        }
        
        /// <summary>
        /// Invokes the NewSamplesNumber event
        /// </summary>
        protected void InvokeNewSamplesNumber()
        {
            var ev = NewSamplesNumber;
            if (ev != null)
                ev();
        }

        /// <summary>
        /// Invokes the MediaFailed event, notifying any subscriber that there was
        /// a media exception.
        /// </summary>
        /// <param name="e">The MediaFailedEventArgs contains the exception that caused this event to fire</param>
        protected void InvokeMediaFailed(MediaFailedEventArgs e)
        {
            var mediaFailedHandler = MediaFailed;
            if (mediaFailedHandler != null)
                mediaFailedHandler(this, e);
        }

        /// <summary>
        /// Invokes the NoSubtitleLoaded event
        /// </summary>
        /// <param name="e"></param>
        protected void InvokeNoSubtitleLoaded(EventArgs e)
        {
            var h = NoSubtitleLoaded;
            if (h != null)
                h(this, e);
        }

        /// <summary>
        /// Invokes the NewAllocatorFrame event, notifying any subscriber that new frame
        /// is ready to be presented.
        /// </summary>
        protected void InvokeNewAllocatorFrame()
        {
            var newAllocatorFrameHandler = NewAllocatorFrame;
            if (newAllocatorFrameHandler != null)
                newAllocatorFrameHandler();
        }

        /// <summary>
        /// Invokes the NewAllocatorSurface event, notifying any subscriber of a new surface
        /// </summary>
        /// <param name="pSurface">The COM pointer to the D3D surface</param>
        protected void InvokeNewAllocatorSurface(IntPtr pSurface)
        {
            var del = NewAllocatorSurface;
            if (del != null)
                del(this, pSurface);
        }

        #endregion
        
        #region Helper Methods
        /// <summary>
        /// Sets the natural pixel resolution the video in the graph
        /// </summary>
        /// <param name="renderer">The video renderer</param>
        public void SetNativePixelSizes(SIZE sz)
        {
            Size size = new Size(sz.cx, sz.cy);

            NaturalVideoHeight = (int)size.Height;
            NaturalVideoWidth = (int)size.Width;
        }

        /// <summary>
        /// Gets the video resolution of a pin on a renderer.
        /// </summary>
        /// <param name="renderer">The renderer to inspect</param>
        /// <param name="direction">The direction the pin is</param>
        /// <param name="pinIndex">The zero based index of the pin to inspect</param>
        /// <returns>If successful a video resolution is returned.  If not, a 0x0 size is returned</returns>
        protected static Size GetVideoSize(IBaseFilter renderer, PinDirection direction, int pinIndex)
        {
            var size = new Size();

            var mediaType = new AMMediaType();
            IPin pin = DsFindPin.ByDirection(renderer, direction, pinIndex);

            if (pin == null)
                goto done;
            
            int hr = pin.ConnectionMediaType(mediaType);
            
            if (hr != 0)
                goto done;

            /* Check to see if its a video media type */
            if (mediaType.formatType != FormatType.VideoInfo2 && 
                mediaType.formatType != FormatType.VideoInfo)
            {
                goto done;
            }

            var videoInfo = new VideoInfoHeader();

            /* Read the video info header struct from the native pointer */
            Marshal.PtrToStructure(mediaType.formatPtr, videoInfo);

            Rectangle rect = videoInfo.SrcRect.ToRectangle();
            size = new Size(rect.Width, rect.Height);

        done:
            DsUtils.FreeAMMediaType(mediaType);
            
            if(pin != null)
                Marshal.ReleaseComObject(pin);
            return size;
        }

        /// <summary>
        /// Removes all filters from a DirectShow graph
        /// </summary>
        /// <param name="graphBuilder">The DirectShow graph to remove all the filters from</param>
        protected static void RemoveAllFilters(IGraphBuilder graphBuilder)
        {
            if (graphBuilder == null)
                return;

            IEnumFilters enumFilters;

            /* The list of filters from the DirectShow graph */
            var filtersArray = new List<IBaseFilter>();

            if (graphBuilder == null)
                throw new ArgumentNullException("graphBuilder");

            /* Gets the filter enumerator from the graph */
            int hr = graphBuilder.EnumFilters(out enumFilters);
            DsError.ThrowExceptionForHR(hr);

            try
            {
                /* This array is filled with reference to a filter */
                var filters = new IBaseFilter[1];
                IntPtr fetched = IntPtr.Zero;

                /* Get reference to all the filters */
                while (enumFilters.Next(filters.Length, filters, fetched) == 0)
                {
                    /* Add the filter to our array */
                    filtersArray.Add(filters[0]);
                }
            }
            finally
            {
                /* Enum filters is a COM, so release that */
                Marshal.ReleaseComObject(enumFilters);
            }

            /* Loop over and release each COM */
            for (int i = 0; i < filtersArray.Count; i++)
            {
                graphBuilder.RemoveFilter(filtersArray[i]);
                while (Marshal.ReleaseComObject(filtersArray[i]) > 0)
                {}
            }
        }


        protected static IEnumerable<string> EnumAllFilters(IGraphBuilder graphBuilder)
        {
            IEnumFilters enumFilters;

            /* The list of filters from the DirectShow graph */
            var filtersArray = new List<IBaseFilter>();

            if (graphBuilder == null)
                throw new ArgumentNullException("graphBuilder");

            /* Gets the filter enumerator from the graph */
            int hr = graphBuilder.EnumFilters(out enumFilters);
            DsError.ThrowExceptionForHR(hr);

            try
            {
                /* This array is filled with reference to a filter */
                var filters = new IBaseFilter[1];
                IntPtr fetched = IntPtr.Zero;

                /* Get reference to all the filters */
                while (enumFilters.Next(filters.Length, filters, fetched) == 0)
                {
                    /* Add the filter to our array */
                    filtersArray.Add(filters[0]);
                }
            }
            finally
            {
                /* Enum filters is a COM, so release that */
                Marshal.ReleaseComObject(enumFilters);
            }

            for (int i = 0; i < filtersArray.Count; i++)
            {
                FilterInfo fi;
                filtersArray[i].QueryFilterInfo(out fi);
                yield return fi.achName;
            }

            /* Loop over and release each COM */
            //for (int i = 0; i < filtersArray.Count; i++)
            //{
            //    while (Marshal.ReleaseComObject(filtersArray[i]) > 0)
            //    { }
            //}
        }

		/// <summary>
		/// Adds a filter to a DirectShow graph based on it's name and filter category
		/// </summary>
		/// <param name="graphBuilder">The graph builder to add the filter to</param>
		/// <param name="deviceCategory">The category the filter belongs to</param>
		/// <param name="friendlyName">The friendly name of the filter</param>
		/// <returns>Reference to the IBaseFilter that was added to the graph or returns null if unsuccessful</returns>
		protected static IBaseFilter AddFilterByName(IGraphBuilder graphBuilder, Guid deviceCategory, string friendlyName)
		{
			var devices = DsDevice.GetDevicesOfCat(deviceCategory);

			var deviceList = (from d in devices
							  where d.Name == friendlyName
							  select d);
			DsDevice device = null;
			if (deviceList.Count() > 0)
				device = deviceList.Take(1).Single();

		    foreach (var item in deviceList)
		    {
                if (item != device)
                    item.Dispose();
		    }

			return AddFilterByDevice(graphBuilder, device);
		}

		protected static IBaseFilter AddFilterByDevicePath(IGraphBuilder graphBuilder, Guid deviceCategory, string devicePath)
		{
			var devices = DsDevice.GetDevicesOfCat(deviceCategory);

			var deviceList = (from d in devices
							  where d.DevicePath == devicePath
							  select d);
			DsDevice device = null;
			if (deviceList.Count() > 0)
				device = deviceList.Take(1).Single();

			return AddFilterByDevice(graphBuilder, device);
		}

		private static IBaseFilter AddFilterByDevice(IGraphBuilder graphBuilder, DsDevice device)
		{
			if (graphBuilder == null)
				throw new ArgumentNullException("graphBuilder");

			var filterGraph = graphBuilder as IFilterGraph2;

			if (filterGraph == null)
				return null;

			IBaseFilter filter = null;
			if (device != null)
			{
				int hr = filterGraph.AddSourceFilterForMoniker(device.Mon, null, device.Name, out filter);
				DsError.ThrowExceptionForHR(hr);
			}
			return filter;
		}

        /// <summary>
        /// Finds a pin that exists in a graph.
        /// </summary>
        /// <param name="majorOrMinorMediaType">The GUID of the major or minor type of the media</param>
        /// <param name="pinDirection">The direction of the pin - in/out</param>
        /// <param name="graph">The graph to search in</param>
        /// <returns>Returns null if the pin was not found, or if a pin is found, returns the first instance of it</returns>
        protected static IPin FindPinInGraphByMediaType(Guid majorOrMinorMediaType, PinDirection pinDirection, IGraphBuilder graph)
        {
            IEnumFilters enumFilters;
            
            /* Get the filter enum */
            graph.EnumFilters(out enumFilters);

            /* Init our vars */
            var filters = new IBaseFilter[1];
            var fetched = IntPtr.Zero;
            IPin pin = null;
            IEnumMediaTypes mediaTypesEnum = null;

            /* Loop over each filter in the graph */
            while (enumFilters.Next(1, filters, fetched) == 0)
            {
                var filter = filters[0];

                int i = 0;

                /* Loop over each pin in the filter */
                while ((pin = DsFindPin.ByDirection(filter, pinDirection, i)) != null)
                {
                    /* Get the pin enumerator */
                    pin.EnumMediaTypes(out mediaTypesEnum);
                    var mediaTypesFetched = IntPtr.Zero;
                    var mediaTypes = new AMMediaType[1];
                    
                    /* Enumerate the media types on the pin */
                    while (mediaTypesEnum.Next(1, mediaTypes, mediaTypesFetched) == 0)
                    {
                        /* See if the major or subtype meets our requirements */
                        if (mediaTypes[0].majorType.Equals(majorOrMinorMediaType) || mediaTypes[0].subType.Equals(majorOrMinorMediaType))
                        {
                            /* We found a match */
                            goto done;
                        }
                    }
                    i++;
                }
            }

        done:
            if (mediaTypesEnum != null)
            {
                mediaTypesEnum.Reset();
                Marshal.ReleaseComObject(mediaTypesEnum);
            }

            enumFilters.Reset();
            Marshal.ReleaseComObject(enumFilters);

            return pin;
        }

        #endregion

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string name)
		{
			var pc = PropertyChanged;
			if (pc != null)
			{
				pc(this, new PropertyChangedEventArgs(name));
			}
		}


        public void SetBand(int channel, int band, sbyte value)
        {
            if (_equalizer == null || _dspFilter == null) return;            

            int hr = _equalizer.set_Enabled(true);

            //int num = -1;
            //_dspFilter.get_PresetCount(ref num);

            //_amplify.get_Seperate(false);
            //_amplify.set_Enabled(true);

            if (channel == -1)
                hr = _equalizer.set_Seperate(false);
            else
                hr = _equalizer.set_Seperate(true);

            //_downmix.set_Enabled(true);
            if (channel == -1)
                for (byte i = 0; i < 10; i++)
                    hr = _equalizer.set_Band(i, (ushort)band, (sbyte)(value));
            else
                hr = _equalizer.set_Band((byte)channel, (ushort)band, (sbyte)(value));
        }
    }
}