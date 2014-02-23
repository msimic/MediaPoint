/*
 * (C) 2003-2006 Gabest
 * (C) 2006-2013 see Authors.txt
 *
 * This file is part of MPC-HC.
 *
 * MPC-HC is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 3 of the License, or
 * (at your option) any later version.
 *
 * MPC-HC is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 */

#include <tchar.h>
#include <d3dx9.h>
//#include "moreuuids.h"

#include "IPinHook.h"
//#include "AllocatorCommon.h"

#define DXVA_LOGFILE_A 0 // set to 1 for logging DXVA data to a file
#define LOG_BITSTREAM  0 // set to 1 for logging DXVA bistream data to a file
#define LOG_MATRIX     0 // set to 1 for logging DXVA matrix data to a file

#if defined(_DEBUG) && DXVA_LOGFILE_A
#define LOG_FILE_DXVA       _T("dxva_ipinhook.log")
#define LOG_FILE_PICTURE    _T("picture.log")
#define LOG_FILE_SLICELONG  _T("slicelong.log")
#define LOG_FILE_SLICESHORT _T("sliceshort.log")
#define LOG_FILE_BITSTREAM  _T("bitstream.log")
#endif

REFERENCE_TIME g_tSegmentStart = 0;
REFERENCE_TIME g_tSampleStart = 0;
GUID g_guidDXVADecoder = GUID_NULL;
int  g_nDXVAVersion = 0;

IPinCVtbl* g_pPinCVtbl = nullptr;
IMemInputPinCVtbl* g_pMemInputPinCVtbl = nullptr;

struct D3DFORMAT_TYPE {
    int Format;
    LPCTSTR Description;
};

const D3DFORMAT_TYPE D3DFormatType[] = {
    { D3DFMT_UNKNOWN       , _T("D3DFMT_UNKNOWN      ") },
    { D3DFMT_R8G8B8        , _T("D3DFMT_R8G8B8       ") },
    { D3DFMT_A8R8G8B8      , _T("D3DFMT_A8R8G8B8     ") },
    { D3DFMT_X8R8G8B8      , _T("D3DFMT_X8R8G8B8     ") },
    { D3DFMT_R5G6B5        , _T("D3DFMT_R5G6B5       ") },
    { D3DFMT_X1R5G5B5      , _T("D3DFMT_X1R5G5B5     ") },
    { D3DFMT_A1R5G5B5      , _T("D3DFMT_A1R5G5B5     ") },
    { D3DFMT_A4R4G4B4      , _T("D3DFMT_A4R4G4B4     ") },
    { D3DFMT_R3G3B2        , _T("D3DFMT_R3G3B2       ") },
    { D3DFMT_A8            , _T("D3DFMT_A8           ") },
    { D3DFMT_A8R3G3B2      , _T("D3DFMT_A8R3G3B2     ") },
    { D3DFMT_X4R4G4B4      , _T("D3DFMT_X4R4G4B4     ") },
    { D3DFMT_A2B10G10R10   , _T("D3DFMT_A2B10G10R10  ") },
    { D3DFMT_A8B8G8R8      , _T("D3DFMT_A8B8G8R8     ") },
    { D3DFMT_X8B8G8R8      , _T("D3DFMT_X8B8G8R8     ") },
    { D3DFMT_G16R16        , _T("D3DFMT_G16R16       ") },
    { D3DFMT_A2R10G10B10   , _T("D3DFMT_A2R10G10B10  ") },
    { D3DFMT_A16B16G16R16  , _T("D3DFMT_A16B16G16R16 ") },
    { D3DFMT_A8P8          , _T("D3DFMT_A8P8         ") },
    { D3DFMT_P8            , _T("D3DFMT_P8           ") },
    { D3DFMT_L8            , _T("D3DFMT_L8           ") },
    { D3DFMT_A8L8          , _T("D3DFMT_A8L8         ") },
    { D3DFMT_A4L4          , _T("D3DFMT_A4L4         ") },
    { D3DFMT_X8L8V8U8      , _T("D3DFMT_X8L8V8U8     ") },
    { D3DFMT_Q8W8V8U8      , _T("D3DFMT_Q8W8V8U8     ") },
    { D3DFMT_V16U16        , _T("D3DFMT_V16U16       ") },
    { D3DFMT_A2W10V10U10   , _T("D3DFMT_A2W10V10U10  ") },
    { D3DFMT_UYVY          , _T("D3DFMT_UYVY         ") },
    { D3DFMT_R8G8_B8G8     , _T("D3DFMT_R8G8_B8G8    ") },
    { D3DFMT_YUY2          , _T("D3DFMT_YUY2         ") },
    { D3DFMT_G8R8_G8B8     , _T("D3DFMT_G8R8_G8B8    ") },
    { D3DFMT_DXT1          , _T("D3DFMT_DXT1         ") },
    { D3DFMT_DXT2          , _T("D3DFMT_DXT2         ") },
    { D3DFMT_DXT3          , _T("D3DFMT_DXT3         ") },
    { D3DFMT_DXT4          , _T("D3DFMT_DXT4         ") },
    { D3DFMT_DXT5          , _T("D3DFMT_DXT5         ") },
    { D3DFMT_D16_LOCKABLE  , _T("D3DFMT_D16_LOCKABLE ") },
    { D3DFMT_D32           , _T("D3DFMT_D32          ") },
    { D3DFMT_D15S1         , _T("D3DFMT_D15S1        ") },
    { D3DFMT_D24S8         , _T("D3DFMT_D24S8        ") },
    { D3DFMT_D24X8         , _T("D3DFMT_D24X8        ") },
    { D3DFMT_D24X4S4       , _T("D3DFMT_D24X4S4      ") },
    { D3DFMT_D16           , _T("D3DFMT_D16          ") },
    { D3DFMT_D32F_LOCKABLE , _T("D3DFMT_D32F_LOCKABLE") },
    { D3DFMT_D24FS8        , _T("D3DFMT_D24FS8       ") },
    { D3DFMT_L16           , _T("D3DFMT_L16          ") },
    { D3DFMT_VERTEXDATA    , _T("D3DFMT_VERTEXDATA   ") },
    { D3DFMT_INDEX16       , _T("D3DFMT_INDEX16      ") },
    { D3DFMT_INDEX32       , _T("D3DFMT_INDEX32      ") },
    { D3DFMT_Q16W16V16U16  , _T("D3DFMT_Q16W16V16U16 ") },

    { MAKEFOURCC('N', 'V', '1', '2'), _T("D3DFMT_NV12") },
    { MAKEFOURCC('N', 'V', '2', '4'), _T("D3DFMT_NV24") },
};

