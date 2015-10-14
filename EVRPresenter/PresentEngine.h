#pragma once
#include "EVRPresenter.h"
#include <d3dx9tex.h>
#include <D3Dcompiler.h>
#include <comutil.h>
#include <string>
#include <D3D9Types.h>
#include "..\..\..\Skilja\ALPR\ALPR\Skilja.PR.API.Native\PlateRecognition.h"

//-----------------------------------------------------------------------------
// D3DPresentEngine class
//
// This class creates the Direct3D device, allocates Direct3D surfaces for
// rendering, and presents the surfaces. This class also owns the Direct3D
// device manager and provides the IDirect3DDeviceManager9 interface via
// GetService.
//
// The goal of this class is to isolate the EVRCustomPresenter class from
// the details of Direct3D as much as possible.
//-----------------------------------------------------------------------------

const DWORD D3DFVF_TLVERTEX = D3DFVF_XYZRHW | D3DFVF_DIFFUSE | D3DFVF_TEX1;

//Custom vertex
struct TLVERTEX
{
    float x;
    float y;
    float z;
    float rhw;
    D3DCOLOR colour;
    float u;
    float v;
};


typedef HRESULT(__stdcall* PTR_DXVA2CreateDirect3DDeviceManager9)(UINT* pResetToken, IDirect3DDeviceManager9** ppDeviceManager);

// evr.dll
typedef HRESULT(__stdcall* PTR_MFCreateDXSurfaceBuffer)(REFIID riid, IUnknown* punkSurface, BOOL fBottomUpWhenLinear, IMFMediaBuffer** ppBuffer);
typedef HRESULT(__stdcall* PTR_MFCreateVideoSampleFromSurface)(IUnknown* pUnkSurface, IMFSample** ppSample);
typedef HRESULT(__stdcall* PTR_MFCreateVideoMediaType)(const MFVIDEOFORMAT* pVideoFormat, IMFVideoMediaType** ppIVideoMediaType);

class D3DPresentEngine : public SchedulerCallback
{
public:

    // State of the Direct3D device.
    enum DeviceState
    {
        DeviceOK,
        DeviceReset,    // The device was reset OR re-created.
        DeviceRemoved,  // The device was removed.
    };

    D3DPresentEngine(HRESULT& hr, IDeviceResetCallback *drC);
    virtual ~D3DPresentEngine();

    // GetService: Returns the IDirect3DDeviceManager9 interface.
    // (The signature is identical to IMFGetService::GetService but 
    // this object does not derive from IUnknown.)
    virtual HRESULT GetService(REFGUID guidService, REFIID riid, void** ppv);
    virtual HRESULT CheckFormat(D3DFORMAT format);

    // Video window / destination rectangle:
    // This object implements a sub-set of the functions defined by the 
    // IMFVideoDisplayControl interface. However, some of the method signatures 
    // are different. The presenter's implementation of IMFVideoDisplayControl 
    // calls these methods.
    HRESULT SetVideoWindow(HWND hwnd);
    HWND    GetVideoWindow() const { return m_hwnd; }
    HRESULT SetDestinationRect(const RECT& rcDest);
    RECT    GetDestinationRect() const { return m_rcDestRect; };
	int     GetSampleWidth() const { return m_SampleWidth; }; 
	int     GetSampleHeight() const { return m_SampleHeight; }; 

    HRESULT CreateVideoSamples(IMFMediaType *pFormat, VideoSampleList& videoSampleQueue);
    void    ReleaseResources();

    HRESULT CheckDeviceState(DeviceState *pState);
    HRESULT PresentSample(IMFSample* pSample, LONGLONG llTarget, LONGLONG timeDelta, LONGLONG remainingInQueue, LONGLONG frameDurationDiv4); 

    UINT    RefreshRate() const { return m_DisplayMode.RefreshRate; }

	HRESULT RegisterCallback(IEVRPresenterCallback *pCallback);

