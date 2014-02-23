using System;
using System.Runtime.InteropServices;
using System.Security;

// Interfaces match LAVFilters release 0.60.1

namespace MediaPoint.Common.Interfaces.LavAudio
{
    #region "Lav Audio settings and status interfaces, implemented by LavAudio"

    [ComImport, Guid("E8E73B6B-4CB3-44A4-BE99-4F7BCB96E491")]
    public class LAVAudio
    {
        // com class
    }

    // LAV mixing flags
    [Flags]
    public enum LavMixingFlags
    {
        UntouchedStereo = 0x0001,
        NormalizeMatrix = 0x0002,
        ClipProtection = 0x0004
    }

    // Codecs supported in the LAV Audio configuration
    // Codecs not listed here cannot be turned off. You can request codecs to be added to this list, if you wish.
    public enum LavCodec
    {
        AAC,
        AC3,
        EAC3,
        DTS,
        MP2,
        MP3,
        TRUEHD,
        FLAC,
        VORBIS,
        LPCM,
        PCM,
        WAVPACK,
        TTA,
        WMA2,
        WMAPRO,
        Cook,
        RealAudio,
        WMALL,
        ALAC,
        Opus,
        AMR,
        Nellymoser,
        MSPCM,
        Truespeech,
        TAK,
    };

    // Bitstreaming Codecs supported in LAV Audio
    public enum LAVBitstreamCodec
    {
        AC3,
        EAC3,
        TRUEHD,
        DTS,
        DTSHD
    };


    // Supported Sample Formats in LAV Audio
    public enum LAVAudioSampleFormat
    {
        SampleFormat_16 = 0,
        SampleFormat_24,
        SampleFormat_32,
        SampleFormat_U8,
        SampleFormat_FP32,
        SampleFormat_Bitstream,
        SampleFormat_None = -1
    };

    // mixing modes
    public enum LAVAudioMixingMode
    {
        MatrixEncoding_None,
        MatrixEncoding_Dolby,
        MatrixEncoding_DPLII,
    };

    // lav speaker layouts
    [Flags]
    public enum LAVSpeakerLayouts
    {
        AV_CH_LAYOUT_MONO = 0x00000004,
        AV_CH_LAYOUT_STEREO = 0x00000001 | 0x00000002,
        AV_CH_LAYOUT_2_2 = AV_CH_LAYOUT_STEREO | 0x00000200 | 0x00000400,
        AV_CH_LAYOUT_5POINT1_BACK = AV_CH_LAYOUT_MONO | AV_CH_LAYOUT_STEREO | 0x00000200 | 0x00000400 | 0x00000008,
        AV_CH_LAYOUT_6POINT1 = AV_CH_LAYOUT_MONO | AV_CH_LAYOUT_STEREO | 0x00000200 | 0x00000400 | 0x00000008 | 0x00000100,
        AV_CH_LAYOUT_7POINT1 = AV_CH_LAYOUT_MONO | AV_CH_LAYOUT_STEREO | 0x00000200 | 0x00000400 | 0x00000008 | 0x00000010 | 0x00000020
    };