const LPCTSTR DXVAVersion[] = { _T("DXVA "), _T("DXVA1"), _T("DXVA2") };

struct DXVA2_DECODER {
    const GUID* Guid;
    LPCTSTR Description;
};

// Additionnal DXVA GUIDs

// Intel ClearVideo VC1 bitstream decoder
DEFINE_GUID(DXVA_Intel_VC1_ClearVideo, 0xBCC5DB6D, 0xA2B6, 0x4AF0, 0xAC, 0xE4, 0xAD, 0xB1, 0xF7, 0x87, 0xBC, 0x89);

DEFINE_GUID(DXVA_Intel_VC1_ClearVideo_2, 0xE07EC519, 0xE651, 0x4CD6, 0xAC, 0x84, 0x13, 0x70, 0xCC, 0xEE, 0xC8, 0x51);

// Intel ClearVideo H264 bitstream decoder
DEFINE_GUID(DXVA_Intel_H264_ClearVideo, 0x604F8E68, 0x4951, 0x4C54, 0x88, 0xFE, 0xAB, 0xD2, 0x5C, 0x15, 0xB3, 0xD6);

// Nvidia MPEG-4 ASP bitstream decoder
// 9947EC6F-689B-11DC-A320-0019DBBC4184
DEFINE_GUID(DXVA_MPEG4_ASP, 0x9947EC6F, 0x689B, 0x11DC, 0xA3, 0x20, 0x00, 0x19, 0xDB, 0xBC, 0x41, 0x84);

