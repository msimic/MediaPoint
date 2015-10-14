#region Usings
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DirectShowLib;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using MediaPoint.Common.Interfaces;
using MediaPoint.Common.MediaFoundation;
using MediaPoint.Common.MediaFoundation.Interop;
using MediaPoint.Common.MediaPlayers;
using Microsoft.Win32.SafeHandles;
using MediaPoint.Common.Helpers;
using MediaPoint.Common.Interfaces.LavAudio;

#endregion

namespace MediaPoint.Common.DirectShow.MediaPlayers
{
	/// <summary>
	/// The MediaUriPlayer plays media files from a given Uri.
	/// </summary>
	public class MediaUriPlayer : MediaSeekingPlayer
	{
		/// <summary>
		/// The name of the default audio render.  This is the
		/// same on all versions of windows
		/// </summary>
        private static readonly string DEFAULT_AUDIO_RENDERER_NAME = SharpDX.DirectSound.DirectSound.GetDevices().Count == 0 ? "" : SharpDX.DirectSound.DirectSound.GetDevices()[0].Description;

		/// <summary>
		/// Set the default audio renderer property backing
		/// </summary>
		private string m_audioRenderer = DEFAULT_AUDIO_RENDERER_NAME;

#if DEBUG
		/// <summary>
		/// Used to view the graph in graphedit
		/// </summary>
		private DsROTEntry m_dsRotEntry;
#endif

		/// <summary>
		/// The DirectShow graph interface.  In this example
		/// We keep reference to this so we can dispose 
		/// of it later.
		/// </summary>
		//private IGraphBuilder m_graph;

		/// <summary>
		/// The media Uri
		/// </summary>
		private Uri m_sourceUri;

		/// <summary>
		/// Gets or sets the Uri source of the media
		/// </summary>
		public Uri Source
		{
			get
			{
				VerifyAccess();
				return m_sourceUri;
			}
			set
			{
				VerifyAccess();
				m_sourceUri = value;
				OpenSource();
			}
		}

		public void SetAdvancedSubtitleConfig(string sett)
		{
			Dispatcher.BeginInvoke((Action)(() =>
			{
				var set = _splitterSettings as ILAVSplitterSettings;
				int ret = set.SetRuntimeConfig(true);
				ret = set.SetAdvancedSubtitleConfig(sett);
			}));
		}

		/// <summary>
		/// The renderer type to use when
		/// rendering video
		/// </summary>
		public VideoRendererType VideoRenderer
		{
			get;
			set;
		}

		/// <summary>
		/// The name of the audio renderer device
		/// </summary>
		public string AudioRenderer
		{
			get
			{
				VerifyAccess();
				return m_audioRenderer;
			}
			set
			{
				VerifyAccess();

				if (string.IsNullOrEmpty(value))
				{
					value = DEFAULT_AUDIO_RENDERER_NAME;
				}

				m_audioRenderer = value;
			}
		}

		/// <summary>
		/// Gets or sets if the media should play in loop
		/// or if it should just stop when the media is complete
		/// </summary>
		public bool Loop { get; set; }

		/// <summary>
		/// Is ran everytime a new media event occurs on the graph
		/// </summary>
		/// <param name="code">The Event code that occured</param>
		/// <param name="lparam1">The first event parameter sent by the graph</param>
		/// <param name="lparam2">The second event parameter sent by the graph</param>
		protected override void OnMediaEvent(EventCode code, IntPtr lparam1, IntPtr lparam2)
		{
			Console.WriteLine(code.ToString());
			if (Loop)
			{
				switch (code)
				{
					case EventCode.Complete:
						MediaPosition = 0;
						break;
				}
			}
			else
				/* Only run the base when we don't loop
				 * otherwise the default behavior is to
				 * fire a media ended event */
				base.OnMediaEvent(code, lparam1, lparam2);
		}

		public static IPin ByDirection(IBaseFilter vSource, PinDirection vDir, int iIndex)
		{
			IPin result = null;
			IPin[] array = new IPin[1];
			if (vSource == null)
			{
				return null;
			}
			IEnumPins enumPins;
			int hr = vSource.EnumPins(out enumPins);
			DsError.ThrowExceptionForHR(hr);
			try
			{
				while (enumPins.Next(1, array, IntPtr.Zero) == 0)
				{
					PinDirection pinDirection;
					hr = array[0].QueryDirection(out pinDirection);
					DsError.ThrowExceptionForHR(hr);
					if (pinDirection == vDir)
					{
						if (iIndex == 0)
						{
							result = array[0];
							break;
						}
						iIndex--;
					}
					//Marshal.ReleaseComObject(array[0]);
				}
			}
			finally
			{
				//Marshal.ReleaseComObject(enumPins);
			}
			return result;
		}

