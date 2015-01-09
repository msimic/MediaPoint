using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using System.IO;
using DirectShowLib;
using MediaPoint.Common.MediaFoundation;
using System.Reflection;
using MediaPoint.Common.MediaFoundation.Interop;
using MediaPoint.Subtitles.Logic;
using MediaPoint.Common.Interfaces.LavAudio;

namespace MediaPoint.Common.Interfaces
{
    //[ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    //public interface IPropertyBag
    //{
    //   [PreserveSig]
    //   int Read(
    //     [In, MarshalAs(UnmanagedType.LPWStr)] string pszPropName,
    //     [Out, MarshalAs(UnmanagedType.Struct)] out object pVar,
    //     [In] IErrorLog pErrorLog
    //   );

    //   [PreserveSig]
    //   int Write(
    //     [In, MarshalAs(UnmanagedType.LPWStr)] string pszPropName,
    //     [In, MarshalAs(UnmanagedType.Struct)] ref object pVar
    //   );
    //}

	#region MadVR
	
	[ComVisible(true), ComImport, SuppressUnmanagedCodeSecurity,
	 Guid("1CAEE23B-D14B-4DB4-8AEA-F3528CB78922"),
	 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IMadVRDirect3D9Manager
	{
		[PreserveSig]
		int UseTheseDevices(IntPtr scanlineReading, IntPtr rendering, IntPtr presentation);
	}

	#endregion

    #region DCDSP

    [ComImport, Guid("B38C58A0-1809-11D6-A458-EDAE78F1DF12")]
    public class DCDSPFilter
    {
    }

    #endregion

    #region "LAV COM classes"

    [ComImport, Guid("9852A670-F845-491b-9BE6-EBD841B8A613")]
    public class DirectVobSub
    {
    }

    #endregion

    #region "LAV COM classes"

    // use these if LAV is already registered (regsvr32)
	// you will need the apropriate architecture (ie. if your player is 64 bits a 64 bit LAv filter must be installed)
	// if you want to use your own supplied LAV filter without registration see FilterProvider class

	[ComImport, Guid("B98D13E7-55DB-4385-A33D-09FD1BA26338")]
	public class LAVSplitterSource
	{
	}
	[ComImport, Guid("171252A0-8820-4AFE-9DF8-5C92B2D66B04")]
	public class LAVSplitter
	{
	}

	[ComImport, Guid("EE30215D-164F-4A92-A4EB-9D4C13390F9F")]
	public class LAVVideo
	{
	}

	#endregion

	#region "Lav Video settings interface, implemented by LAVVideo"


	// Codecs supported in the LAV Video configuration
	// Codecs not listed here cannot be turned off. You can request codecs to be added to this list, if you wish.
	public enum LAVVideoCodec
	{
		Codec_H264,
		Codec_VC1,
		Codec_MPEG1,
		Codec_MPEG2,
		Codec_MPEG4,
		Codec_MSMPEG4,
		Codec_VP8,
		Codec_WMV3,
		Codec_WMV12,
		Codec_MJPEG,
		Codec_Theora,
		Codec_FLV1,
		Codec_VP6,
		Codec_SVQ,
		Codec_H261,
		Codec_H263,
		Codec_Indeo,
		Codec_TSCC,
		Codec_Fraps,
		Codec_HuffYUV,
		Codec_QTRle,
		Codec_DV,
		Codec_Bink,
		Codec_Smacker,
		Codec_RV12,
		Codec_RV34,
		Codec_Lagarith,
		Codec_Cinepak,
		Codec_Camstudio,
		Codec_QPEG,
		Codec_ZLIB,
		Codec_QTRpza,
		Codec_PNG,
		Codec_MSRLE,
		Codec_ProRes,
		Codec_UtVideo,
		Codec_Dirac,
		Codec_DNxHD,
		Codec_MSVideo1,
		Codec_8BPS,
		Codec_LOCO,
		Codec_ZMBV,
		Codec_VCR1,
		Codec_Snow,
		Codec_FFV1,
		Codec_v210,
		//Codec_NB            // Number of entrys (do not use when dynamically linking)
	};

	// Codecs with hardware acceleration
	public enum LAVVideoHWCodec
	{
		HWCodec_H264 = LAVVideoCodec.Codec_H264,
		HWCodec_VC1 = LAVVideoCodec.Codec_VC1,
		HWCodec_MPEG2 = LAVVideoCodec.Codec_MPEG2,
		HWCodec_MPEG4 = LAVVideoCodec.Codec_MPEG4,

		HWCodec_NB = LAVVideoHWCodec.HWCodec_MPEG4 + 1
	};

	// Type of hardware accelerations
	public enum LAVHWAccel
	{
		HWAccel_None,
		HWAccel_CUDA,
		HWAccel_QuickSync,
		HWAccel_DXVA2,
		HWAccel_DXVA2CopyBack = HWAccel_DXVA2,
		HWAccel_DXVA2Native
	};

	// Deinterlace algorithms offered by the hardware decoders
	public enum LAVHWDeintModes
	{
		HWDeintMode_Weave,
		HWDeintMode_BOB,
		HWDeintMode_Hardware
	};

	// Software deinterlacing algorithms
	public enum LAVSWDeintModes
	{
		SWDeintMode_None,
		SWDeintMode_YADIF
	};

	// Type of deinterlacing to perform
	// - FramePerField re-constructs one frame from every field, resulting in 50/60 fps.
	// - FramePer2Field re-constructs one frame from every 2 fields, resulting in 25/30 fps.
	// Note: Weave will always use FramePer2Field
	public enum LAVDeintOutput
	{
		DeintOutput_FramePerField,
		DeintOutput_FramePer2Field
	};

	// Control the field order of the deinterlacer
	public enum LAVDeintFieldOrder
	{
		DeintFieldOrder_Auto,
		DeintFieldOrder_TopFieldFirst,
		DeintFieldOrder_BottomFieldFirst,
	};

	// Supported output pixel formats
	public enum LAVOutPixFmts
	{
		LAVOutPixFmt_None = -1,
		LAVOutPixFmt_YV12,            // 4:2:0, 8bit, planar
		LAVOutPixFmt_NV12,            // 4:2:0, 8bit, Y planar, U/V packed
		LAVOutPixFmt_YUY2,            // 4:2:2, 8bit, packed
		LAVOutPixFmt_UYVY,            // 4:2:2, 8bit, packed
		LAVOutPixFmt_AYUV,            // 4:4:4, 8bit, packed
		LAVOutPixFmt_P010,            // 4:2:0, 10bit, Y planar, U/V packed
		LAVOutPixFmt_P210,            // 4:2:2, 10bit, Y planar, U/V packed
		LAVOutPixFmt_Y410,            // 4:4:4, 10bit, packed
		LAVOutPixFmt_P016,            // 4:2:0, 16bit, Y planar, U/V packed
		LAVOutPixFmt_P216,            // 4:2:2, 16bit, Y planar, U/V packed
		LAVOutPixFmt_Y416,            // 4:4:4, 16bit, packed
		LAVOutPixFmt_RGB32,           // 32-bit RGB (BGRA)
		LAVOutPixFmt_RGB24,           // 24-bit RGB (BGR)

		LAVOutPixFmt_v210,            // 4:2:2, 10bit, packed
		LAVOutPixFmt_v410,            // 4:4:4, 10bit, packed

		LAVOutPixFmt_YV16,            // 4:2:2, 8-bit, planar
		LAVOutPixFmt_YV24,            // 4:4:4, 8-bit, planar

		//LAVOutPixFmt_NB               // Number of formats
	};

	// dithering mode used by the filter
	public enum LAVDitherMode
	{
		LAVDither_Ordered,
		LAVDither_Random
	};

    // deinterlacing mode
    public enum LAVDeintMode {
        DeintMode_Auto,
        DeintMode_Aggressive,
        DeintMode_Force,
        DeintMode_Disable
    };

    // resolutions for hardware accelerated decoding
    [Flags]
    public enum LAVHWResFlag 
    {
        SD      = 0x0001,
        HD      = 0x0002,
        UHD     = 0x0004
    }

	// LAV Video configuration interface
	[ComVisible(true), ComImport, SuppressUnmanagedCodeSecurity,
		 Guid("FA40D6E9-4D38-4761-ADD2-71A9EC5FD32F"),
		 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ILAVVideoSettings
	{
		// Switch to Runtime Config mode. This will reset all settings to default, and no changes to the settings will be saved
		// You can use this to programmatically configure LAV Audio without interfering with the users settings in the registry.
		// Subsequent calls to this function will reset all settings back to defaults, even if the mode does not change.
		//
		// Note that calling this function during playback is not supported and may exhibit undocumented behaviour. 
		// For smooth operations, it must be called before LAV Audio is connected to other filters.
		[PreserveSig]
		int SetRuntimeConfig(bool bRuntimeConfig);

		// Configure which codecs are enabled
		// If vCodec is invalid (possibly a version difference), Get will return FALSE, and Set E_FAIL.
		[PreserveSig]
		bool GetFormatConfiguration(LAVVideoCodec vCodec);
		[PreserveSig]
		int SetFormatConfiguration(LAVVideoCodec vCodec, bool bEnabled);

		// Set the number of threads to use for Multi-Threaded decoding (where available)
		//  0 = Auto Detect (based on number of CPU cores)
		//  1 = 1 Thread -- No Multi-Threading
		// >1 = Multi-Threading with the specified number of threads
		[PreserveSig]
		int SetNumThreads(int dwNum);

		// Get the number of threads to use for Multi-Threaded decoding (where available)
		//  0 = Auto Detect (based on number of CPU cores)
		//  1 = 1 Thread -- No Multi-Threading
		// >1 = Multi-Threading with the specified number of threads
		[PreserveSig]
		int GetNumThreads();

		// Set wether the aspect ratio encoded in the stream should be forwarded to the renderer,
		// or the aspect ratio specified by the source filter should be kept.
		// TRUE  = AR from the Stream
		// FALSE = AR from the source filter
		[PreserveSig]
		int SetStreamAR(bool bStreamAR);

		// Get wether the aspect ratio encoded in the stream should be forwarded to the renderer,
		// or the aspect ratio specified by the source filter should be kept.
		// TRUE  = AR from the Stream
		// FALSE = AR from the source filter
		[PreserveSig]
		bool GetStreamAR();

		// Configure which pixel formats are enabled for output
		// If pixFmt is invalid, Get will return FALSE and Set E_FAIL
		[PreserveSig]
		bool GetPixelFormat(LAVOutPixFmts pixFmt);
		[PreserveSig]
		int SetPixelFormat(LAVOutPixFmts pixFmt, bool bEnabled);

		// Set the RGB output range for the YUV->RGB conversion
		// 0 = Auto (same as input), 1 = Limited (16-235), 2 = Full (0-255)
		[PreserveSig]
		int SetRGBOutputRange(int dwRange);

		// Get the RGB output range for the YUV->RGB conversion
		// 0 = Auto (same as input), 1 = Limited (16-235), 2 = Full (0-255)
		[PreserveSig]
		int GetRGBOutputRange();

		// Set the deinterlacing field order of the hardware decoder
		[PreserveSig]
		int SetDeintFieldOrder(LAVDeintFieldOrder fieldOrder);

		// get the deinterlacing field order of the hardware decoder
		[PreserveSig]
		LAVDeintFieldOrder GetDeintFieldOrder();

		// Set wether all frames should be deinterlaced if the stream is flagged interlaced
		[PreserveSig]
		int SetDeintAggressive(bool bAggressive);

		// Get wether all frames should be deinterlaced if the stream is flagged interlaced
		[PreserveSig]
		bool GetDeintAggressive();

		// Set wether all frames should be deinterlaced, even ones marked as progressive
		[PreserveSig]
		int SetDeintForce(bool bForce);

		// Get wether all frames should be deinterlaced, even ones marked as progressive
		[PreserveSig]
		bool GetDeintForce();

		// Check if the specified HWAccel is supported
		// Note: This will usually only check the availability of the required libraries (ie. for NVIDIA if a recent enough NVIDIA driver is installed)
		// and not check actual hardware support
		// Returns: 0 = Unsupported, 1 = Supported, 2 = Currently running
		[PreserveSig]
		int CheckHWAccelSupport(LAVHWAccel hwAccel);

		// Set which HW Accel method is used
		// See LAVHWAccel for options.
		[PreserveSig]
		int SetHWAccel(LAVHWAccel hwAccel);

		// Get which HW Accel method is active
		[PreserveSig]
		LAVHWAccel GetHWAccel();

		// Set which codecs should use HW Acceleration
		[PreserveSig]
		int SetHWAccelCodec(LAVVideoHWCodec hwAccelCodec, bool bEnabled);

		// Get which codecs should use HW Acceleration
		[PreserveSig]
		bool GetHWAccelCodec(LAVVideoHWCodec hwAccelCodec);

		// Set the deinterlacing mode used by the hardware decoder
		[PreserveSig]
		int SetHWAccelDeintMode(LAVHWDeintModes deintMode);

		// Get the deinterlacing mode used by the hardware decoder
		[PreserveSig]
		LAVHWDeintModes GetHWAccelDeintMode();

		// Set the deinterlacing output for the hardware decoder
		[PreserveSig]
		int SetHWAccelDeintOutput(LAVDeintOutput deintOutput);

		// Get the deinterlacing output for the hardware decoder
		[PreserveSig]
		LAVDeintOutput GetHWAccelDeintOutput();

		// Set wether the hardware decoder should force high-quality deinterlacing
		// Note: this option is not supported on all decoder implementations and/or all operating systems
		[PreserveSig]
		int SetHWAccelDeintHQ(bool bHQ);

		// Get wether the hardware decoder should force high-quality deinterlacing
		// Note: this option is not supported on all decoder implementations and/or all operating systems
		[PreserveSig]
		bool GetHWAccelDeintHQ();

		// Set the software deinterlacing mode used
		[PreserveSig]
		int SetSWDeintMode(LAVSWDeintModes deintMode);

		// Get the software deinterlacing mode used
		[PreserveSig]
		LAVSWDeintModes GetSWDeintMode();

		// Set the software deinterlacing output
		[PreserveSig]
		int SetSWDeintOutput(LAVDeintOutput deintOutput);

		// Get the software deinterlacing output
		[PreserveSig]
		LAVDeintOutput GetSWDeintOutput();

		// Set wether all content is treated as progressive, and any interlaced flags are ignored
		[PreserveSig]
		int SetDeintTreatAsProgressive(bool bEnabled);

		// Get wether all content is treated as progressive, and any interlaced flags are ignored
		[PreserveSig]
		bool GetDeintTreatAsProgressive();

		// Set the dithering mode used
		[PreserveSig]
		int SetDitherMode(LAVDitherMode ditherMode);

		// Get the dithering mode used
		[PreserveSig]
		LAVDitherMode GetDitherMode();

        // Set if the MS WMV9 DMO Decoder should be used for VC-1/WMV3
        [PreserveSig]
        int SetUseMSWMV9Decoder(bool bEnabled);

        // Get if the MS WMV9 DMO Decoder should be used for VC-1/WMV3
        [PreserveSig]
        bool GetUseMSWMV9Decoder();

        // Set if DVD Video support is enabled
        [PreserveSig]
        int SetDVDVideoSupport(bool bEnabled);

        // Get if DVD Video support is enabled
        [PreserveSig]
        bool GetDVDVideoSupport();

        // Set the HW Accel Resolution Flags
        // flags: bitmask of LAVHWResFlag flags
        [PreserveSig]
        int SetHWAccelResolutionFlags(LAVHWResFlag dwResFlags);

        // Get the HW Accel Resolution Flags
        // flags: bitmask of LAVHWResFlag flags
        [PreserveSig]
        LAVHWResFlag GetHWAccelResolutionFlags();

        // Toggle Tray Icon
        [PreserveSig]
        int SetTrayIcon(bool bEnabled);

        // Get Tray Icon
        [PreserveSig]
        bool GetTrayIcon();

        // Set the Deint Mode
        [PreserveSig]
        int SetDeinterlacingMode(LAVDeintMode deintMode);

        // Get the Deint Mode
        [PreserveSig]
        LAVDeintMode GetDeinterlacingMode();

        // Set the index of the GPU to be used for hardware decoding
        // Only supported for CUVID and DXVA2 copy-back. If the device is not valid, it'll fallback to auto-detection
        // Must be called before an input is connected to LAV Video, and the setting is non-persistent
        // NOTE: For CUVID, the index defines the index of the CUDA capable device, while for DXVA2, the list includes all D3D9 devices
        [PreserveSig]
        int SetGPUDeviceIndex(uint dwDevice);
	};

	#endregion

	#region "Lav splitter settings interface, implemented by LAVSplitter and LAVSplitterSource"

	public enum LAVSubtitleMode
	{
		LAVSubtitleMode_NoSubs = 0,
		LAVSubtitleMode_ForcedOnly,
		LAVSubtitleMode_Default,
		LAVSubtitleMode_Advanced
	};

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NORMALIZEDRECT
    {
        public float left;
        public float top;
        public float right;
        public float bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class LOGFONT
    {
        public int lfHeight;
        public int lfWidth;
        public int lfEscapement;
        public int lfOrientation;
        public int lfWeight;
        public byte lfItalic;
        public byte lfUnderline;
        public byte lfStrikeOut;
        public byte lfCharSet;
        public byte lfOutPrecision;
        public byte lfClipPrecision;
        public byte lfQuality;
        public byte lfPitchAndFamily;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string lfFaceName = string.Empty;
    }

    [ComVisible(true), ComImport, SuppressUnmanagedCodeSecurity,
     Guid("EBE1FB08-3957-47ca-AF13-5827E5442E56"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDirectVobSub : IBaseFilter
    {
        [PreserveSig]
        int get_FileName([MarshalAs(UnmanagedType.LPWStr, SizeConst = 260)]ref StringBuilder fn);

        [PreserveSig]
        int put_FileName([MarshalAs(UnmanagedType.LPWStr)]string fn) ;

        [PreserveSig]
        int get_LanguageCount(out int nLangs) ;

        [PreserveSig]
        int get_LanguageName(int iLanguage, [MarshalAs(UnmanagedType.LPWStr)] out string ppName) ;
        
        [PreserveSig]
        int get_SelectedLanguage(ref int iSelected);

        [PreserveSig]
        int put_SelectedLanguage(int iSelected) ;

        [PreserveSig]
        int get_HideSubtitles(ref bool fHideSubtitles);

        [PreserveSig]
        int put_HideSubtitles([MarshalAsAttribute(UnmanagedType.I1)] bool fHideSubtitles) ;

        [PreserveSig]
        int get_PreBuffering(ref bool fDoPreBuffering);

        [PreserveSig]
        int put_PreBuffering([MarshalAsAttribute(UnmanagedType.I1)] bool fDoPreBuffering) ;

        [PreserveSig]
        int get_Placement(ref bool fOverridePlacement, ref int xperc, ref int yperc);

        [PreserveSig]
        int put_Placement([MarshalAsAttribute(UnmanagedType.I1)] bool fOverridePlacement, int xperc, int yperc) ;

        [PreserveSig]
        int get_VobSubSettings(ref bool fBuffer, ref bool fOnlyShowForcedSubs, ref bool fPolygonize);

        [PreserveSig]
        int put_VobSubSettings([MarshalAsAttribute(UnmanagedType.I1)] bool fBuffer, [MarshalAsAttribute(UnmanagedType.I1)] bool fOnlyShowForcedSubs, [MarshalAsAttribute(UnmanagedType.I1)] bool fPolygonize) ;

        [PreserveSig]
        int get_TextSettings(LOGFONT lf, int lflen, ref uint color, ref bool fShadow, ref bool fOutline, ref bool fAdvancedRenderer);

        [PreserveSig]
        int put_TextSettings(LOGFONT lf, int lflen, uint color, bool fShadow, bool fOutline, bool fAdvancedRenderer);

        [PreserveSig]
        int get_Flip(ref bool fPicture, ref bool fSubtitles);
        
        [PreserveSig]
        int put_Flip([MarshalAsAttribute(UnmanagedType.I1)] bool fPicture, [MarshalAsAttribute(UnmanagedType.I1)] bool fSubtitles);

        [PreserveSig]
        int get_OSD(ref bool fOSD);

        [PreserveSig]
        int put_OSD([MarshalAsAttribute(UnmanagedType.I1)] bool fOSD);

        [PreserveSig]
        int get_SaveFullPath([MarshalAsAttribute(UnmanagedType.I1)] ref bool fSaveFullPath);

        [PreserveSig]
        int put_SaveFullPath([MarshalAsAttribute(UnmanagedType.I1)]bool fSaveFullPath);

        [PreserveSig]
        int get_SubtitleTiming(ref int delay, ref int speedmul, ref int speeddiv);

        [PreserveSig]
        int put_SubtitleTiming(int delay, int speedmul, int speeddiv);
    }

    [ComVisible(true), ComImport, SuppressUnmanagedCodeSecurity,
     Guid("85E5D6F9-BEFB-4E01-B047-758359CDF9AB"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDirectVobSubXy : IBaseFilter
    {
        [PreserveSig]
        int XyGetBool(int field, ref bool value);
        [PreserveSig]
        int XyGetInt(int field, ref int value);
        [PreserveSig]
        int XyGetSize(int field, ref SIZE value);
        [PreserveSig]
        int XyGetRect(int field, ref RECT value);
        [PreserveSig]
        int XyGetUlonglong(int field, ref long value);
        [PreserveSig]
        int XyGetDouble(int field, ref double value);
        [PreserveSig]
        int XyGetString(int field, ref string value, ref int chars);
        [PreserveSig]
        int XyGetBin(int field, ref IntPtr value, ref int size);

        [PreserveSig]
        int XySetBool(int field, bool value);
        [PreserveSig]
        int XySetInt(int field, int value);
        [PreserveSig]
        int XySetSize(int field, SIZE value);
        [PreserveSig]
        int XySetRect(int field, RECT value);
        [PreserveSig]
        int XySetUlonglong(int field, long value);
        [PreserveSig]
        int XySetDouble(int field, double value);
        [PreserveSig]
        int XySetString(int field, string value, int chars);
        [PreserveSig]
        int XySetBin(int field, IntPtr value, int size);

    }

    [ComVisible(true), ComImport, SuppressUnmanagedCodeSecurity,
     Guid("AB52FC9C-2415-4dca-BC1C-8DCC2EAE8151"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDirectVobSub3 : IBaseFilter
    {
        [PreserveSig]
        int OpenSubtitles([MarshalAs(UnmanagedType.LPWStr)]string fn);

        [PreserveSig]
        int SkipAutoloadCheck(int boolval);
    }

    [ComVisible(true), ComImport, SuppressUnmanagedCodeSecurity,
	 Guid("774A919D-EA95-4A87-8A1E-F48ABE8499C7"),
	 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ILAVSplitterSettings
	{
		// Switch to Runtime Config mode. This will reset all settings to default, and no changes to the settings will be saved
		// You can use this to programmatically configure LAV Splitter without interfering with the users settings in the registry.
		// Subsequent calls to this function will reset all settings back to defaults, even if the mode does not change.
		//
		// Note that calling this function during playback is not supported and may exhibit undocumented behaviour. 
		// For smooth operations, it must be called before LAV Splitter opens a file.
		[PreserveSig]
		int SetRuntimeConfig(bool runtime);


		// Retrieve the preferred languages as ISO 639-2 language codes, comma seperated
		// If the result is NULL, no language has been set
		// Memory for the string will be allocated, and has to be free'ed by the caller with CoTaskMemFree
		[PreserveSig]
		int GetPreferredLanguages([MarshalAs(UnmanagedType.LPWStr)]out string langs);

		// Set the preferred languages as ISO 639-2 language codes, comma seperated
		// To reset to no preferred language, pass NULL or the empty string
		[PreserveSig]
		int SetPreferredLanguages([MarshalAs(UnmanagedType.LPWStr)]string langs);

		// Retrieve the preferred subtitle languages as ISO 639-2 language codes, comma seperated
		// If the result is NULL, no language has been set
		// If no subtitle language is set, the main language preference is used.
		// Memory for the string will be allocated, and has to be free'ed by the caller with CoTaskMemFree
		[PreserveSig]
		int GetPreferredSubtitleLanguages([MarshalAs(UnmanagedType.LPWStr)]out string langs);

		// Set the preferred subtitle languages as ISO 639-2 language codes, comma seperated
		// To reset to no preferred language, pass NULL or the empty string
		// If no subtitle language is set, the main language preference is used.
		[PreserveSig]
		int SetPreferredSubtitleLanguages([MarshalAs(UnmanagedType.LPWStr)]string langs);

		// Get the current subtitle mode
		// See enum for possible values
		[PreserveSig]
		LAVSubtitleMode GetSubtitleMode();

		// Set the current subtitle mode
		// See enum for possible values
		[PreserveSig]
		int SetSubtitleMode(LAVSubtitleMode mode);

		// Get the subtitle matching language flag
		// TRUE = Only subtitles with a language in the preferred list will be used; FALSE = All subtitles will be used
		// @deprecated - do not use anymore, deprecated and non-functional, replaced by advanced subtitle mode
		[PreserveSig]
		bool GetSubtitleMatchingLanguage();

		// Set the subtitle matching language flag
		// TRUE = Only subtitles with a language in the preferred list will be used; FALSE = All subtitles will be used
		// @deprecated - do not use anymore, deprecated and non-functional, replaced by advanced subtitle mode
		[PreserveSig]
		int SetSubtitleMatchingLanguage(bool mode);

		// Control wether a special "Forced Subtitles" stream will be created for PGS subs
		[PreserveSig]
		bool GetPGSForcedStream();

		// Control wether a special "Forced Subtitles" stream will be created for PGS subs
		[PreserveSig]
		int SetPGSForcedStream(bool enabled);

		// Get the PGS forced subs config
		// TRUE = only forced PGS frames will be shown, FALSE = all frames will be shown
		[PreserveSig]
		bool GetPGSOnlyForced();

		// Set the PGS forced subs config
		// TRUE = only forced PGS frames will be shown, FALSE = all frames will be shown
		[PreserveSig]
		int SetPGSOnlyForced(bool forced);

		// Get the VC-1 Timestamp Processing mode
		// 0 - No Timestamp Correction, 1 - Always Timestamp Correction, 2 - Auto (Correction for Decoders that need it)
		[PreserveSig]
		int GetVC1TimestampMode();

		// Set the VC-1 Timestamp Processing mode
		// 0 - No Timestamp Correction, 1 - Always Timestamp Correction, 2 - Auto (Correction for Decoders that need it)
		[PreserveSig]
		int SetVC1TimestampMode(short enabled);

		// Set whether substreams (AC3 in TrueHD, for example) should be shown as a seperate stream
		[PreserveSig]
		int SetSubstreamsEnabled(bool enabled);

		// Check whether substreams (AC3 in TrueHD, for example) should be shown as a seperate stream
		[PreserveSig]
		bool GetSubstreamsEnabled();

		// Set if the ffmpeg parsers should be used for video streams
		[PreserveSig]
		int SetVideoParsingEnabled(bool enabled);

		// Query if the ffmpeg parsers are being used for video streams
		[PreserveSig]
		bool GetVideoParsingEnabled();

		// Set if LAV Splitter should try to fix broken HD-PVR streams
		[PreserveSig]
		int SetFixBrokenHDPVR(bool enabled);

		// Query if LAV Splitter should try to fix broken HD-PVR streams
		[PreserveSig]
		bool GetFixBrokenHDPVR();

		// Control wether the givne format is enabled
		[PreserveSig]
		int SetFormatEnabled([MarshalAs(UnmanagedType.LPStr)]string strFormat, bool bEnabled);

		// Check if the given format is enabled
		[PreserveSig]
		bool IsFormatEnabled([MarshalAs(UnmanagedType.LPStr)]string strFormat);

		// Set if LAV Splitter should always completely remove the filter connected to its Audio Pin when the audio stream is changed
		[PreserveSig]
		int SetStreamSwitchRemoveAudio(bool enabled);

		// Query if LAV Splitter should always completely remove the filter connected to its Audio Pin when the audio stream is changed
		[PreserveSig]
		bool GetStreamSwitchRemoveAudio();

		// Advanced Subtitle configuration. Refer to the documention for details.
		// If no advanced config exists, will be NULL.
		// Memory for the string will be allocated, and has to be free'ed by the caller with CoTaskMemFree
		[PreserveSig]
		int GetAdvancedSubtitleConfig([MarshalAs(UnmanagedType.LPWStr)]out string ec);

		// Advanced Subtitle configuration. Refer to the documention for details.
		// To reset the config, pass NULL or the empty string.
		// If no subtitle language is set, the main language preference is used.
		[PreserveSig]
		int SetAdvancedSubtitleConfig([MarshalAs(UnmanagedType.LPWStr)]string config);

	}

	#endregion

	#region "FIlterProvider - methods to get all the interfaces without having a registered COM object"

	/// <summary>
	/// Class used to provide LAV filters as COM object without the need to register the filters
	/// (using native methods to extract the com object with the IClassFactory interface)
	/// </summary>
	public static class FilterProvider
	{
		/// <summary>
		/// Delegate signature of GetClassObject in COM libraries
		/// </summary>
		internal delegate int LavVideoDllGetClassObject([MarshalAs(UnmanagedType.LPStruct)] Guid clsid,
											  [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
											  [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr LoadLibrary(String dllname);

        [DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr GetProcAddress(IntPtr hModule, String procname);

		/// <summary>
		/// The GUID of IUnknown
		/// </summary>
		public static readonly Guid IUNKNOWN_GUID = new Guid("{00000000-0000-0000-C000-000000000046}");

        /// <summary>
        /// The GUID of VSFilter / VOBSub
        /// </summary>
        public static readonly Guid IVOBSUB_GUID = typeof(DirectVobSub).GUID; //new Guid("{9852A670-F845-491b-9BE6-EBD841B8A613}");

        /// <summary>
        /// The GUID of VSFilter / VOBSub
        /// </summary>
        public static readonly Guid IDCDSPFILTER_GUID = typeof(DCDSPFilter).GUID; //new Guid("{B38C58A0-1809-11D6-A458-EDAE78F1DF12}");

		/// <summary>
		/// The GUID of LAVVideo
		/// </summary>
		public static readonly Guid ILAVVIDEO_GUID = typeof(LAVVideo).GUID; //new Guid("{EE30215D-164F-4A92-A4EB-9D4C13390F9F}");

		/// <summary>
		/// The GUID of LAVAudio
		/// </summary>	
		public static readonly Guid ILAVAUDIO_GUID = typeof(LAVAudio).GUID; //new Guid("{E8E73B6B-4CB3-44A4-BE99-4F7BCB96E491}");

		/// <summary>
		/// The GUID of LAVSplitter
		/// </summary>	
		public static readonly Guid ILAVSPLITTER_GUID = typeof(LAVSplitter).GUID; //new Guid("{171252A0-8820-4AFE-9DF8-5C92B2D66B04}");

		/// <summary>
		/// The GUID of LAVSplitterSource
		/// </summary>	
		public static readonly Guid ILAVSPLITTERSOURCE_GUID = typeof(LAVSplitterSource).GUID; //new Guid("{B98D13E7-55DB-4385-A33D-09FD1BA26338}");

		/// <summary>
		/// Will use this to make the method thread safe
		/// </summary>
		public static object threadSync = new object();

		/// <summary>
		/// Gets the IBaseFilter interface for the LAVVideo filter - you must release this when finished using it with Marshal.ReleaseComObject
		/// </summary>
		/// <param name="settings">Get the Lav video settings interface used to controls and get info about the video filter
		///  - you must release this when finished using it with Marshal.ReleaseComObject</param>
		/// <param name="subDir">subdirectory of your app where you store codec files (LAV*.ax) - default "codecs\"</param>
		/// <returns>LAVVideo interface to put into filterGraph</returns>
		public static IBaseFilter GetVideoFilter(out ILAVVideoSettings settings, string subDir = @"codecs\")
		{
			lock (threadSync)
			{
				settings = null;
				object oFactory = null;
				object oFilter = null;
				object oSettings = null;
				IBaseFilter filter = null;
				string currentDir = Directory.GetCurrentDirectory();
				// we have the filters in the subdirectory 'codecs' of the running app
				string path = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), subDir);

				try
				{
					// we need to be in the filter directory since it will load a bunch 
					// of other dlls that are there, and they won't resolve otherwise
					Directory.SetCurrentDirectory(path);

					path = Path.Combine(path, "LAVVideo.ax");

					IntPtr lavVideoDll = LoadLibrary(path);
					IntPtr proc = GetProcAddress(lavVideoDll, "DllGetClassObject");

					var getClassObject = (LavVideoDllGetClassObject)Marshal.GetDelegateForFunctionPointer(proc, typeof(LavVideoDllGetClassObject));

					int hr = getClassObject(ILAVVIDEO_GUID, IUNKNOWN_GUID, out oFactory);

					IClassFactory factory = oFactory as IClassFactory;

					if (factory == null)
					{
						if (oFactory != null) Marshal.ReleaseComObject(oFactory);
						throw new Exception("Could not QueryInterface for the IClassFactory interface");
					}

					Guid baseFilterGUID = typeof(IBaseFilter).GUID;
					hr = factory.CreateInstance(null, ref baseFilterGUID, out oFilter);

					filter = oFilter as IBaseFilter;
					if (filter == null)
					{
						if (oFilter != null) Marshal.ReleaseComObject(oFilter);
						throw new Exception("Could not QueryInterface for the IBaseFilter interface");
					}

                    settings = (ILAVVideoSettings)filter;

				}
				catch
				{
					// if somehting bad happens give back the path since we will rethrow the exception ater cleanup
					Directory.SetCurrentDirectory(currentDir);

					if (oFactory != null)
						Marshal.FinalReleaseComObject(oFactory);

					if (oFilter != null)
						Marshal.FinalReleaseComObject(oFilter);

					if (oSettings != null)
						Marshal.FinalReleaseComObject(oSettings);

					throw;
				}
				finally
				{
					// even if nothing bad happens we need to clenup and give back to the original path
					Directory.SetCurrentDirectory(currentDir);

					if (oFactory != null)
						Marshal.FinalReleaseComObject(oFactory);

				}

				return filter;
			}
		}

        public static IBaseFilter GetVobSubFilter(string subDir = @"codecs\")
        {
            lock (threadSync)
            {
                //settings = null;
                object oFactory = null;
                object oFilter = null;
                object oSettings = null;
                IBaseFilter filter = null;
                string currentDir = Directory.GetCurrentDirectory();
                // we have the filters in the subdirectory 'codecs' of the running app
                string path = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), subDir);

                try
                {
                    // we need to be in the filter directory since it will load a bunch 
                    // of other dlls that are there, and they won't resolve otherwise
                    Directory.SetCurrentDirectory(path);

                    path = Path.Combine(path, "VSFilter.dll");

                    IntPtr lavVideoDll = LoadLibrary(path);
                    IntPtr proc = GetProcAddress(lavVideoDll, "DllGetClassObject");

                    var getClassObject = (LavVideoDllGetClassObject)Marshal.GetDelegateForFunctionPointer(proc, typeof(LavVideoDllGetClassObject));

                    int hr = getClassObject(IVOBSUB_GUID, IUNKNOWN_GUID, out oFactory);

                    IClassFactory factory = oFactory as IClassFactory;

                    if (factory == null)
                    {
                        if (oFactory != null) Marshal.ReleaseComObject(oFactory);
                        throw new Exception("Could not QueryInterface for the IClassFactory interface");
                    }

                    Guid baseFilterGUID = typeof(IBaseFilter).GUID;
                    hr = factory.CreateInstance(null, ref baseFilterGUID, out oFilter);

                    filter = oFilter as IBaseFilter;
                    if (filter == null)
                    {
                        if (oFilter != null) Marshal.ReleaseComObject(oFilter);
                        throw new Exception("Could not QueryInterface for the IBaseFilter interface");
                    }

                    //Guid videoSettingsGUID = new Guid("{FA40D6E9-4D38-4761-ADD2-71A9EC5FD32F}");
                    //hr = factory.CreateInstance(null, ref videoSettingsGUID, out oSettings);

                    //settings = oSettings as ILAVVideoSettings;
                    //if (filter == null)
                    //{
                    //    if (oSettings != null) Marshal.ReleaseComObject(oSettings);
                    //    throw new Exception("Could not QueryInterface for the ILAVVideoSettings interface");
                    //}

                }
                catch
                {
                    // if somehting bad happens give back the path since we will rethrow the exception ater cleanup
                    Directory.SetCurrentDirectory(currentDir);

                    if (oFactory != null)
                        Marshal.FinalReleaseComObject(oFactory);

                    if (oFilter != null)
                        Marshal.FinalReleaseComObject(oFilter);

                    if (oSettings != null)
                        Marshal.FinalReleaseComObject(oSettings);

                    throw;
                }
                finally
                {
                    // even if nothing bad happens we need to clenup and give back to the original path
                    Directory.SetCurrentDirectory(currentDir);

                    if (oFactory != null)
                        Marshal.FinalReleaseComObject(oFactory);

                }

                return filter;
            }
        }

        public static IBaseFilter GetDCDSPFilter(string subDir = @"codecs\")
        {
            lock (threadSync)
            {
                //settings = null;
                object oFactory = null;
                object oFilter = null;
                object oSettings = null;
                IBaseFilter filter = null;
                string currentDir = Directory.GetCurrentDirectory();
                // we have the filters in the subdirectory 'codecs' of the running app
                string path = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), subDir);

                try
                {
                    // we need to be in the filter directory since it will load a bunch 
                    // of other dlls that are there, and they won't resolve otherwise
                    Directory.SetCurrentDirectory(path);

                    path = Path.Combine(path, "dcdspfilter.ax");

                    IntPtr lavVideoDll = LoadLibrary(path);
                    IntPtr proc = GetProcAddress(lavVideoDll, "DllGetClassObject");

                    var getClassObject = (LavVideoDllGetClassObject)Marshal.GetDelegateForFunctionPointer(proc, typeof(LavVideoDllGetClassObject));

                    int hr = getClassObject(IDCDSPFILTER_GUID, IUNKNOWN_GUID, out oFactory);
                    if (hr != 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    IClassFactory factory = oFactory as IClassFactory;

                    if (factory == null)
                    {
                        if (oFactory != null) Marshal.ReleaseComObject(oFactory);
                        throw new Exception("Could not QueryInterface for the IClassFactory interface");
                    }

                    Guid baseFilterGUID = typeof(IBaseFilter).GUID;
                    hr = factory.CreateInstance(null, ref baseFilterGUID, out oFilter);

                    filter = oFilter as IBaseFilter;
                    if (filter == null)
                    {
                        if (oFilter != null) Marshal.ReleaseComObject(oFilter);
                        throw new Exception("Could not QueryInterface for the IBaseFilter interface");
                    }

                }
                catch
                {
                    // if somehting bad happens give back the path since we will rethrow the exception ater cleanup
                    Directory.SetCurrentDirectory(currentDir);

                    if (oFactory != null)
                        Marshal.FinalReleaseComObject(oFactory);

                    if (oFilter != null)
                        Marshal.FinalReleaseComObject(oFilter);

                    if (oSettings != null)
                        Marshal.FinalReleaseComObject(oSettings);

                    throw;
                }
                finally
                {
                    // even if nothing bad happens we need to clenup and give back to the original path
                    Directory.SetCurrentDirectory(currentDir);

                    if (oFactory != null)
                        Marshal.FinalReleaseComObject(oFactory);

                }

                return filter;
            }
        }

		/// <summary>
		/// Gets the IBaseFilter interface for the LAVAudio filter - you must release this when finished using it with Marshal.ReleaseComObject
		/// </summary>
		/// <param name="settings">Get the Lav audio settings interface used to control the audio filter
		///  - you must release this when finished using it with Marshal.ReleaseComObject</param>
		/// <param name="status">Get the Lav audio status interface used to get info about the video filter status
		///  - you must release this when finished using it with Marshal.ReleaseComObject</param>
		/// <param name="subDir">subdirectory of your app where you store codec files (LAV*.ax) - default "codecs\"</param>
		/// <returns>LAVVideo audio to put into filterGraph</returns>
		public static IBaseFilter GetAudioFilter(out ILAVAudioSettings settings, out ILAVAudioStatus status, string subDir = @"codecs\")
		{
			lock (threadSync)
			{
				settings = null;
				status = null;
				object oFactory = null;
				object oFilter = null;
				object oSettings = null;
				object oStatus = null;
				IBaseFilter filter = null;
				string currentDir = Directory.GetCurrentDirectory();
				// we have the filters in the subdirectory 'codecs' of the running app
				string path = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), subDir);

				try
				{
					// we need to be in the filter directory since it will load a bunch 
					// of other dlls that are there, and they won't resolve otherwise
					Directory.SetCurrentDirectory(path);

					path = Path.Combine(path, "LAVAudio.ax");

					IntPtr lavVideoDll = LoadLibrary(path);
                    var r = Marshal.GetLastWin32Error();

					IntPtr proc = GetProcAddress(lavVideoDll, "DllGetClassObject");


					var getClassObject = (LavVideoDllGetClassObject)Marshal.GetDelegateForFunctionPointer(proc, typeof(LavVideoDllGetClassObject));

					int hr = getClassObject(ILAVAUDIO_GUID, IUNKNOWN_GUID, out oFactory);

					IClassFactory factory = oFactory as IClassFactory;

					if (factory == null)
					{
						if (oFactory != null) Marshal.ReleaseComObject(oFactory);
						throw new Exception("Could not QueryInterface for the IClassFactory interface");
					}

					Guid baseFilterGUID = typeof(IBaseFilter).GUID;
					hr = factory.CreateInstance(null, ref baseFilterGUID, out oFilter);

					filter = oFilter as IBaseFilter;
					if (filter == null)
					{
						if (oFilter != null) Marshal.ReleaseComObject(oFilter);
						throw new Exception("Could not QueryInterface for the IBaseFilter interface");
					}

					settings = (ILAVAudioSettings)filter;
                    status = (ILAVAudioStatus)filter;

				}
				catch
				{
					// if somehting bad happens give back the path since we will rethrow the exception ater cleanup
					Directory.SetCurrentDirectory(currentDir);

					if (oFactory != null)
						Marshal.FinalReleaseComObject(oFactory);

					if (oFilter != null)
						Marshal.FinalReleaseComObject(oFilter);

					if (oStatus != null)
						Marshal.FinalReleaseComObject(oStatus);

					if (oSettings != null)
						Marshal.FinalReleaseComObject(oSettings);

					throw;
				}
				finally
				{
					// even if nothing bad happens we need to clenup and give back to the original path
					Directory.SetCurrentDirectory(currentDir);

					if (oFactory != null)
						Marshal.FinalReleaseComObject(oFactory);

				}

				return filter;
			}
		}

		/// <summary>
		/// Gets the IFileSourceFilter interface for the LAVSplitter filter - you must release this when finished using it with Marshal.ReleaseComObject
		/// </summary>
		/// <param name="settings">Get the Lav splitter settings interface used to control the splitter filter
		///  - you must release this when finished using it with Marshal.ReleaseComObject</param>
		/// <param name="subDir">subdirectory of your app where you store codec files (LAV*.ax) - default "codecs\"</param>
		/// <returns>LAVSplitterSource filter to put into filterGraph - set the filesource on it and release when finished</returns>		
		public static IFileSourceFilter GetSplitterSource(out ILAVSplitterSettings settings, string subDir = @"codecs\")
		{
			lock (threadSync)
			{
				settings = null;
				object oFactory = null;
				object oFilter = null;
				object oSettings = null;
				IFileSourceFilter filter = null;
				string currentDir = Directory.GetCurrentDirectory();
				// we have the filters in the subdirectory 'codecs' of the running app
				string path = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), subDir);

				try
				{
					// we need to be in the filter directory since it will load a bunch 
					// of other dlls that are there, and they won't resolve otherwise
					Directory.SetCurrentDirectory(path);

					path = Path.Combine(path, "LAVSplitter.ax");

					IntPtr lavVideoDll = LoadLibrary(path);
					IntPtr proc = GetProcAddress(lavVideoDll, "DllGetClassObject");

					var getClassObject = (LavVideoDllGetClassObject)Marshal.GetDelegateForFunctionPointer(proc, typeof(LavVideoDllGetClassObject));

					int hr = getClassObject(ILAVSPLITTERSOURCE_GUID, IUNKNOWN_GUID, out oFactory);

					IClassFactory factory = oFactory as IClassFactory;

					if (factory == null)
					{
						if (oFactory != null) Marshal.ReleaseComObject(oFactory);
						throw new Exception("Could not QueryInterface for the IClassFactory interface");
					}

					Guid baseFilterGUID = typeof(IFileSourceFilter).GUID;
					hr = factory.CreateInstance(null, ref baseFilterGUID, out oFilter);

					filter = oFilter as IFileSourceFilter;
					if (filter == null)
					{
						if (oFilter != null) Marshal.ReleaseComObject(oFilter);
						throw new Exception("Could not QueryInterface for the IFileSourceFilter interface");
					}

					settings = (ILAVSplitterSettings)filter;

				}
				catch
				{
					// if somehting bad happens give back the path since we will rethrow the exception ater cleanup
					Directory.SetCurrentDirectory(currentDir);

					if (oFactory != null)
						Marshal.FinalReleaseComObject(oFactory);

					if (oFilter != null)
						Marshal.FinalReleaseComObject(oFilter);

					if (oSettings != null)
						Marshal.FinalReleaseComObject(oSettings);

					throw;
				}
				finally
				{
					// even if nothing bad happens we need to clenup and give back to the original path
					Directory.SetCurrentDirectory(currentDir);

					if (oFactory != null)
						Marshal.FinalReleaseComObject(oFactory);

				}

				return filter;
			}
		}

		/// <summary>
		/// Gets the IBaseFilter interface for the LAVSplitter filter - you must release this when finished using it with Marshal.ReleaseComObject
		/// </summary>
		/// <param name="settings">Get the Lav splitter settings interface used to control the splitter filter
		///  - you must release this when finished using it with Marshal.ReleaseComObject</param>
		/// <param name="subDir">subdirectory of your app where you store codec files (LAV*.ax) - default "codecs\"</param>
		/// <returns>LAVSplitter filter to put into filterGraph - release when finished</returns>				
		public static IBaseFilter GetSplitter(out ILAVSplitterSettings settings, string subDir = @"codecs\")
		{
			lock (threadSync)
			{
				settings = null;
				object oFactory = null;
				object oFilter = null;
				object oSettings = null;
				IBaseFilter filter = null;
				string currentDir = Directory.GetCurrentDirectory();
				// we have the filters in the subdirectory 'codecs' of the running app
				string path = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), subDir);

				try
				{
					// we need to be in the filter directory since it will load a bunch 
					// of other dlls that are there, and they won't resolve otherwise
					Directory.SetCurrentDirectory(path);

					path = Path.Combine(path, "LAVSplitter.ax");

					IntPtr lavVideoDll = LoadLibrary(path);
					IntPtr proc = GetProcAddress(lavVideoDll, "DllGetClassObject");

					var getClassObject = (LavVideoDllGetClassObject)Marshal.GetDelegateForFunctionPointer(proc, typeof(LavVideoDllGetClassObject));

					int hr = getClassObject(ILAVSPLITTER_GUID, IUNKNOWN_GUID, out oFactory);

					IClassFactory factory = oFactory as IClassFactory;

					if (factory == null)
					{
						if (oFactory != null) Marshal.ReleaseComObject(oFactory);
						throw new Exception("Could not QueryInterface for the IClassFactory interface");
					}

					Guid baseFilterGUID = typeof(IBaseFilter).GUID;
					hr = factory.CreateInstance(null, ref baseFilterGUID, out oFilter);

					filter = oFilter as IBaseFilter;
					if (filter == null)
					{
						if (oFilter != null) Marshal.ReleaseComObject(oFilter);
						throw new Exception("Could not QueryInterface for the IBaseFilter interface");
					}

					settings = (ILAVSplitterSettings)filter;

				}
				catch
				{
					// if somehting bad happens give back the path since we will rethrow the exception ater cleanup
					Directory.SetCurrentDirectory(currentDir);

					if (oFactory != null)
						Marshal.FinalReleaseComObject(oFactory);

					if (oFilter != null)
						Marshal.FinalReleaseComObject(oFilter);

					if (oSettings != null)
						Marshal.FinalReleaseComObject(oSettings);

					throw;
				}
				finally
				{
					// even if nothing bad happens we need to clenup and give back to the original path
					Directory.SetCurrentDirectory(currentDir);

					if (oFactory != null)
						Marshal.FinalReleaseComObject(oFactory);

				}

				return filter;
			}
		}
	}

	#endregion

}