DEFINE_GUID(CLSID_AC3Filter, 0xA753A1EC, 0x973E, 0x4718, 0xAF, 0x8E, 0xA3, 0xF5, 0x54, 0xD4, 0x5C, 0x44);
//
//static const DXVA2_DECODER DXVA2Decoder[] = {
//    {&GUID_NULL,                        _T("Unknown")},
//    {&GUID_NULL,                        _T("Not using DXVA")},
//    {&DXVA_Intel_H264_ClearVideo,       _T("H.264 bitstream decoder, ClearVideo(tm)")},  // Intel ClearVideo H264 bitstream decoder
//    {&DXVA_Intel_VC1_ClearVideo,        _T("VC-1 bitstream decoder, ClearVideo(tm)")},   // Intel ClearVideo VC-1 bitstream decoder
//    {&DXVA_Intel_VC1_ClearVideo_2,      _T("VC-1 bitstream decoder 2, ClearVideo(tm)")}, // Intel ClearVideo VC-1 bitstream decoder 2
//    {&DXVA_MPEG4_ASP,                   _T("MPEG-4 ASP bitstream decoder")},             // NVIDIA MPEG-4 ASP bitstream decoder
//    {&DXVA_ModeNone,                    _T("Mode none")},
//    {&DXVA_ModeH261_A,                  _T("H.261 A, post processing")},
//    {&DXVA_ModeH261_B,                  _T("H.261 B, deblocking")},
//    {&DXVA_ModeH263_A,                  _T("H.263 A, motion compensation, no FGT")},
//    {&DXVA_ModeH263_B,                  _T("H.263 B, motion compensation, FGT")},
//    {&DXVA_ModeH263_C,                  _T("H.263 C, IDCT, no FGT")},
//    {&DXVA_ModeH263_D,                  _T("H.263 D, IDCT, FGT")},
//    {&DXVA_ModeH263_E,                  _T("H.263 E, bitstream decoder, no FGT")},
//    {&DXVA_ModeH263_F,                  _T("H.263 F, bitstream decoder, FGT")},
//    {&DXVA_ModeMPEG1_A,                 _T("MPEG-1 A, post processing")},
//    {&DXVA_ModeMPEG2_A,                 _T("MPEG-2 A, motion compensation")},
//    {&DXVA_ModeMPEG2_B,                 _T("MPEG-2 B, motion compensation + blending")},
//    {&DXVA_ModeMPEG2_C,                 _T("MPEG-2 C, IDCT")},
//    {&DXVA_ModeMPEG2_D,                 _T("MPEG-2 D, IDCT + blending")},
//    {&DXVA_ModeH264_A,                  _T("H.264 A, motion compensation, no FGT")},
//    {&DXVA_ModeH264_B,                  _T("H.264 B, motion compensation, FGT")},
//    {&DXVA_ModeH264_C,                  _T("H.264 C, IDCT, no FGT")},
//    {&DXVA_ModeH264_D,                  _T("H.264 D, IDCT, FGT")},
//    {&DXVA_ModeH264_E,                  _T("H.264 E, bitstream decoder, no FGT")},
//    {&DXVA_ModeH264_F,                  _T("H.264 F, bitstream decoder, FGT")},
//    {&DXVA_ModeWMV8_A,                  _T("WMV8 A, post processing")},
//    {&DXVA_ModeWMV8_B,                  _T("WMV8 B, motion compensation")},
//    {&DXVA_ModeWMV9_A,                  _T("WMV9 A, post processing")},
//    {&DXVA_ModeWMV9_B,                  _T("WMV9 B, motion compensation")},
//    {&DXVA_ModeWMV9_C,                  _T("WMV9 C, IDCT")},
//    {&DXVA_ModeVC1_A,                   _T("VC-1 A, post processing")},
//    {&DXVA_ModeVC1_B,                   _T("VC-1 B, motion compensation")},
//    {&DXVA_ModeVC1_C,                   _T("VC-1 C, IDCT")},
//    {&DXVA_ModeVC1_D,                   _T("VC-1 D, bitstream decoder")},
//    {&DXVA_NoEncrypt,                   _T("No encryption")},
//    {&DXVA2_ModeMPEG2_MoComp,           _T("MPEG-2 motion compensation")},
//    {&DXVA2_ModeMPEG2_IDCT,             _T("MPEG-2 IDCT")},
//    {&DXVA2_ModeMPEG2_VLD,              _T("MPEG-2 variable-length decoder")},
//    {&DXVA2_ModeH264_A,                 _T("H.264 A, motion compensation, no FGT")},
//    {&DXVA2_ModeH264_B,                 _T("H.264 B, motion compensation, FGT")},
//    {&DXVA2_ModeH264_C,                 _T("H.264 C, IDCT, no FGT")},
//    {&DXVA2_ModeH264_D,                 _T("H.264 D, IDCT, FGT")},
//    {&DXVA2_ModeH264_E,                 _T("H.264 E, bitstream decoder, no FGT")},
//    {&DXVA2_ModeH264_F,                 _T("H.264 F, bitstream decoder, FGT")},
//    {&DXVA2_ModeWMV8_A,                 _T("WMV8 A, post processing")},
//    {&DXVA2_ModeWMV8_B,                 _T("WMV8 B, motion compensation")},
//    {&DXVA2_ModeWMV9_A,                 _T("WMV9 A, post processing")},
//    {&DXVA2_ModeWMV9_B,                 _T("WMV9 B, motion compensation")},
//    {&DXVA2_ModeWMV9_C,                 _T("WMV9 C, IDCT")},
//    {&DXVA2_ModeVC1_A,                  _T("VC-1 A, post processing")},
//    {&DXVA2_ModeVC1_B,                  _T("VC-1 B, motion compensation")},
//    {&DXVA2_ModeVC1_C,                  _T("VC-1 C, IDCT")},
//    {&DXVA2_ModeVC1_D,                  _T("VC-1 D, bitstream decoder")},
//    {&DXVA2_NoEncrypt,                  _T("No encryption")},
//    {&DXVA2_VideoProcProgressiveDevice, _T("Progressive scan")},
//    {&DXVA2_VideoProcBobDevice,         _T("Bob deinterlacing")},
//    {&DXVA2_VideoProcSoftwareDevice,    _T("Software processing")}
//};