		/// <summary>
		/// Opens the media by initializing the DirectShow graph
        //
        //                                          +----------------+          +----------------+       +-----------------------+
        //     +---------------------+              | LavVideo       |          | VobSub         |       | EVR+CustomPresenter   |
        //     | LavSplitterSource   |              |----------------|          |----------------|       |-----------------------|
        //     +---------------------+              |                |          |                |       |                       |
        //     |                     |              |                |          |                |       |    VIDEO              |
        //     |             video  +|->+---------+<-+ IN       OUT +->+------+<-+ VID_IN   OUT +-> +-+ <-+   RENDERER           |
        //     |                     |              +----------------+          |                |       |                       |
        //     |             audio  +|->+------+                                |                |       +-----------------------+
        //     |                     |         |    +----------------+      +-+<-+ TXT_IN        |
        //     |          subtitle  +|->+--+   |    | LavAudio       |      |   |                |
        //     +---------------------+    |   |    |----------------|      |   +----------------+       +-----------------------+
        //                                 |   |    |                |      |                            | DShow output device   |
        //                                 |   |    |                |     xxx                           |-----------------------|
        //                                 |   +--+<-+ IN       OUT +->+--x | x-----------------------+  |                       |
        //                                 |        +----------------+      |                            |    AUDIO              |
        //                                 |                                |                           <-+   RENDERER           |
        //                                 |                                |                            |                       |
        //                                 +--------------------------------+                            +-----------------------+
        // 								
		/// </summary>
//        protected virtual void OpenSource()
//        {
//            /* Make sure we clean up any remaining mess */
//            //if (m_graph != null) RemoveAllFilters(m_graph);
//            FreeResources();

//            if (m_sourceUri == null)
//                return;

//            string fileSource = m_sourceUri.OriginalString;

//            if (string.IsNullOrEmpty(fileSource))
//                return;
            
//            try
//            {
//                if (m_graph != null) Marshal.ReleaseComObject(m_graph);

//                /* Creates the GraphBuilder COM object */
//                m_graph = new FilterGraphNoThread() as IGraphBuilder;

//                if (m_graph == null)
//                    throw new Exception("Could not create a graph");

//                /* Add our prefered audio renderer */
//                var audioRenderer = InsertAudioRenderer(AudioRenderer);
//                if (_audioRenderer != null) Marshal.ReleaseComObject(_audioRenderer);
//                _audioRenderer = audioRenderer;

//                if ((System.Environment.OSVersion.Platform == PlatformID.Win32NT &&
//                (System.Environment.OSVersion.Version.Major == 5)))
//                    VideoRenderer = VideoRendererType.VideoMixingRenderer9;

//                IBaseFilter renderer = CreateVideoRenderer(VideoRenderer, m_graph, 2);
//                if (_renderer != null) Marshal.ReleaseComObject(_renderer);
//                _renderer = renderer;
//                //if (_renderer != null)
//                //    m_graph.AddFilter((IBaseFilter)_renderer, "Renderer");

//                var filterGraph = m_graph as IFilterGraph2;

//                if (filterGraph == null)
//                    throw new Exception("Could not QueryInterface for the IFilterGraph2");

//                ILAVAudioSettings lavAudioSettings;
//                ILAVAudioStatus lavStatus;
//                IBaseFilter audioDecoder = FilterProvider.GetAudioFilter(out lavAudioSettings, out lavStatus);
//                if (audioDecoder != null)
//                {
//                    if (_audio != null) Marshal.ReleaseComObject(_audio);
//                    _audio = audioDecoder;
//                    lavAudioSettings.SetRuntimeConfig(true);
//                    m_graph.AddFilter((IBaseFilter)_audio, "LavAudio");
//                }

//                ILAVSplitterSettings splitterSettings;
//                IFileSourceFilter splitter = FilterProvider.GetSplitterSource(out splitterSettings);
//                //IBaseFilter splitter = FilterProvider.GetSplitter(out splitterSettings);

//                if (splitter != null)
//                {
//                    splitter.Load(fileSource, null);
//                    if (_splitter != null) Marshal.ReleaseComObject(_splitter);
//                    _splitter = splitter;
//                    splitterSettings.SetRuntimeConfig(true);
//                    m_graph.AddFilter((IBaseFilter)splitter, "LavSplitter");
//                }

//                int hr = 0;


//                /* We will want to enum all the pins on the source filter */
//                IEnumPins pinEnum;

//                hr = ((IBaseFilter)splitter).EnumPins(out pinEnum);
//                DsError.ThrowExceptionForHR(hr);

//                IntPtr fetched = IntPtr.Zero;
//                IPin[] pins = { null };

//                /* Counter for how many pins successfully rendered */


//                if (VideoRenderer == VideoRendererType.VideoMixingRenderer9)
//                {
//                    var mixer = renderer as IVMRMixerControl9;

//                    if (mixer != null)
//                    {
//                        VMR9MixerPrefs dwPrefs;
//                        mixer.GetMixingPrefs(out dwPrefs);
//                        dwPrefs &= ~VMR9MixerPrefs.RenderTargetMask;
//                        dwPrefs |= VMR9MixerPrefs.RenderTargetRGB;
//                        //mixer.SetMixingPrefs(dwPrefs);
//                    }
//                }

//                // Test using FFDShow Video Decoder Filter
//                ILAVVideoSettings lavVideoSettings;
//                IBaseFilter lavVideo = FilterProvider.GetVideoFilter(out lavVideoSettings);
//                if (_video != null) Marshal.ReleaseComObject(_video);
//                _video = lavVideo;

//                IBaseFilter vobSub = FilterProvider.GetVobSubFilter();

//                if (vobSub != null)
//                {
//                    m_graph.AddFilter(vobSub, "VobSub");
//                    IDirectVobSub vss = vobSub as IDirectVobSub;
//                    if (_vobsub != null) Marshal.ReleaseComObject(_vobsub);
//                    _vobsub = vss;
//                    InitSubSettings();
//                }

//                if (lavVideo != null)
//                {
//                    lavVideoSettings.SetRuntimeConfig(true);
//                    m_graph.AddFilter(lavVideo, "LavVideo");
//                }

//                int ret;

//                IBaseFilter dcDsp = FilterProvider.GetDCDSPFilter();
//                if (dcDsp != null)
//                {
//                    _dspFilter = (IDCDSPFilterInterface)dcDsp;

//                    //hr = i.set_PCMDataBeforeMainDSP(true);
//                    hr = m_graph.AddFilter((IBaseFilter)dcDsp, "VobSub");

//                    ret = m_graph.Connect(DsFindPin.ByName((IBaseFilter)splitter, "Audio"), DsFindPin.ByDirection(audioDecoder, PinDirection.Input, 0));
//                    ret = m_graph.Connect(DsFindPin.ByDirection((IBaseFilter)audioDecoder, PinDirection.Output, 0), DsFindPin.ByDirection(_dspFilter, PinDirection.Input, 0));
//                    ret = m_graph.Connect(DsFindPin.ByDirection((IBaseFilter)_dspFilter, PinDirection.Output, 0), DsFindPin.ByDirection(audioRenderer, PinDirection.Input, 0));

//                    //bool d = false;
//                    //int delay = 0;
//                    //hr = i.get_EnableDelay(ref d);
//                    int cnt = 0;
//                    object intf = null;
//                    //hr = i.set_EnableDelay(true);
//                    //hr = i.set_Delay(0);
//                    hr = _dspFilter.set_AddFilter(0, TDCFilterType.ftEqualizer);
//                    hr = _dspFilter.get_FilterCount(ref cnt);
//                    hr = _dspFilter.get_FilterInterface(0, out intf);
//                    _equalizer = (IDCEqualizer)intf;
//                    hr = _dspFilter.set_AddFilter(0, TDCFilterType.ftDownMix);
//                    hr = _dspFilter.get_FilterInterface(0, out intf);
//                    _downmix = (IDCDownMix)intf;
//                    hr = _dspFilter.set_AddFilter(0, TDCFilterType.ftAmplify);
//                    hr = _dspFilter.get_FilterInterface(0, out intf);
//                    _amplify = (IDCAmplify)intf;

//                    _equalizer.set_Seperate(false);
//                }

//                bool subconnected = false;
//                ret = m_graph.Connect(DsFindPin.ByName((IBaseFilter)splitter, "Video"), DsFindPin.ByDirection(lavVideo, PinDirection.Input, 0));
//                ret = m_graph.Connect(DsFindPin.ByDirection((IBaseFilter)lavVideo, PinDirection.Output, 0), DsFindPin.ByDirection(vobSub, PinDirection.Input, 0));
//                if (ret == 0)
//                {
//                    int lc;
//                    ((IDirectVobSub)vobSub).get_LanguageCount(out lc);
//                    subconnected = (lc != 0);
//                    IPin pn = DsFindPin.ByName((IBaseFilter)splitter, "Subtitle");
//                    if (pn != null)
//                    {
//                        ret = m_graph.Connect(pn, DsFindPin.ByDirection(vobSub, PinDirection.Input, 1));
//                        ((IDirectVobSub)vobSub).get_LanguageCount(out lc);
//                        subconnected = (lc != 0);
//                    }
//                    ret = m_graph.Connect(DsFindPin.ByDirection(vobSub, PinDirection.Output, 0),
//                                          DsFindPin.ByDirection(renderer, PinDirection.Input, 0));
//                }
//                else
//                {
//                    ret = m_graph.Connect(DsFindPin.ByDirection(lavVideo, PinDirection.Output, 0),
//                                      DsFindPin.ByDirection(renderer, PinDirection.Input, 0));
//                }

//                /* Loop over each pin of the source filter */
//                while (pinEnum.Next(pins.Length, pins, fetched) == 0)
//                {
//                    IPin cTo;
//                    pins[0].ConnectedTo(out cTo);
//                    if (cTo == null)
//                    {
//                        // this should not happen if the filtegraph is manually connected in a good manner
//                        ret = filterGraph.RenderEx(pins[0], AMRenderExFlags.RenderToExistingRenderers, IntPtr.Zero);
//                    }
//                    else
//                    {
//                        Marshal.ReleaseComObject(cTo);
//                    }
//                    Marshal.ReleaseComObject(pins[0]);
//                }

//                if (lavVideoSettings != null)
//                {
//                    if (lavVideoSettings.CheckHWAccelSupport(LAVHWAccel.HWAccel_CUDA) != 0)
//                    {
//                        ret = lavVideoSettings.SetHWAccel(LAVHWAccel.HWAccel_CUDA);
//                    }
//                    else if (lavVideoSettings.CheckHWAccelSupport(LAVHWAccel.HWAccel_QuickSync) != 0)
//                    {
//                        ret = lavVideoSettings.SetHWAccel(LAVHWAccel.HWAccel_QuickSync);
//                    }
//                    else if (lavVideoSettings.CheckHWAccelSupport(LAVHWAccel.HWAccel_DXVA2Native) != 0)
//                    {
//                        ret = lavVideoSettings.SetHWAccel(LAVHWAccel.HWAccel_DXVA2Native);
//                    }
//                    else if (lavVideoSettings.CheckHWAccelSupport(LAVHWAccel.HWAccel_DXVA2) != 0)
//                    {
//                        ret = lavVideoSettings.SetHWAccel(LAVHWAccel.HWAccel_DXVA2);
//                    }
//                    else if (lavVideoSettings.CheckHWAccelSupport(LAVHWAccel.HWAccel_DXVA2CopyBack) != 0)
//                    {
//                        ret = lavVideoSettings.SetHWAccel(LAVHWAccel.HWAccel_DXVA2CopyBack);
//                    }
//                }

//                //hr = m_graph.RenderFile(fileSource, null);

//                Marshal.ReleaseComObject(pinEnum);

//                IAMStreamSelect selector = splitter as IAMStreamSelect;
//                int numstreams;
//                selector.Count(out numstreams);
//                AMMediaType mt;
//                AMStreamSelectInfoFlags fl;
//                SubtitleStreams.Clear();
//                VideoStreams.Clear();
//                AudioStreams.Clear();
//                for (int i = 0; i < numstreams; i++)
//                {
//                    int lcid;
//                    int group;
//                    string name;
//                    object o, o2;
//                    selector.Info(i, out mt, out fl, out lcid, out group, out name, out o, out o2);
//                    switch (group)
//                    {
//                        case 0:
//                            VideoStreams.Add(name);
//                            break;
//                        case 1:
//                            AudioStreams.Add(name);
//                            break;
//                        case 2:
//                            SubtitleStreams.Add(name);
//                            break;
//                    }

//                    if (o != null) Marshal.ReleaseComObject(o);
//                    if (o2 != null) Marshal.ReleaseComObject(o2);
//                }

//                OnPropertyChanged("SubtitleStreams");
//                OnPropertyChanged("VideoStreams");
//                OnPropertyChanged("AudioStreams");

//                //Marshal.ReleaseComObject(splitter);


//                /* Configure the graph in the base class */
//                SetupFilterGraph(m_graph);

//#if DEBUG
//                /* Adds the GB to the ROT so we can view
//* it in graphedit */
//                m_dsRotEntry = new DsROTEntry(m_graph);
//#endif

//                //if (_splitterSettings != null)
//                //{
//                // Marshal.ReleaseComObject(_splitterSettings);
//                // _splitterSettings = null;
//                //}
//                if (_splitterSettings != null) Marshal.ReleaseComObject(_splitterSettings);
//                _splitterSettings = (ILAVSplitterSettings)splitter;
//                //ret = _splitterSettings.SetRuntimeConfig(true);
//                //if (ret != 0)
//                // throw new Exception("Could not set SetRuntimeConfig to true");

//                //string sss = "*:*";

//                //LAVSubtitleMode mode = LAVSubtitleMode.LAVSubtitleMode_NoSubs;
//                //mode = _splitterSettings.GetSubtitleMode();
//                //if (mode != LAVSubtitleMode.LAVSubtitleMode_Default)
//                // throw new Exception("Could not set GetAdvancedSubtitleConfige");

//                //ret = _splitterSettings.SetSubtitleMode(LAVSubtitleMode.LAVSubtitleMode_Advanced);
//                //if (ret != 1)
//                // throw new Exception("Could not set SetAdvancedSubtitleConfige");

//                //ret = _splitterSettings.SetAdvancedSubtitleConfig(sss);
//                //if (ret != 1)
//                // throw new Exception("Could not set SetAdvancedSubtitleConfige");

//                //sss = "";
//                //ret = _splitterSettings.GetAdvancedSubtitleConfig(out sss);
//                //if (ret != 0)
//                // throw new Exception("Could not set GetAdvancedSubtitleConfige");

//                //IPin sub = DsFindPin.ByDirection((IBaseFilter)splitter, PinDirection.Output, 2);
//                //PinInfo pi;
//                //sub.QueryPinInfo(out pi);
//                SIZE a, b;
//                if ((_displayControl).GetNativeVideoSize(out a, out b) == 0)
//                {
//                    if (a.cx > 0 && a.cy > 0)
//                    {
//                        HasVideo = true;
//                        SetNativePixelSizes(a);
//                    }
//                }

//                if (!subconnected)
//                {
//                    InvokeNoSubtitleLoaded(new EventArgs());
//                }
//                else
//                {
//                    InitSubSettings();
//                }
//                /* Sets the NaturalVideoWidth/Height */
//                //SetNativePixelSizes(renderer);

//                //InvokeMediaFailed(new MediaFailedEventArgs(sss, null));
//            }
//            catch (Exception ex)
//            {
//                /* This exection will happen usually if the media does
//                * not exist or could not open due to not having the
//                * proper filters installed */
//                FreeResources();

//                /* Fire our failed event */
//                InvokeMediaFailed(new MediaFailedEventArgs(ex.Message, ex));
//            }
//            finally
//            {
//                string filters = string.Join(Environment.NewLine, EnumAllFilters(m_graph).ToArray());
//                System.Diagnostics.Debug.WriteLine(filters);
//            }
//            InvokeMediaOpened();
//        }
        protected virtual void OpenSource()
        {
            _eqEnabled = false;
            //if (m_graph != null)
            //{
            //    //RemoveAllFilters(m_graph);
            //    Marshal.ReleaseComObject(m_graph);
            //}

            /* Make sure we clean up any remaining mess */
            FreeResources();

            if (m_sourceUri == null)
                return;

            string fileSource = m_sourceUri.OriginalString;

            if (string.IsNullOrEmpty(fileSource))
                return;

            try
            {
                int hr = 0;

                /* Creates the GraphBuilder COM object */
                m_graph = new FilterGraphNoThread() as IGraphBuilder;

                if (_displayControl != null)
                {
                    Marshal.ReleaseComObject(_displayControl);
                    _displayControl = null;
                }

                if (_displayControlVMR != null)
                {
                    Marshal.ReleaseComObject(_displayControlVMR);
                    _displayControlVMR = null;
                }

                if (m_graph == null)
                    throw new Exception("Could not create a graph");

                var filterGraph = m_graph as IFilterGraph2;

                var flt = EnumAllFilters(m_graph).ToList();

                if (filterGraph == null)
                    throw new Exception("Could not QueryInterface for the IFilterGraph2");

                /* Add our prefered audio renderer */
                var audioRenderer = InsertAudioRenderer(AudioRenderer);
                if (audioRenderer != null)
                {
                    if (_audioRenderer != null) Marshal.ReleaseComObject(_audioRenderer);
                    _audioRenderer = audioRenderer;
                }

                if ((System.Environment.OSVersion.Platform == PlatformID.Win32NT &&
                    (System.Environment.OSVersion.Version.Major == 5)))
                    VideoRenderer = VideoRendererType.VideoMixingRenderer9;

                if (_presenterSettings != null) Marshal.ReleaseComObject(_presenterSettings);
                if (_renderer != null) Marshal.ReleaseComObject(_renderer);

                IBaseFilter renderer = InsertVideoRenderer(VideoRenderer, m_graph, 1);
                _renderer = renderer;
                
                ILAVAudioSettings lavAudioSettings;
                ILAVAudioStatus lavStatus;
                IBaseFilter audioDecoder = FilterProvider.GetAudioFilter(out lavAudioSettings, out lavStatus);
                if (audioDecoder != null)
                {
                    if (_audio != null) Marshal.ReleaseComObject(_audio);
                    _audio = audioDecoder;
                    _audioStatus = lavStatus;
                    _audioSettings = lavAudioSettings;

                    hr = (int)lavAudioSettings.SetRuntimeConfig(true);
                    hr = m_graph.AddFilter((IBaseFilter)audioDecoder, "LavAudio");
                    DsError.ThrowExceptionForHR(hr);
#if DEBUG
                    hr = (int)lavAudioSettings.SetTrayIcon(true);
#endif
                }

                ILAVSplitterSettings splitterSettings;
                IFileSourceFilter splitter = FilterProvider.GetSplitterSource(out splitterSettings);

                if (splitter != null)
                {
                    if (_splitter != null) Marshal.ReleaseComObject(_splitter);
                    _splitter = splitter;
                    
                    _splitterSettings = (ILAVSplitterSettings)splitterSettings;
                
                    hr = splitterSettings.SetRuntimeConfig(true);
                    hr = splitter.Load(fileSource, null);
                    if (hr != 0)
                    {
                        throw new Exception("Playback of this file is not supported!");
                    }
                    hr = m_graph.AddFilter((IBaseFilter)splitter, "LavSplitter");
                    DsError.ThrowExceptionForHR(hr);
                }

                IEnumPins pinEnum;
                hr = ((IBaseFilter)splitter).EnumPins(out pinEnum);
                DsError.ThrowExceptionForHR(hr);

                IntPtr fetched = IntPtr.Zero;
                IPin[] pins = { null };

                if (VideoRenderer == VideoRendererType.VideoMixingRenderer9)
                {
                    var mixer = _renderer as IVMRMixerControl9;

                    if (mixer != null)
                    {
                        VMR9MixerPrefs dwPrefs;
                        mixer.GetMixingPrefs(out dwPrefs);
                        dwPrefs &= ~VMR9MixerPrefs.RenderTargetMask;
                        dwPrefs |= VMR9MixerPrefs.RenderTargetRGB;
                        mixer.SetMixingPrefs(dwPrefs);
                    }
                }

                ILAVVideoSettings lavVideoSettings;
                IBaseFilter lavVideo = FilterProvider.GetVideoFilter(out lavVideoSettings);

                if (lavVideo != null)
                {
                    if (_video != null) Marshal.ReleaseComObject(_video);
                    _video = lavVideo;
                    
                    if (lavVideoSettings != null)
                    {
                        _videoSettings = lavVideoSettings;
                    
                        lavVideoSettings.SetRuntimeConfig(true);
                        hr = lavVideoSettings.SetHWAccel(LAVHWAccel.HWAccel_None);

                        // check for best acceleration available
                        //if (lavVideoSettings.CheckHWAccelSupport(LAVHWAccel.HWAccel_CUDA) != 0)
                        //{
                        //    hr = lavVideoSettings.SetHWAccel(LAVHWAccel.HWAccel_CUDA);
                        //    hr = lavVideoSettings.SetHWAccelResolutionFlags(LAVHWResFlag.SD | LAVHWResFlag.HD | LAVHWResFlag.UHD);
                        //}
                        //else if (lavVideoSettings.CheckHWAccelSupport(LAVHWAccel.HWAccel_QuickSync) != 0)
                        //{
                        //    hr = lavVideoSettings.SetHWAccel(LAVHWAccel.HWAccel_QuickSync);
                        //    hr = lavVideoSettings.SetHWAccelResolutionFlags(LAVHWResFlag.SD | LAVHWResFlag.HD | LAVHWResFlag.UHD);
                        //}
                        //else
                        if (lavVideoSettings.CheckHWAccelSupport(LAVHWAccel.HWAccel_DXVA2Native) != 0)
                        {
                            hr = lavVideoSettings.SetHWAccel(LAVHWAccel.HWAccel_DXVA2Native);
                            hr = lavVideoSettings.SetHWAccelResolutionFlags(LAVHWResFlag.SD | LAVHWResFlag.HD | LAVHWResFlag.UHD);
                        }
                        //else
                        //if (lavVideoSettings.CheckHWAccelSupport(LAVHWAccel.HWAccel_DXVA2CopyBack) != 0)
                        //{
                        //    hr = lavVideoSettings.SetHWAccel(LAVHWAccel.HWAccel_DXVA2CopyBack);
                        //    hr = lavVideoSettings.SetHWAccelResolutionFlags(LAVHWResFlag.SD | LAVHWResFlag.HD | LAVHWResFlag.UHD);
                        //}

#if DEBUG
                        hr = lavVideoSettings.SetTrayIcon(true);
#endif
                    }

                    hr = m_graph.AddFilter(_video, "LavVideo");
                    DsError.ThrowExceptionForHR(hr);
                }

                IBaseFilter vobSub = FilterProvider.GetVobSubFilter();

                if (vobSub != null)
                {
                    try
                    {
                        hr = m_graph.AddFilter(vobSub, "VobSub");
                        DsError.ThrowExceptionForHR(hr);
                        IDirectVobSub vss = vobSub as IDirectVobSub;
                        if (_vobsub != null) Marshal.ReleaseComObject(_vobsub);
                        _vobsub = vss;
                        InitSubSettings();
                    }
                    catch { }
                }

                hr = m_graph.Connect(DsFindPin.ByName((IBaseFilter)splitter, "Audio"), DsFindPin.ByDirection(_audio, PinDirection.Input, 0));
                if (hr == 0)
                    HasAudio = true;
                else
                    HasAudio = false;


                IBaseFilter dcDsp = FilterProvider.GetDCDSPFilter();
                if (dcDsp != null)
                {
                    if (_dspFilter != null) Marshal.ReleaseComObject(_dspFilter);
                    _dspFilter = (IDCDSPFilterInterface)dcDsp;

                    if (HasAudio)
                    {
                        hr = m_graph.AddFilter((IBaseFilter)_dspFilter, "AudioProcessor");
                        hr = _dspFilter.set_EnableBitrateConversionBeforeDSP(true);
                        hr = ((IDCDSPFilterVisualInterface)_dspFilter).set_VISafterDSP(true);
                        hr = m_graph.Connect(DsFindPin.ByDirection((IBaseFilter)_audio, PinDirection.Output, 0), DsFindPin.ByDirection(_dspFilter, PinDirection.Input, 0));
                        DsError.ThrowExceptionForHR(hr);
                        hr = m_graph.Connect(DsFindPin.ByDirection((IBaseFilter)_dspFilter, PinDirection.Output, 0), DsFindPin.ByDirection(_audioRenderer, PinDirection.Input, 0));

                        var cb = new AudioCallback(this);
                        hr = _dspFilter.set_CallBackPCM(cb);

                        object intf = null;
                        hr = _dspFilter.set_AddFilter(0, TDCFilterType.ftEqualizer);
                        hr = _dspFilter.get_FilterInterface(0, out intf);
                        _equalizer = (IDCEqualizer)intf;
                        _equalizer.set_Seperate(false);
                    }
                }
                else
                {
                    if (HasAudio)
                    {
                        hr = m_graph.Connect(DsFindPin.ByDirection((IBaseFilter)_audio, PinDirection.Output, 0), DsFindPin.ByDirection(_audioRenderer, PinDirection.Input, 0));
                    }
                }

                bool subconnected = false;

                hr = m_graph.Connect(DsFindPin.ByName((IBaseFilter)_splitter, "Video"), DsFindPin.ByDirection(_video, PinDirection.Input, 0));
                if (hr == 0)
                    HasVideo = true;
                else
                    HasVideo = false;

                if (HasVideo)
                {
                    hr = m_graph.Connect(DsFindPin.ByDirection((IBaseFilter)_video, PinDirection.Output, 0), DsFindPin.ByDirection(vobSub, PinDirection.Input, 0));
                    DsError.ThrowExceptionForHR(hr);
                    if (hr == 0)
                    {
                        int lc;
                        ((IDirectVobSub)vobSub).get_LanguageCount(out lc);
                        subconnected = (lc != 0);
                        IPin pn = DsFindPin.ByName((IBaseFilter)splitter, "Subtitle");
                        if (pn != null)
                        {
                            hr = m_graph.Connect(pn, DsFindPin.ByDirection(vobSub, PinDirection.Input, 1));
                            ((IDirectVobSub)vobSub).get_LanguageCount(out lc);
                            subconnected = (lc != 0);
                        }
                        hr = m_graph.Connect(DsFindPin.ByDirection(vobSub, PinDirection.Output, 0),
                                              DsFindPin.ByDirection(_renderer, PinDirection.Input, 0));
                    }
                    else
                    {
                        if (_vobsub != null) Marshal.ReleaseComObject(_vobsub);
                        _vobsub = null; 
                        hr = m_graph.Connect(DsFindPin.ByDirection(_video, PinDirection.Output, 0),
                                          DsFindPin.ByDirection(_renderer, PinDirection.Input, 0));

                    }
                }

                /* Loop over each pin of the source filter */
                while (pinEnum.Next(pins.Length, pins, fetched) == 0)
                {
                    IPin cTo;
                    pins[0].ConnectedTo(out cTo);
                    if (cTo == null)
                    {
                        // this should not happen if the filtegraph is manually connected in a good manner
                        hr = filterGraph.RenderEx(pins[0], AMRenderExFlags.RenderToExistingRenderers, IntPtr.Zero);
                    }
                    else
                    {
                        Marshal.ReleaseComObject(cTo);
                    }
                    Marshal.ReleaseComObject(pins[0]);
                }

                Marshal.ReleaseComObject(pinEnum);

                var selector = splitter as IAMStreamSelect;
                int numstreams;
                selector.Count(out numstreams);
                AMMediaType mt;
                AMStreamSelectInfoFlags fl;
                SubtitleStreams.Clear();
                VideoStreams.Clear();
                AudioStreams.Clear();
                for (int i = 0; i < numstreams; i++)
                {
                    int lcid;
                    int group;
                    string name;
                    object o, o2;
                    selector.Info(i, out mt, out fl, out lcid, out group, out name, out o, out o2);
                    switch (group)
                    {
                        case 0:
                            VideoStreams.Add(name);
                            break;
                        case 1:
                            AudioStreams.Add(name);
                            break;
                        case 2:
                            SubtitleStreams.Add(name);
                            break;
                    }

                    if (o != null) Marshal.ReleaseComObject(o);
                    if (o2 != null) Marshal.ReleaseComObject(o2);
                }

                OnPropertyChanged("SubtitleStreams");
                OnPropertyChanged("VideoStreams");
                OnPropertyChanged("AudioStreams");

                /* Configure the graph in the base class */
                SetupFilterGraph(m_graph);

#if DEBUG
                /* Adds the GB to the ROT so we can view
                 * it in graphedit */
                m_dsRotEntry = new DsROTEntry(m_graph);
#endif

                SIZE a, b;
                if (HasVideo && _displayControl != null && (_displayControl).GetNativeVideoSize(out a, out b) == 0)
                {
                    var sz = MediaPlayerBase.GetVideoSize(_renderer, PinDirection.Input, 0);
                    if (a.cx > 0 && a.cy > 0)
                    {
                        SetNativePixelSizes(a);
                    }
                }

                if (!subconnected)
                {
                    InvokeNoSubtitleLoaded(new EventArgs());
                }
                else
                {
                    InitSubSettings();
                }

            }
            catch (Exception ex)
            {
                /* This exection will happen usually if the media does
                 * not exist or could not open due to not having the
                 * proper filters installed */
                FreeResources();

                /* Fire our failed event */
                InvokeMediaFailed(new MediaFailedEventArgs(ex.Message, ex));
            }

            InvokeMediaOpened();
        }