	HRESULT SetBufferCount(int bufferCount);
	HRESULT GetDirect3DDevice(LPDIRECT3DDEVICE9 *device);
	HRESULT SetPixelShader(BSTR code, std::wstring &errors);
	HRESULT HookEVR(IBaseFilter *evr);
	bool Setup(int w, int h);
	HRESULT SetDeviceResetCallback(IDeviceResetCallback *pCallback);
	IDirect3DDeviceManager9* GetManager();
	HRESULT SetAdapter(POINT p);
	
	void BlitD3D (RECT *rDest, D3DCOLOR vertexColour, float rotate);

	IDirect3DDevice9            *m_pDevice;
	IEVRPresenterCallback		*m_pCallback;

#ifdef ALPR
	void AlprProcess(IMFSample *pSample);
#endif

protected:
	HRESULT InitializeD3D();
    HRESULT GetSwapChainPresentParameters(IMFMediaType *pType, D3DPRESENT_PARAMETERS* pPP);
	HRESULT CreateD3DDevice();
	HRESULT CreateD3DSample(IDirect3DSwapChain9 *pSwapChain, IMFSample **ppVideoSample);
	HRESULT SetUpCamera();

	// A derived class can override these handlers to allocate any additional D3D resources.
	virtual HRESULT OnCreateVideoSamples(D3DPRESENT_PARAMETERS& pp) { return S_OK; }
	virtual void	OnReleaseResources() { }

    virtual HRESULT PresentSwapChain(IDirect3DSwapChain9* pSwapChain, IDirect3DSurface9* pSurface);

protected:
	
#ifdef ALPR
	void ALPR(IDirect3DSurface9* pD3DSurface);
	ProcessorHandle _pProcessor;
#endif

	IDeviceResetCallback		*m_pDeviceResetCallback;
    UINT                        m_DeviceResetToken;     // Reset token for the D3D device manager.
	int							m_bufferCount;
	int							m_SampleWidth;
	int							m_SampleHeight;

    HWND                        m_hwnd;                 // Application-provided destination window.
	RECT						m_rcDestRect;           // Destination rectangle.
    D3DDISPLAYMODE              m_DisplayMode;          // Adapter's display mode.
	LPCSTR						m_ShaderCode;
    CritSec                     m_ObjectLock;           // Thread lock for the D3D device.
	LPD3DXEFFECT                m_pEffect;
	POINT						m_AdapterPoint;

    // COM interfaces
    IDirect3D9Ex                *m_pD3D9;

    IDirect3DDeviceManager9     *m_pDeviceManager;        // Direct3D device manager.
    IDirect3DSurface9           *m_pSurfaceRepaint;       // Surface for repaint requests.
	IDirect3DSurface9			*m_pRenderSurface;

	int m_DroppedFrames;
	int m_GoodFrames;
	int m_FramesInQueue;
	double m_AvgTimeDelta;

	HMODULE m_hDXVA2Lib;
    HMODULE m_hEVRLib;

    PTR_DXVA2CreateDirect3DDeviceManager9 pfDXVA2CreateDirect3DDeviceManager9;
    PTR_MFCreateDXSurfaceBuffer           pfMFCreateDXSurfaceBuffer;
    PTR_MFCreateVideoSampleFromSurface    pfMFCreateVideoSampleFromSurface;
    PTR_MFCreateVideoMediaType            pfMFCreateVideoMediaType;

	IDirect3DPixelShader9* MultiTexPS;
	IDirect3DVertexBuffer9* QuadVB;
	IDirect3DTexture9* BaseTex;
	IDirect3DTexture9* SpotLightTex;
	IDirect3DTexture9* StringTex;

	ID3DXFont* pFont;
	ID3DXFont* pFontBig;
	LicencePlateOCRResultHandle res;
	IDirect3DVertexBuffer9* vertexBuffer;
	IDirect3DTexture9 *tex;
	unsigned char *transparentYellow;
};