LPCTSTR GetDXVAMode(const GUID* guidDecoder)
{
    /*int nPos = 0;

    for (int i = 1; i < _countof(DXVA2Decoder); i++) {
        if (*guidDecoder == *DXVA2Decoder[i].Guid) {
            nPos = i;
            break;
        }
    }*/

    return L""; // DXVA2Decoder[nPos].Description;
}

LPCTSTR GetDXVADecoderDescription()
{
    return GetDXVAMode(&g_guidDXVADecoder);
}

LPCTSTR GetDXVAVersion()
{
    return DXVAVersion[g_nDXVAVersion];
}

void ClearDXVAState()
{
    g_guidDXVADecoder = GUID_NULL;
    g_nDXVAVersion = 0;
}

LPCTSTR FindD3DFormat(const D3DFORMAT Format)
{
    for (int i = 0; i < _countof(D3DFormatType); i++) {
        if (Format == D3DFormatType[i].Format) {
            return D3DFormatType[i].Description;
        }
    }

    return D3DFormatType[0].Description;
}

// === DirectShow hooks
static HRESULT(STDMETHODCALLTYPE* NewSegmentOrg)(IPinC* This, /* [in] */ REFERENCE_TIME tStart, /* [in] */ REFERENCE_TIME tStop, /* [in] */ double dRate) = nullptr;

static HRESULT STDMETHODCALLTYPE NewSegmentMine(IPinC* This, /* [in] */ REFERENCE_TIME tStart, /* [in] */ REFERENCE_TIME tStop, /* [in] */ double dRate)
{
    g_tSegmentStart = tStart;
    return NewSegmentOrg(This, tStart, tStop, dRate);
}

static HRESULT(STDMETHODCALLTYPE* ReceiveOrg)(IMemInputPinC* This, IMediaSample* pSample) = nullptr;

static HRESULT STDMETHODCALLTYPE ReceiveMineI(IMemInputPinC* This, IMediaSample* pSample)
{
    REFERENCE_TIME rtStart, rtStop;
    if (pSample && SUCCEEDED(pSample->GetTime(&rtStart, &rtStop))) {
        g_tSampleStart = rtStart;
    }
    return ReceiveOrg(This, pSample);
}

static HRESULT STDMETHODCALLTYPE ReceiveMine(IMemInputPinC* This, IMediaSample* pSample)
{
    // Support ffdshow queueing.
    // To avoid black out on pause, we have to lock g_ffdshowReceive to synchronize with CMainFrame::OnPlayPause.
    return ReceiveMineI(This, pSample);
}