        /// <summary>
		/// Inserts the audio renderer by the name of
		/// the audio renderer that is passed
		/// </summary>
		protected virtual IBaseFilter InsertAudioRenderer(string audioDeviceName)
		{
			if (m_graph == null)
				return null;

            var dv = SharpDX.DirectSound.DirectSound.GetDevices().FirstOrDefault(d => d.Description == audioDeviceName);

            if (dv != null)
            {
                return AddFilterByDsGuid(m_graph, FilterCategory.AudioRendererCategory, dv.DriverGuid);
            }

            return null;
		}

		/// <summary>
		/// Frees all unmanaged memory and resets the object back
		/// to its initial state
		/// </summary>
		protected override void FreeResources()
		{
#if DEBUG
			/* Remove us from the ROT */
			if (m_dsRotEntry != null)
			{
				m_dsRotEntry.Dispose();
				m_dsRotEntry = null;
			}
#endif

			/* We run the StopInternal() to avoid any 
             * Dispatcher VeryifyAccess() issues because
             * this may be called from the GC */
			StopInternal();

			/* Let's clean up the base 
			 * class's stuff first */
			base.FreeResources();

			if (m_graph != null)
			{
				Marshal.ReleaseComObject(m_graph);
				m_graph = null;

				/* Only run the media closed if we have an
				 * initialized filter graph */
				InvokeMediaClosed(new EventArgs());
			}
		}
	}
}