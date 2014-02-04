#pragma once

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

    D3DPresentEngine(HRESULT& hr);
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
    HRESULT PresentSample(IMFSample* pSample, LONGLONG llTarget); 

    UINT    RefreshRate() const { return m_DisplayMode.RefreshRate; }

	HRESULT RegisterCallback(IEVRPresenterCallback *pCallback);

	HRESULT SetBufferCount(int bufferCount);
	HRESULT GetDirect3DDevice(LPDIRECT3DDEVICE9 *device);
	HRESULT SetPixelShader(BSTR code);
	bool Setup(int w, int h);

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
	IEVRPresenterCallback		*m_pCallback;
    UINT                        m_DeviceResetToken;     // Reset token for the D3D device manager.
	int							m_bufferCount;
	int							m_SampleWidth;
	int							m_SampleHeight;

    HWND                        m_hwnd;                 // Application-provided destination window.
	RECT						m_rcDestRect;           // Destination rectangle.
    D3DDISPLAYMODE              m_DisplayMode;          // Adapter's display mode.
	LPCSTR						m_ShaderCode;
    CritSec                     m_ObjectLock;           // Thread lock for the D3D device.

    // COM interfaces
    IDirect3D9Ex                *m_pD3D9;
    IDirect3DDevice9            *m_pDevice;
    IDirect3DDeviceManager9     *m_pDeviceManager;        // Direct3D device manager.
    IDirect3DSurface9           *m_pSurfaceRepaint;       // Surface for repaint requests.
	IDirect3DSurface9			*m_pRenderSurface;

	IDirect3DPixelShader9* MultiTexPS;
	IDirect3DVertexBuffer9* QuadVB;
	IDirect3DTexture9* BaseTex;
	IDirect3DTexture9* SpotLightTex;
	IDirect3DTexture9* StringTex;

	struct MultiTexVertex
	{
		 MultiTexVertex(float x, float y, float z,
						float u0, float v0,
						float u1, float v1,
						float u2, float v2)
		 {
			  _x =  x;   _y =  y; _z = z;
			  _u0 = u0;  _v0 = v0;
			  _u1 = u1;  _v1 = v1;
			  _u2 = u2,  _v2 = v2;
		 }

		 float _x,  _y,  _z;
		 float _u0,  _v0;
		 float _u1,  _v1;
		 float _u2,  _v2;

		 static const DWORD FVF = D3DFVF_XYZ | D3DFVF_TEX3;
	};
	
};