void UnhookNewSegmentAndReceive()
{
    BOOL res;
    DWORD flOldProtect = 0;

    // Casimir666 : unhook previous VTables
    if (g_pPinCVtbl && g_pMemInputPinCVtbl) {
        res = VirtualProtect(g_pPinCVtbl, sizeof(IPinCVtbl), PAGE_WRITECOPY, &flOldProtect);
        if (g_pPinCVtbl->NewSegment == NewSegmentMine) {
            g_pPinCVtbl->NewSegment = NewSegmentOrg;
        }
        res = VirtualProtect(g_pPinCVtbl, sizeof(IPinCVtbl), flOldProtect, &flOldProtect);

        res = VirtualProtect(g_pMemInputPinCVtbl, sizeof(IMemInputPinCVtbl), PAGE_WRITECOPY, &flOldProtect);
        if (g_pMemInputPinCVtbl->Receive == ReceiveMine) {
            g_pMemInputPinCVtbl->Receive = ReceiveOrg;
        }
        res = VirtualProtect(g_pMemInputPinCVtbl, sizeof(IMemInputPinCVtbl), flOldProtect, &flOldProtect);

        g_pPinCVtbl         = nullptr;
        g_pMemInputPinCVtbl = nullptr;
        NewSegmentOrg       = nullptr;
        ReceiveOrg          = nullptr;
    }
}

bool HookNewSegmentAndReceive(IPinC* pPinC, IMemInputPinC* pMemInputPinC)
{
    if (!pPinC || !pMemInputPinC) {
        return false;
    }

    g_tSegmentStart = 0;
    g_tSampleStart = 0;

    BOOL res;
    DWORD flOldProtect = 0;

    UnhookNewSegmentAndReceive();

    // Casimir666 : change sizeof(IPinC) to sizeof(IPinCVtbl) to fix crash with EVR hack on Vista!
    res = VirtualProtect(pPinC->lpVtbl, sizeof(IPinCVtbl), PAGE_WRITECOPY, &flOldProtect);
    if (NewSegmentOrg == nullptr) {
        NewSegmentOrg = pPinC->lpVtbl->NewSegment;
    }
    pPinC->lpVtbl->NewSegment = NewSegmentMine; // Function sets global variable(s)
    res = VirtualProtect(pPinC->lpVtbl, sizeof(IPinCVtbl), flOldProtect, &flOldProtect);

    // Casimir666 : change sizeof(IMemInputPinC) to sizeof(IMemInputPinCVtbl) to fix crash with EVR hack on Vista!
    res = VirtualProtect(pMemInputPinC->lpVtbl, sizeof(IMemInputPinCVtbl), PAGE_WRITECOPY, &flOldProtect);
    if (ReceiveOrg == nullptr) {
        ReceiveOrg = pMemInputPinC->lpVtbl->Receive;
    }
    pMemInputPinC->lpVtbl->Receive = ReceiveMine; // Function sets global variable(s)
    res = VirtualProtect(pMemInputPinC->lpVtbl, sizeof(IMemInputPinCVtbl), flOldProtect, &flOldProtect);

    g_pPinCVtbl = pPinC->lpVtbl;
    g_pMemInputPinCVtbl = pMemInputPinC->lpVtbl;

    return true;
}


// === DXVA1 hooks
static HRESULT(STDMETHODCALLTYPE* GetCompBufferInfoOrg)(IAMVideoAcceleratorC* This,/* [in] */ const GUID* pGuid,/* [in] */ const AMVAUncompDataInfo* pamvaUncompDataInfo,/* [out][in] */ LPDWORD pdwNumTypesCompBuffers,/* [out] */ LPAMVACompBufferInfo pamvaCompBufferInfo) = nullptr;

inline static void LOG(...) {}
inline static void LOGPF(LPCTSTR prefix, const DDPIXELFORMAT* p, int n) {}
inline static void LOGUDI(LPCTSTR prefix, const AMVAUncompDataInfo* p, int n) {}
inline static void LogDXVA_PicParams_H264(DXVA_PicParams_H264* pPic) {}
inline static void LogDXVA_PictureParameters(DXVA_PictureParameters* pPic) {}
inline static void LogDXVA_Bitstream(BYTE* pBuffer, int nSize) {}

static HRESULT STDMETHODCALLTYPE GetCompBufferInfoMine(IAMVideoAcceleratorC* This, const GUID* pGuid, const AMVAUncompDataInfo* pamvaUncompDataInfo, LPDWORD pdwNumTypesCompBuffers, LPAMVACompBufferInfo pamvaCompBufferInfo)
{
    if (pGuid) {
        g_guidDXVADecoder = *pGuid;
        g_nDXVAVersion = 1;

    }

    HRESULT hr = GetCompBufferInfoOrg(This, pGuid, pamvaUncompDataInfo, pdwNumTypesCompBuffers, pamvaCompBufferInfo);

    return hr;
}