    // LAV Audio status interface
    [ComVisible(true), ComImport, SuppressUnmanagedCodeSecurity,
         Guid("A668B8F2-BA87-4F63-9D41-768F7DE9C50E"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ILAVAudioStatus
    {
        // Check if the given sample format is supported by the current playback chain
        [PreserveSig]
        bool IsSampleFormatSupported(LAVAudioSampleFormat sfCheck);

        // Get details about the current decoding format
        [PreserveSig]
        uint GetDecodeDetails(/*Marshal.PtrToStringAnsi()*/ out IntPtr pCodec, /*Marshal.PtrToStringAnsi()*/ out IntPtr pDecodeFormat, out int pnChannels, out int pSampleRate, out uint pChannelMask);

        // Get details about the current output format
        [PreserveSig]
        uint GetOutputDetails(/*Marshal.PtrToStringAnsi()*/ out IntPtr pOutputFormat, out int pnChannels, out int pSampleRate, out uint pChannelMask);

        // Enable Volume measurements
        [PreserveSig]
        uint EnableVolumeStats();

        // Disable Volume measurements
        [PreserveSig]
        uint DisableVolumeStats();

        // Get Volume Average for the given channel
        [PreserveSig]
        uint GetChannelVolumeAverage(ushort nChannel, out float pfDb);
    };

    // LAV Audio configuration interface
    [ComVisible(true), ComImport, SuppressUnmanagedCodeSecurity,
         Guid("4158A22B-6553-45D0-8069-24716F8FF171"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ILAVAudioSettings
    {
        // Switch to Runtime Config mode. This will reset all settings to default, and no changes to the settings will be saved
        // You can use this to programmatically configure LAV Audio without interfering with the users settings in the registry.
        // Subsequent calls to this function will reset all settings back to defaults, even if the mode does not change.
        //
        // Note that calling this function during playback is not supported and may exhibit undocumented behaviour. 
        // For smooth operations, it must be called before LAV Audio is connected to other filters.
        [PreserveSig]
        uint SetRuntimeConfig(bool bRuntimeConfig);

        // Dynamic Range Compression
        // pbDRCEnabled: The state of DRC
        // piDRCLevel:   The DRC strength (0-100, 100 is maximum)
        [PreserveSig]
        uint GetDRC(out bool pbDRCEnabled, out int piDRCLevel);
        [PreserveSig]
        uint SetDRC(bool bDRCEnabled, int iDRCLevel);

        // Configure which codecs are enabled
        // If aCodec is invalid (possibly a version difference), Get will return FALSE, and Set E_FAIL.
        [PreserveSig]
        bool GetFormatConfiguration(LavCodec aCodec);
        [PreserveSig]
        uint SetFormatConfiguration(LavCodec aCodec, bool bEnabled);

        // Control Bitstreaming
        // If bsCodec is invalid (possibly a version difference), Get will return FALSE, and Set E_FAIL.
        [PreserveSig]
        bool GetBitstreamConfig(LAVBitstreamCodec bsCodec);
        [PreserveSig]
        uint SetBitstreamConfig(LAVBitstreamCodec bsCodec, bool bEnabled);

        // Should "normal" DTS frames be encapsulated in DTS-HD frames when bitstreaming?
        [PreserveSig]
        bool GetDTSHDFraming();
        [PreserveSig]
        uint SetDTSHDFraming(bool bHDFraming);

        // Control Auto A/V syncing
        [PreserveSig]
        bool GetAutoAVSync();
        [PreserveSig]
        uint SetAutoAVSync(bool bAutoSync);

        // Convert all Channel Layouts to standard layouts
        // Standard are: Mono, Stereo, 5.1, 6.1, 7.1
        [PreserveSig]
        bool GetOutputStandardLayout();
        [PreserveSig]
        uint SetOutputStandardLayout(bool bStdLayout);

        // Expand Mono to Stereo by simply doubling the audio
        [PreserveSig]
        bool GetExpandMono();
        [PreserveSig]
        uint SetExpandMono(bool bExpandMono);

        // Expand 6.1 to 7.1 by doubling the back center
        [PreserveSig]
        bool GetExpand61();
        [PreserveSig]
        uint SetExpand61(bool bExpand61);

        // Allow Raw PCM and SPDIF encoded input
        [PreserveSig]
        bool GetAllowRawSPDIFInput();
        [PreserveSig]
        uint SetAllowRawSPDIFInput(bool bAllow);

        // Configure which sample formats are enabled
        // Note: SampleFormat_Bitstream cannot be controlled by this
        [PreserveSig]
        bool GetSampleFormat(LAVAudioSampleFormat format);
        [PreserveSig]
        uint SetSampleFormat(LAVAudioSampleFormat format, bool bEnabled);

        // Configure a delay for the audio
        [PreserveSig]
        uint GetAudioDelay(out bool pbEnabled, out int pDelay);
        [PreserveSig]
        uint SetAudioDelay(bool bEnabled, int delay);

        // Enable/Disable Mixing
        [PreserveSig]
        uint SetMixingEnabled(bool bEnabled);
        [PreserveSig]
        bool GetMixingEnabled();

        // Control Mixing Layout
        [PreserveSig]
        uint SetMixingLayout(LAVSpeakerLayouts dwLayout);
        [PreserveSig]
        LAVSpeakerLayouts GetMixingLayout();

        // Set Mixing Flags
        [PreserveSig]
        uint SetMixingFlags(LavMixingFlags dwFlags);
        [PreserveSig]
        LavMixingFlags GetMixingFlags();

        // Set Mixing Mode
        [PreserveSig]
        uint SetMixingMode(LAVAudioMixingMode mixingMode);
        LAVAudioMixingMode GetMixingMode();

        // Set Mixing Levels
        [PreserveSig]
        uint SetMixingLevels(uint dwCenterLevel, uint dwSurroundLevel, uint dwLFELevel);
        [PreserveSig]
        uint GetMixingLevels(out uint dwCenterLevel, out uint dwSurroundLevel, out uint dwLFELevel);

        // Toggle Tray Icon
        [PreserveSig]
        uint SetTrayIcon(bool bEnabled);
        [PreserveSig]
        bool GetTrayIcon();

        // Toggle Dithering for sample format conversion
        [PreserveSig]
        uint SetSampleConvertDithering(bool bEnabled);
        [PreserveSig]
        bool GetSampleConvertDithering();

    }

    #endregion
}