void HookAMVideoAccelerator(IAMVideoAcceleratorC* pAMVideoAcceleratorC)
{
    g_guidDXVADecoder = GUID_NULL;
    g_nDXVAVersion = 0;

    BOOL res;
    DWORD flOldProtect = 0;
    res = VirtualProtect(pAMVideoAcceleratorC->lpVtbl, sizeof(IAMVideoAcceleratorC), PAGE_WRITECOPY, &flOldProtect);

    if (GetCompBufferInfoOrg == nullptr) {
        GetCompBufferInfoOrg = pAMVideoAcceleratorC->lpVtbl->GetCompBufferInfo;
    }

    pAMVideoAcceleratorC->lpVtbl->GetCompBufferInfo = GetCompBufferInfoMine; // Function sets global variable(s)

}


// === Hook for DXVA2

// Both IDirectXVideoDecoderServiceCVtbl and IDirectXVideoDecoderServiceC already exists in file \Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Include\dxva2api.h
// Why was the code duplicated ?
interface IDirectXVideoDecoderServiceC;
struct IDirectXVideoDecoderServiceCVtbl {
    BEGIN_INTERFACE
    HRESULT(STDMETHODCALLTYPE* QueryInterface)(IDirectXVideoDecoderServiceC* pThis, /* [in] */ REFIID riid, /* [iid_is][out] */ void** ppvObject);
    ULONG(STDMETHODCALLTYPE* AddRef)(IDirectXVideoDecoderServiceC* pThis);
    ULONG(STDMETHODCALLTYPE* Release)(IDirectXVideoDecoderServiceC*   pThis);
    HRESULT(STDMETHODCALLTYPE* CreateSurface)(
        IDirectXVideoDecoderServiceC* pThis,
        __in  UINT Width,
        __in  UINT Height,
        __in  UINT BackBuffers,
        __in  D3DFORMAT Format,
        __in  D3DPOOL Pool,
        __in  DWORD Usage,
        __in  DWORD DxvaType,
        __out_ecount(BackBuffers + 1)
        IDirect3DSurface9** ppSurface, __inout_opt  HANDLE* pSharedHandle);

    HRESULT(STDMETHODCALLTYPE* GetDecoderDeviceGuids)(
        IDirectXVideoDecoderServiceC* pThis,
        __out UINT* pCount,
        __deref_out_ecount_opt(*pCount) GUID** pGuids);

    HRESULT(STDMETHODCALLTYPE* GetDecoderRenderTargets)(
        IDirectXVideoDecoderServiceC* pThis,
        __in REFGUID Guid,
        __out UINT* pCount,
        __deref_out_ecount_opt(*pCount) D3DFORMAT** pFormats);

    HRESULT(STDMETHODCALLTYPE* GetDecoderConfigurations)(
        IDirectXVideoDecoderServiceC* pThis,
        __in REFGUID Guid,
        __in const DXVA2_VideoDesc* pVideoDesc,
        __reserved void* pReserved,
        __out UINT* pCount,
        __deref_out_ecount_opt(*pCount) DXVA2_ConfigPictureDecode** ppConfigs);

    HRESULT(STDMETHODCALLTYPE* CreateVideoDecoder)(
        IDirectXVideoDecoderServiceC* pThis,
        __in REFGUID Guid,
        __in const DXVA2_VideoDesc* pVideoDesc,
        __in const DXVA2_ConfigPictureDecode* pConfig,
        __in_ecount(NumRenderTargets) IDirect3DSurface9** ppDecoderRenderTargets,
        __in UINT NumRenderTargets,
        __deref_out IDirectXVideoDecoder** ppDecode);

    END_INTERFACE
};

interface IDirectXVideoDecoderServiceC {
    CONST_VTBL struct IDirectXVideoDecoderServiceCVtbl* lpVtbl;
};


IDirectXVideoDecoderServiceCVtbl* g_pIDirectXVideoDecoderServiceCVtbl = nullptr;
static HRESULT(STDMETHODCALLTYPE* CreateVideoDecoderOrg)(IDirectXVideoDecoderServiceC* pThis,
        __in REFGUID Guid,
        __in const DXVA2_VideoDesc* pVideoDesc,
        __in const DXVA2_ConfigPictureDecode* pConfig,
        __in_ecount(NumRenderTargets)
        IDirect3DSurface9** ppDecoderRenderTargets, __in  UINT NumRenderTargets, __deref_out  IDirectXVideoDecoder** ppDecode) = nullptr;
#ifdef _DEBUG
static HRESULT(STDMETHODCALLTYPE* GetDecoderDeviceGuidsOrg)(IDirectXVideoDecoderServiceC* pThis, __out  UINT* pCount, __deref_out_ecount_opt(*pCount)  GUID** pGuids) = nullptr;
static HRESULT(STDMETHODCALLTYPE* GetDecoderConfigurationsOrg)(IDirectXVideoDecoderServiceC* pThis, __in  REFGUID Guid, __in const DXVA2_VideoDesc* pVideoDesc, __reserved void* pReserved, __out UINT* pCount, __deref_out_ecount_opt(*pCount)  DXVA2_ConfigPictureDecode** ppConfigs) = nullptr;
#endif

static HRESULT STDMETHODCALLTYPE CreateVideoDecoderMine(
    IDirectXVideoDecoderServiceC* pThis,
    __in REFGUID Guid,
    __in const DXVA2_VideoDesc* pVideoDesc,
    __in const DXVA2_ConfigPictureDecode* pConfig,
    __in_ecount(NumRenderTargets) IDirect3DSurface9** ppDecoderRenderTargets,
    __in UINT NumRenderTargets,
    __deref_out IDirectXVideoDecoder** ppDecode)
{
    //  DebugBreak();
    //  ((DXVA2_VideoDesc*)pVideoDesc)->Format = (D3DFORMAT)0x3231564E;
    g_guidDXVADecoder = Guid;
    g_nDXVAVersion = 2;

    HRESULT hr = CreateVideoDecoderOrg(pThis, Guid, pVideoDesc, pConfig, ppDecoderRenderTargets, NumRenderTargets, ppDecode);

    if (FAILED(hr)) {
        g_guidDXVADecoder = GUID_NULL;
    }
    GetDXVADecoderDescription();

    return hr;
}

void HookDirectXVideoDecoderService(void* pIDirectXVideoDecoderService)
{
    IDirectXVideoDecoderServiceC* pIDirectXVideoDecoderServiceC = (IDirectXVideoDecoderServiceC*) pIDirectXVideoDecoderService;

    BOOL res;
    DWORD flOldProtect = 0;

    // Casimir666 : unhook previous VTables
    if (g_pIDirectXVideoDecoderServiceCVtbl) {
        res = VirtualProtect(g_pIDirectXVideoDecoderServiceCVtbl, sizeof(IDirectXVideoDecoderServiceCVtbl), PAGE_WRITECOPY, &flOldProtect);
        if (g_pIDirectXVideoDecoderServiceCVtbl->CreateVideoDecoder == CreateVideoDecoderMine) {
            g_pIDirectXVideoDecoderServiceCVtbl->CreateVideoDecoder = CreateVideoDecoderOrg;
        }

        res = VirtualProtect(g_pIDirectXVideoDecoderServiceCVtbl, sizeof(IDirectXVideoDecoderServiceCVtbl), flOldProtect, &flOldProtect);

        g_pIDirectXVideoDecoderServiceCVtbl = nullptr;
        CreateVideoDecoderOrg = nullptr;
        g_guidDXVADecoder = GUID_NULL;
        g_nDXVAVersion = 0;
    }

    if (!g_pIDirectXVideoDecoderServiceCVtbl && pIDirectXVideoDecoderService) {
        res = VirtualProtect(pIDirectXVideoDecoderServiceC->lpVtbl, sizeof(IDirectXVideoDecoderServiceCVtbl), PAGE_WRITECOPY, &flOldProtect);

        CreateVideoDecoderOrg = pIDirectXVideoDecoderServiceC->lpVtbl->CreateVideoDecoder;
        pIDirectXVideoDecoderServiceC->lpVtbl->CreateVideoDecoder = CreateVideoDecoderMine;

        res = VirtualProtect(pIDirectXVideoDecoderServiceC->lpVtbl, sizeof(IDirectXVideoDecoderServiceCVtbl), flOldProtect, &flOldProtect);

        g_pIDirectXVideoDecoderServiceCVtbl = pIDirectXVideoDecoderServiceC->lpVtbl;
    }
}
