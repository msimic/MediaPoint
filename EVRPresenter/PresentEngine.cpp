#include "atlbase.h"
#include "EVRPresenter.h"
#include <d3dx9tex.h>
#include <D3Dcompiler.h>
#include <comutil.h>
#include <fstream>
#include <string>
#include "PresentEngine.h"
#include <chrono>
#include <sstream>
#include "IPinHook.h"
#include <iostream>

HRESULT FindAdapter(IDirect3D9 *pD3D9, HMONITOR hMonitor, UINT *puAdapterID);
BOOL IsVistaOrLater();

//-----------------------------------------------------------------------------
// Constructor
//-----------------------------------------------------------------------------

D3DPresentEngine::D3DPresentEngine(HRESULT& hr, IDeviceResetCallback *drC) : 
    m_hwnd(NULL),
    m_DeviceResetToken(0),
    m_pD3D9(NULL),
    m_pDevice(NULL),
    m_pDeviceManager(NULL),
    m_pSurfaceRepaint(NULL),
	m_pCallback(NULL),
	m_pRenderSurface(NULL),
	m_bufferCount(4)
{
	res = NULL;
	SetRectEmpty(&m_rcDestRect);

    ZeroMemory(&m_DisplayMode, sizeof(m_DisplayMode));
	m_ShaderCode = NULL;
	m_SampleWidth = -1;
	m_SampleHeight = -1;
	m_pEffect = NULL;
	
	m_DroppedFrames = 0;
	m_GoodFrames = 0;
	m_FramesInQueue = 0;
	m_AvgTimeDelta = 0;
	
	pFont = NULL;
	pFontBig = NULL;
	m_pDeviceResetCallback = drC;
	vertexBuffer = NULL;
	tex = NULL;

#ifdef ALPR

	_pProcessor = CreateProcessor(L"Sample License", L"Skilja GmbH", L"2016-07-12", L"qkyckptvsjfxjgchughothxnjbykfokgyrdquxgg", L"4f754a4372868e8c230c971597c18dfa8849eb06", Nationality_Norway, L"", 1);
	if (_pProcessor == NULL)
	{
		throw std::exception("ALPR License expired");
	}

#endif

	/*transparentYellow = new unsigned char[4];
	transparentYellow[0] = 127;
	transparentYellow[1] = 255;
	transparentYellow[2] = 255;
	transparentYellow[3] = 0;*/

    hr = InitializeD3D();

    if (SUCCEEDED(hr))
    {
       hr = CreateD3DDevice();
    }
}

void D3DPresentEngine::BlitD3D (RECT *rDest, D3DCOLOR vertexColour, float rotate)
{
	TLVERTEX* vertices;

	//Lock the vertex buffer
	vertexBuffer->Lock(0, 0, (void **)&vertices, NULL);

	//Setup vertices
	//A -0.5f modifier is applied to vertex coordinates to match texture and screen coords
	//Some drivers may compensate for this automatically, but on others texture alignment errors are introduced
	//More information on this can be found in the Direct3D 9 documentation
	vertices[0].colour = vertexColour;
	vertices[0].x = (float) rDest->left - 0.5f;
	vertices[0].y = (float) rDest->top - 0.5f;
	vertices[0].z = 0.0f;
	vertices[0].rhw = 1.0f;
	vertices[0].u = 0.0f;
	vertices[0].v = 0.0f;

	vertices[1].colour = vertexColour;
	vertices[1].x = (float) rDest->right - 0.5f;
	vertices[1].y = (float) rDest->top - 0.5f;
	vertices[1].z = 0.0f;
	vertices[1].rhw = 1.0f;
	vertices[1].u = 1.0f;
	vertices[1].v = 0.0f;

	vertices[2].colour = vertexColour;
	vertices[2].x = (float) rDest->right - 0.5f;
	vertices[2].y = (float) rDest->bottom - 0.5f;
	vertices[2].z = 0.0f;
	vertices[2].rhw = 1.0f;
	vertices[2].u = 1.0f;
	vertices[2].v = 1.0f;

	vertices[3].colour = vertexColour;
	vertices[3].x = (float) rDest->left - 0.5f;
	vertices[3].y = (float) rDest->bottom - 0.5f;
	vertices[3].z = 0.0f;
	vertices[3].rhw = 1.0f;
	vertices[3].u = 0.0f;
	vertices[3].v = 1.0f;

  //Handle rotation
  if (rotate != 0)
  {
      RECT rOrigin;
      float centerX, centerY;

      //Find center of destination rectangle
      centerX = (float)(rDest->left + rDest->right) / 2;
      centerY = (float)(rDest->top + rDest->bottom) / 2;

      //Translate destination rect to be centered on the origin
      rOrigin.top = rDest->top - (int)(centerY);
      rOrigin.bottom = rDest->bottom - (int)(centerY);
      rOrigin.left = rDest->left - (int)(centerX);
      rOrigin.right = rDest->right - (int)(centerX);

	  int index = 0;
      //Rotate vertices about the origin
      vertices[index].x = rOrigin.left * cosf(rotate) -
                                rOrigin.top * sinf(rotate);
      vertices[index].y = rOrigin.left * sinf(rotate) +
                                rOrigin.top * cosf(rotate);

      vertices[index + 1].x = rOrigin.right * cosf(rotate) -
                                    rOrigin.top * sinf(rotate);
      vertices[index + 1].y = rOrigin.right * sinf(rotate) +
                                    rOrigin.top * cosf(rotate);

      vertices[index + 2].x = rOrigin.right * cosf(rotate) -
                                    rOrigin.bottom * sinf(rotate);
      vertices[index + 2].y = rOrigin.right * sinf(rotate) +
                                    rOrigin.bottom * cosf(rotate);

      vertices[index + 3].x = rOrigin.left * cosf(rotate) -
                                    rOrigin.bottom * sinf(rotate);
      vertices[index + 3].y = rOrigin.left * sinf(rotate) +
                                    rOrigin.bottom * cosf(rotate);

      //Translate vertices to proper position
      vertices[index].x += centerX;
      vertices[index].y += centerY;
      vertices[index + 1].x += centerX;
      vertices[index + 1].y += centerY;
      vertices[index + 2].x += centerX;
      vertices[index + 2].y += centerY;
      vertices[index + 3].x += centerX;
      vertices[index + 3].y += centerY;
  }

	//Unlock the vertex buffer
	vertexBuffer->Unlock();
	//m_pDevice->SetTexture (0, tex);
	//Draw image
	m_pDevice->DrawPrimitive (D3DPT_TRIANGLEFAN, 0, 2);
}

//-----------------------------------------------------------------------------
// Destructor
//-----------------------------------------------------------------------------

D3DPresentEngine::~D3DPresentEngine()
{
	SAFE_RELEASE(vertexBuffer);
    SAFE_RELEASE(m_pDevice);
    SAFE_RELEASE(m_pSurfaceRepaint);
    SAFE_RELEASE(m_pDeviceManager);
    SAFE_RELEASE(m_pD3D9);
	SAFE_RELEASE(m_pCallback);
	SAFE_RELEASE(m_pRenderSurface);
	SAFE_RELEASE(pFont);
	SAFE_RELEASE(pFontBig);
	if (m_hDXVA2Lib) {
        FreeLibrary(m_hDXVA2Lib);
    }
    if (m_hEVRLib) {
        FreeLibrary(m_hEVRLib);
    }
#ifdef ALPR

	_pProcessor->Release();

#endif

}


//-----------------------------------------------------------------------------
// GetService
//
// Returns a service interface from the presenter engine.
// The presenter calls this method from inside it's implementation of 
// IMFGetService::GetService.
//
// Classes that derive from D3DPresentEngine can override this method to return 
// other interfaces. If you override this method, call the base method from the 
// derived class.
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::GetService(REFGUID guidService, REFIID riid, void** ppv)
{
    assert(ppv != NULL);

    HRESULT hr = S_OK;

	if (MR_VIDEO_ACCELERATION_SERVICE == guidService || MR_VIDEO_RENDER_SERVICE == guidService)
	{
		if (riid == __uuidof(IDirect3DDeviceManager9) && NULL != m_pDeviceManager)
		{
			*ppv = m_pDeviceManager;
			m_pDeviceManager->AddRef();
		}
		/*else
		{
			hr = NonDelegatingQueryInterface(riid, ppvObject);
		}*/
	}
	else if (riid == __uuidof(IDirect3DDeviceManager9))
    {
        if (m_pDeviceManager == NULL)
        {
            hr = MF_E_UNSUPPORTED_SERVICE;
        }
        else
        {
            *ppv = m_pDeviceManager;
            m_pDeviceManager->AddRef();
        }
    }
    else
    {
        hr = MF_E_UNSUPPORTED_SERVICE;
    }

    return hr;
}


//-----------------------------------------------------------------------------
// CheckFormat
//
// Queries whether the D3DPresentEngine can use a specified Direct3D format.
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::CheckFormat(D3DFORMAT format)
{
    HRESULT hr = S_OK;

    UINT uAdapter = D3DADAPTER_DEFAULT;
    D3DDEVTYPE type = D3DDEVTYPE_HAL;

    D3DDISPLAYMODE mode;
    D3DDEVICE_CREATION_PARAMETERS params;

    if (m_pDevice)
    {
        CHECK_HR(hr = m_pDevice->GetCreationParameters(&params));

        uAdapter = params.AdapterOrdinal;
        type = params.DeviceType;

    }

    CHECK_HR(hr = m_pD3D9->GetAdapterDisplayMode(uAdapter, &mode));

    CHECK_HR(hr = m_pD3D9->CheckDeviceType(uAdapter, type, mode.Format, format, TRUE)); 

done:
    return hr;
}



//-----------------------------------------------------------------------------
// SetVideoWindow
// 
// Sets the window where the video is drawn.
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::SetVideoWindow(HWND hwnd)
{
    // Assertions: EVRCustomPresenter checks these cases.
    assert(IsWindow(hwnd));
    assert(hwnd != m_hwnd);     

    HRESULT hr = S_OK;

    AutoLock lock(m_ObjectLock);

    m_hwnd = hwnd;

    // Recreate the device.
    hr = CreateD3DDevice();

    return hr;
}

//-----------------------------------------------------------------------------
// SetDestinationRect
// 
// Sets the region within the video window where the video is drawn.
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::SetDestinationRect(const RECT& rcDest)
{
    if (EqualRect(&rcDest, &m_rcDestRect))
    {
        return S_OK; // No change.
    }

    HRESULT hr = S_OK;

    AutoLock lock(m_ObjectLock);

    m_rcDestRect = rcDest;

    return hr;
}

//-----------------------------------------------------------------------------
// CreateVideoSamples
// 
// Creates video samples based on a specified media type.
// 
// pFormat: Media type that describes the video format.
// videoSampleQueue: List that will contain the video samples.
//
// Note: For each video sample, the method creates a swap chain with a
// single back buffer. The video sample object holds a pointer to the swap
// chain's back buffer surface. The mixer renders to this surface, and the
// D3DPresentEngine renders the video frame by presenting the swap chain.
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::CreateVideoSamples(
    IMFMediaType *pFormat, 
    VideoSampleList& videoSampleQueue
    )
{
    if (m_hwnd == NULL)
    {
        return MF_E_INVALIDREQUEST;
    }

    if (pFormat == NULL)
    {
        return MF_E_UNEXPECTED;
    }

	HRESULT hr = S_OK;
	D3DPRESENT_PARAMETERS pp;

    IDirect3DSwapChain9 *pSwapChain = NULL;    // Swap chain
	IMFSample *pVideoSample = NULL;            // Sampl
	
    AutoLock lock(m_ObjectLock);

    ReleaseResources();

    // Get the swap chain parameters from the media type.
    CHECK_HR(hr = GetSwapChainPresentParameters(pFormat, &pp));

	if(m_pRenderSurface)
	{
		SAFE_RELEASE(m_pRenderSurface);
	}

	// Create the video samples.
    for (int i = 0; i < m_bufferCount; i++)
    {
        // Create a new swap chain.
        CHECK_HR(hr = m_pDevice->CreateAdditionalSwapChain(&pp, &pSwapChain));
        
        // Create the video sample from the swap chain.
        CHECK_HR(hr = CreateD3DSample(pSwapChain, &pVideoSample));

        // Add it to the list.
		CHECK_HR(hr = videoSampleQueue.InsertBack(pVideoSample));

        // Set the swap chain pointer as a custom attribute on the sample. This keeps
        // a reference count on the swap chain, so that the swap chain is kept alive
        // for the duration of the sample's lifetime.
        CHECK_HR(hr = pVideoSample->SetUnknown(MFSamplePresenter_SampleSwapChain, pSwapChain));

    	SAFE_RELEASE(pVideoSample);
        SAFE_RELEASE(pSwapChain);
    }

	// Let the derived class create any additional D3D resources that it needs.
    CHECK_HR(hr = OnCreateVideoSamples(pp));

done:
    if (FAILED(hr))
    {
        ReleaseResources();
    }
		
	SAFE_RELEASE(pSwapChain);
	SAFE_RELEASE(pVideoSample);
    return hr;
}



//-----------------------------------------------------------------------------
// ReleaseResources
// 
// Released Direct3D resources used by this object. 
//-----------------------------------------------------------------------------

void D3DPresentEngine::ReleaseResources()
{
    // Let the derived class release any resources it created.
	OnReleaseResources();

    SAFE_RELEASE(m_pSurfaceRepaint);
}


//-----------------------------------------------------------------------------
// CheckDeviceState
// 
// Tests the Direct3D device state.
//
// pState: Receives the state of the device (OK, reset, removed)
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::CheckDeviceState(DeviceState *pState)
{
    HRESULT hr = S_OK;

    AutoLock lock(m_ObjectLock);
	if (IsVistaOrLater())
	{
		hr = ((IDirect3DDevice9Ex*)m_pDevice)->CheckDeviceState(m_hwnd);
	}
	else
	{
		*pState = DeviceOK;
		return S_OK;
	}
	/*if(IsVistaOrLater())
	{
		// Check the device state. Not every failure code is a critical failure.
		hr = ((IDirect3DDevice9Ex*)m_pDevice)->CheckDeviceState(m_hwnd);
	}
	else
	{
		// Add support for XP!! Wait, fuck XP
		hr = m_pDevice->TestCooperativeLevel();
	}*/

    *pState = DeviceOK;
	
    switch (hr)
    {
    case S_OK:
    case S_PRESENT_OCCLUDED:
      case S_PRESENT_MODE_CHANGED:
        // state is DeviceOK
        hr = S_OK;
        break;

    case D3DERR_DEVICELOST:
    /*case D3DERR_DEVICEHUNG:
		MessageBox(0, L"D3DERR_DEVICEHUNG", L"", 0);
        // Lost/hung device. Destroy the device and create a new one.
        CHECK_HR(hr = CreateD3DDevice());
        *pState = DeviceReset;
        hr = S_OK;
        break;*/

    case D3DERR_DEVICEREMOVED:
        // This is a fatal error.
        *pState = DeviceRemoved;
        break;

    case E_INVALIDARG:
        // CheckDeviceState can return E_INVALIDARG if the window is not valid
        // We'll assume that the window was destroyed; we'll recreate the device 
        // if the application sets a new window.
        hr = S_OK;
    }

    return hr;
}

//-----------------------------------------------------------------------------
// PresentSample
//
// Presents a video frame.
//
// pSample:  Pointer to the sample that contains the surface to present. If 
//           this parameter is NULL, the method paints a black rectangle.
// llTarget: Target presentation time.
//
// This method is called by the scheduler and/or the presenter.
//-----------------------------------------------------------------------------
HRESULT D3DPresentEngine::PresentSample(IMFSample* pSample, LONGLONG llTarget, LONGLONG timeDelta, LONGLONG remainingInQueue, LONGLONG frameDurationDiv4)
{
    HRESULT hr = S_OK;

    IMFMediaBuffer* pBuffer = NULL;
    IDirect3DSurface9* pSurface = NULL;
    IDirect3DSwapChain9* pSwapChain = NULL;
	BOOL currentSampleIsTooLate = FALSE;

	m_FramesInQueue = remainingInQueue;

	double lastDelta = m_AvgTimeDelta;
	if (m_AvgTimeDelta == 0)
	{
		m_AvgTimeDelta = timeDelta;
	}
	else
	{
		m_AvgTimeDelta = (m_AvgTimeDelta + timeDelta) / 2;
	}

	if (pSample != NULL && lastDelta > m_AvgTimeDelta && (lastDelta - m_AvgTimeDelta)>frameDurationDiv4)
	{
		m_DroppedFrames++;
		currentSampleIsTooLate = TRUE;
	}
	else
	{
		m_GoodFrames++;
	}

	if (pSample && (!currentSampleIsTooLate || !m_pSurfaceRepaint))
    {
        // Get the buffer from the sample.
        CHECK_HR(hr = pSample->GetBufferByIndex(0, &pBuffer));

        // Get the surface from the buffer.
        CHECK_HR(hr = MFGetService(pBuffer, MR_BUFFER_SERVICE, __uuidof(IDirect3DSurface9), (void**)&pSurface));
    }
    else if (m_pSurfaceRepaint && !currentSampleIsTooLate)
    {
        // Redraw from the last surface.
        pSurface = m_pSurfaceRepaint;
        pSurface->AddRef();
    }

    if (pSurface)
    {
		D3DSURFACE_DESC d;
		CHECK_HR(hr = pSurface->GetDesc(&d));

		m_SampleWidth = d.Width;
		m_SampleHeight = d.Height;

        // Get the swap chain from the surface.
        CHECK_HR(hr = pSurface->GetContainer(__uuidof(IDirect3DSwapChain9), (LPVOID*)&pSwapChain));

        // Present the swap chain.
        CHECK_HR(hr = PresentSwapChain(pSwapChain, pSurface));

        // Store this pointer in case we need to repaint the surface.
        CopyComPointer(m_pSurfaceRepaint, m_pRenderSurface);
    }

done:
    SAFE_RELEASE(pSwapChain);
    SAFE_RELEASE(pSurface);
    SAFE_RELEASE(pBuffer);

    if (FAILED(hr))
    {
        if (hr == D3DERR_DEVICELOST || hr == D3DERR_DEVICENOTRESET || hr == D3DERR_DEVICEHUNG)
        {
            // Ignore. We need to reset or re-create the device, but this method
            // is probably being called from the scheduler thread, which is not the
            // same thread that created the device. The Reset(Ex) method must be
            // called from the thread that created the device.

            // The presenter will detect the state when it calls CheckDeviceState() 
            // on the next sample.
            hr = S_OK;
        }
    }
    return hr;
}

//-----------------------------------------------------------------------------
// private/protected methods
//-----------------------------------------------------------------------------


//-----------------------------------------------------------------------------
// InitializeD3D
// 
// Initializes Direct3D and the Direct3D device manager.
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::InitializeD3D()
{
    HRESULT hr = S_OK;

    assert(m_pD3D9 == NULL);
    assert(m_pDeviceManager == NULL);

    // Create Direct3D
    CHECK_HR(hr = Direct3DCreate9Ex(D3D_SDK_VERSION, &m_pD3D9));

    // Create the device manager
    CHECK_HR(hr = DXVA2CreateDirect3DDeviceManager9(&m_DeviceResetToken, &m_pDeviceManager));

done:
    return hr;
}

#define BeginEnumPins(pBaseFilter, pEnumPins, pPin)                                     \
{                                                                                       \
    CComPtr<IEnumPins> pEnumPins;                                                       \
    if (pBaseFilter && SUCCEEDED(pBaseFilter->EnumPins(&pEnumPins))) {                  \
        for (CComPtr<IPin> pPin; S_OK == pEnumPins->Next(1, &pPin, 0); pPin = nullptr) {

#define EndEnumPins }}}

IPin* GetFirstPin(IBaseFilter* pBF, PIN_DIRECTION dir)
{
    if (pBF) {
        BeginEnumPins(pBF, pEP, pPin) {
            PIN_DIRECTION dir2;
            if (SUCCEEDED(pPin->QueryDirection(&dir2)) && dir == dir2) {
                IPin* pRet = pPin.Detach();
                pRet->Release();
                return pRet;
            }
        }
        EndEnumPins;
    }

    return nullptr;
}

HRESULT D3DPresentEngine::HookEVR(IBaseFilter *evr)
{
	CComPtr<IPin> pPin = GetFirstPin(evr, PINDIR_INPUT);
    CComQIPtr<IMemInputPin> pMemInputPin = pPin;

    // No NewSegment : no chocolate :o)
    bool m_fUseInternalTimer = HookNewSegmentAndReceive((IPinC*)(IPin*)pPin, (IMemInputPinC*)(IMemInputPin*)pMemInputPin);
	return S_OK;
}

IDirect3DDeviceManager9* D3DPresentEngine::GetManager()
{
	return m_pDeviceManager;
}

HRESULT D3DPresentEngine::SetAdapter(POINT p)
{
	AutoLock lock(m_ObjectLock);

	m_AdapterPoint = p;
	HRESULT hr = S_OK;
    
    // Recreate the device.
    hr = CreateD3DDevice();

	return hr;
}

//-----------------------------------------------------------------------------
// CreateD3DDevice
// 
// Creates the Direct3D device.
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::CreateD3DDevice()
{
    HRESULT     hr = S_OK;
	//HWND        hwnd = NULL;
 //   HMONITOR    hMonitor = NULL;
 //   UINT        uAdapterID = D3DADAPTER_DEFAULT;
 //   DWORD       vp = 0;

	//MultiTexPS = 0;
	//QuadVB = 0;
	//BaseTex      = 0;
	//SpotLightTex = 0;
	//StringTex    = 0;

 //   D3DCAPS9    ddCaps;
 //   ZeroMemory(&ddCaps, sizeof(ddCaps));

 //   IDirect3DDevice9* pDevice = NULL;

 //   // Hold the lock because we might be discarding an exisiting device.
 //   AutoLock lock(m_ObjectLock);    

 //   if (!m_pD3D9 || !m_pDeviceManager)
 //   {
 //       return MF_E_NOT_INITIALIZED;
 //   }

 //   hwnd = GetDesktopWindow();

 //   // Note: The presenter creates additional swap chains to present the
 //   // video frames. Therefore, it does not use the device's implicit 
 //   // swap chain, so the size of the back buffer here is 1 x 1.

	//D3DPRESENT_PARAMETERS pp;
	//ZeroMemory(&pp, sizeof(pp));

 //   pp.BackBufferWidth = 1;
 //   pp.BackBufferHeight = 1;
 //   pp.Windowed = TRUE;
 //   pp.SwapEffect = D3DSWAPEFFECT_FLIP;
 //   pp.BackBufferFormat = D3DFMT_UNKNOWN;
 //   pp.hDeviceWindow = hwnd;
 //   pp.Flags = D3DPRESENTFLAG_VIDEO;
 //   pp.PresentationInterval = D3DPRESENT_INTERVAL_DEFAULT;

 //   // Find the monitor for this window.
 //   if (m_hwnd)
 //   {
 //       hMonitor = MonitorFromWindow(m_hwnd, MONITOR_DEFAULTTONEAREST);

 //       // Find the corresponding adapter.
 //   	CHECK_HR(hr = FindAdapter(m_pD3D9, hMonitor, &uAdapterID));
 //   }

 //   // Get the device caps for this adapter.
 //   CHECK_HR(hr = m_pD3D9->GetDeviceCaps(uAdapterID, D3DDEVTYPE_HAL, &ddCaps));

 //   if(ddCaps.DevCaps & D3DDEVCAPS_HWTRANSFORMANDLIGHT)
 //   {
 //       vp = D3DCREATE_HARDWARE_VERTEXPROCESSING;
 //   }
 //   else
 //   {
 //       vp = D3DCREATE_SOFTWARE_VERTEXPROCESSING;
 //   }

	//if(IsVistaOrLater())
	//{
	//	IDirect3DDevice9Ex * pDeviceEx;

	//	// Create the device.
	//	CHECK_HR(hr = m_pD3D9->CreateDeviceEx(uAdapterID,
	//										  D3DDEVTYPE_HAL,
	//										  pp.hDeviceWindow,
	//										  vp | D3DCREATE_FPU_PRESERVE | D3DCREATE_MULTITHREADED | D3DCREATE_ENABLE_PRESENTSTATS | D3DCREATE_NOWINDOWCHANGES,
	//									      &pp, 
	//									  	  NULL,
	//										  &pDeviceEx));
	//	pDevice = pDeviceEx;
	//}
	//else
	//{
	//	CHECK_HR(hr = m_pD3D9->CreateDevice(uAdapterID,
	//										  D3DDEVTYPE_HAL,
	//										  pp.hDeviceWindow,
	//										  vp | D3DCREATE_FPU_PRESERVE | D3DCREATE_MULTITHREADED | D3DCREATE_ENABLE_PRESENTSTATS | D3DCREATE_NOWINDOWCHANGES,
	//									      &pp, 
	//										  &pDevice));
	//}

 //   // Get the adapter display mode.
 //   CHECK_HR(hr = m_pD3D9->GetAdapterDisplayMode(uAdapterID, &m_DisplayMode));

 //   // Reset the D3DDeviceManager with the new device 
 //   CHECK_HR(hr = m_pDeviceManager->ResetDevice(pDevice, m_DeviceResetToken));

 //   SAFE_RELEASE(m_pDevice);

 //   m_pDevice = pDevice;
 //   m_pDevice->AddRef();

	//// Load EVR specific DLLs
 //   m_hDXVA2Lib = LoadLibrary(L"dxva2.dll");
 //   if (m_hDXVA2Lib) {
 //       pfDXVA2CreateDirect3DDeviceManager9 = (PTR_DXVA2CreateDirect3DDeviceManager9) GetProcAddress(m_hDXVA2Lib, "DXVA2CreateDirect3DDeviceManager9");
 //   }

 //   // Load EVR functions
 //   m_hEVRLib = LoadLibrary(L"evr.dll");
 //   if (m_hEVRLib) {
 //       pfMFCreateDXSurfaceBuffer = (PTR_MFCreateDXSurfaceBuffer) GetProcAddress(m_hEVRLib, "MFCreateDXSurfaceBuffer");
 //       pfMFCreateVideoSampleFromSurface = (PTR_MFCreateVideoSampleFromSurface) GetProcAddress(m_hEVRLib, "MFCreateVideoSampleFromSurface");
 //       pfMFCreateVideoMediaType = (PTR_MFCreateVideoMediaType) GetProcAddress(m_hEVRLib, "MFCreateVideoMediaType");
 //   }

 //   if (!pfDXVA2CreateDirect3DDeviceManager9 || !pfMFCreateDXSurfaceBuffer || !pfMFCreateVideoSampleFromSurface || !pfMFCreateVideoMediaType) {
 //       if (!pfDXVA2CreateDirect3DDeviceManager9) {
 //           //_Error += L"Could not find DXVA2CreateDirect3DDeviceManager9 (dxva2.dll)\n";
 //       }
 //       if (!pfMFCreateDXSurfaceBuffer) {
 //           //_Error += L"Could not find MFCreateDXSurfaceBuffer (evr.dll)\n";
 //       }
 //       if (!pfMFCreateVideoSampleFromSurface) {
 //           //_Error += L"Could not find MFCreateVideoSampleFromSurface (evr.dll)\n";
 //       }
 //       if (!pfMFCreateVideoMediaType) {
 //           //_Error += L"Could not find MFCreateVideoMediaType (evr.dll)\n";
 //       }
 //       hr = E_FAIL;
 //       return hr;
 //   }

	//// Init DXVA manager
 //   hr = pfDXVA2CreateDirect3DDeviceManager9(&m_DeviceResetToken, &m_pDeviceManager);
 //   if (SUCCEEDED(hr) && m_pDeviceManager) {
 //       hr = m_pDeviceManager->ResetDevice(m_pDevice, m_DeviceResetToken);
 //       if (FAILED(hr)) {
 //           //_Error += L"m_pD3DManager->ResetDevice failed\n";
 //       }

 //       CComPtr<IDirectXVideoDecoderService> pDecoderService;
 //       HANDLE hDevice;
 //       if (SUCCEEDED(m_pDeviceManager->OpenDeviceHandle(&hDevice)) &&
 //               SUCCEEDED(m_pDeviceManager->GetVideoService(hDevice, IID_PPV_ARGS(&pDecoderService)))) {
 //           //TRACE_EVR("EVR: DXVA2 : device handle = 0x%08x", hDevice);
 //           HookDirectXVideoDecoderService(pDecoderService);

 //           m_pDeviceManager->CloseDeviceHandle(hDevice);
 //       }
 //   } else {
 //       //_Error += L"DXVA2CreateDirect3DDeviceManager9 failed\n";
 //   }

	//hr = D3DXCreateFont( m_pDevice, 25, 0, FW_BOLD, 1, FALSE, DEFAULT_CHARSET,
	//						OUT_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_PITCH | FF_DONTCARE,
	//						L"Arial", &pFont );

	////return hr;

    HWND        hwnd = NULL;
    HMONITOR    hMonitor = NULL;
    UINT        uAdapterID = D3DADAPTER_DEFAULT;
    DWORD       vp = 0;

	MultiTexPS = 0;
	QuadVB = 0;
	BaseTex      = 0;
	SpotLightTex = 0;
	StringTex    = 0;

    D3DCAPS9    ddCaps;
    ZeroMemory(&ddCaps, sizeof(ddCaps));

    IDirect3DDevice9* pDevice = NULL;

    // Hold the lock because we might be discarding an exisiting device.
    AutoLock lock(m_ObjectLock);    

    if (!m_pD3D9 || !m_pDeviceManager)
    {
        return MF_E_NOT_INITIALIZED;
    }

    hwnd = GetDesktopWindow();

    // Note: The presenter creates additional swap chains to present the
    // video frames. Therefore, it does not use the device's implicit 
    // swap chain, so the size of the back buffer here is 1 x 1.

	D3DPRESENT_PARAMETERS pp;
	ZeroMemory(&pp, sizeof(pp));

    pp.BackBufferWidth = 1;
    pp.BackBufferHeight = 1;
    pp.Windowed = TRUE;
    pp.SwapEffect = /*D3DSWAPEFFECT_DISCARD; */ D3DSWAPEFFECT_FLIP;
    pp.BackBufferFormat = D3DFMT_A8R8G8B8; // D3DFMT_UNKNOWN;
	pp.MultiSampleQuality = 0;
	pp.MultiSampleType = D3DMULTISAMPLE_NONE;
    pp.hDeviceWindow = hwnd;
    pp.Flags = D3DPRESENTFLAG_VIDEO;
    pp.PresentationInterval = D3DPRESENT_INTERVAL_DEFAULT;

    // Find the monitor for this window.
    if (m_hwnd)
    {
        hMonitor = MonitorFromWindow(m_hwnd, MONITOR_DEFAULTTONEAREST);

        // Find the corresponding adapter.
    	CHECK_HR(hr = FindAdapter(m_pD3D9, hMonitor, &uAdapterID));
    }

	/*DWORD ql;
	hr = m_pD3D9->CheckDeviceMultiSampleType(uAdapterID, D3DDEVTYPE_HAL, D3DFMT_A8R8G8B8, true, D3DMULTISAMPLE_4_SAMPLES, &ql);
	pp.MultiSampleQuality = 4;
*/
    // Get the device caps for this adapter.
    CHECK_HR(hr = m_pD3D9->GetDeviceCaps(uAdapterID, D3DDEVTYPE_HAL, &ddCaps));

    if(ddCaps.DevCaps & D3DDEVCAPS_HWTRANSFORMANDLIGHT)
    {
        vp = D3DCREATE_HARDWARE_VERTEXPROCESSING;
    }
    else
    {
        vp = D3DCREATE_SOFTWARE_VERTEXPROCESSING;
    }

	if(IsVistaOrLater())
	{
		IDirect3DDevice9Ex * pDeviceEx;

		// Create the device.
		CHECK_HR(hr = m_pD3D9->CreateDeviceEx(uAdapterID,
											  D3DDEVTYPE_HAL,
											  pp.hDeviceWindow,
											  vp | D3DCREATE_FPU_PRESERVE | D3DCREATE_MULTITHREADED | D3DCREATE_ENABLE_PRESENTSTATS | D3DCREATE_NOWINDOWCHANGES,
										      &pp, 
										  	  NULL,
											  &pDeviceEx));
		pDevice = pDeviceEx;
	}
	else
	{
		CHECK_HR(hr = m_pD3D9->CreateDevice(uAdapterID,
											  D3DDEVTYPE_HAL,
											  pp.hDeviceWindow,
											  vp | D3DCREATE_FPU_PRESERVE | D3DCREATE_MULTITHREADED | D3DCREATE_ENABLE_PRESENTSTATS | D3DCREATE_NOWINDOWCHANGES,
										      &pp, 
											  &pDevice));
	}

	SAFE_RELEASE(vertexBuffer);
	CHECK_HR(pDevice->SetVertexShader(NULL));
    CHECK_HR(pDevice->SetFVF (D3DFVF_TLVERTEX));

    //Create vertex buffer and set as stream source
    CHECK_HR(pDevice->CreateVertexBuffer(sizeof(TLVERTEX) * 4, NULL, D3DFVF_TLVERTEX, D3DPOOL_DEFAULT, &vertexBuffer, NULL));
    CHECK_HR(pDevice->SetStreamSource (0, vertexBuffer, 0, sizeof(TLVERTEX)));


    // Get the adapter display mode.
    CHECK_HR(hr = m_pD3D9->GetAdapterDisplayMode(uAdapterID, &m_DisplayMode));

    // Reset the D3DDeviceManager with the new device 
    CHECK_HR(hr = m_pDeviceManager->ResetDevice(pDevice, m_DeviceResetToken));

    SAFE_RELEASE(m_pDevice);

    m_pDevice = pDevice;
    m_pDevice->AddRef();

	if (pFont != NULL)
	{
		SAFE_RELEASE(pFont);
	}

	if (pFontBig != NULL)
	{
		SAFE_RELEASE(pFontBig);
	}

	hr = D3DXCreateFont( m_pDevice, 12, 0, FW_NORMAL, 1, FALSE, DEFAULT_CHARSET,
							OUT_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_PITCH | FF_DONTCARE,
							L"Arial", &pFont );

	hr = D3DXCreateFont( m_pDevice, 20, 0, FW_NORMAL, 1, FALSE, DEFAULT_CHARSET,
						OUT_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_PITCH | FF_DONTCARE,
						L"Impact", &pFontBig );

	//hr = m_pDevice->CreateTexture(1,1,1,0,D3DFMT_A8R8G8B8, D3DPOOL_SYSTEMMEM, &tex, (HANDLE*)&transparentYellow);
	
done:
	SAFE_RELEASE(pDevice);

	if (m_pDeviceResetCallback != NULL)
	{
		m_pDeviceResetCallback->DeviceReset();
	}

	return hr;
}

//-----------------------------------------------------------------------------
// CreateD3DSample
//
// Creates an sample object (IMFSample) to hold a Direct3D swap chain.
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::CreateD3DSample(IDirect3DSwapChain9 *pSwapChain, IMFSample **ppVideoSample)
{
    // Caller holds the object lock.

	HRESULT hr = S_OK;
    D3DCOLOR clrBlack = D3DCOLOR_ARGB(0xFF, 0x00, 0x00, 0x00);

    IDirect3DSurface9* pSurface = NULL;
    IMFSample* pSample = NULL;

    // Get the back buffer surface.
	CHECK_HR(hr = pSwapChain->GetBackBuffer(0, D3DBACKBUFFER_TYPE_MONO, &pSurface));

    // Fill it with black.
	CHECK_HR(hr = m_pDevice->ColorFill(pSurface, NULL, clrBlack));

    // Create the sample.
    CHECK_HR(hr = MFCreateVideoSampleFromSurface(pSurface, &pSample));

    // Return the pointer to the caller.
	*ppVideoSample = pSample;
	(*ppVideoSample)->AddRef();

done:
    SAFE_RELEASE(pSurface);
    SAFE_RELEASE(pSample);
	return hr;
}

HRESULT D3DPresentEngine::SetPixelShader(BSTR code, std::wstring &ErrorString) {

	_bstr_t bs(code);
	char * shader = bs;
	LPD3DXEFFECT effect;
	LPD3DXBUFFER pErrors;
	HRESULT hr = D3DXCreateEffect(m_pDevice, (LPCVOID)shader, bs.length(), NULL, NULL, 0, NULL, &effect, &pErrors);
	if (hr == 0)
	{
		m_pEffect = effect;
	}
	else
	{
		if(pErrors)
		{
			char *ErrorMessage = (char *)pErrors->GetBufferPointer();
			DWORD ErrorLength = pErrors->GetBufferSize();

#if _DEBUG
			std::ofstream file("ShaderDebug.txt");
#endif
			for(UINT i = 0; i < ErrorLength; i++)
			{
#if _DEBUG
				file << ErrorMessage[i];
#endif
				ErrorString.append(std::wstring(1, ErrorMessage[i]));
			}
#if _DEBUG
			file.close();
#endif
		}
		m_pEffect = NULL;
	}
	return hr;
}


struct PPVERT
{
	float x, y, z, rhw;
	float tu, tv;       // Texcoord for post-process source
	float tu2, tv2;     // Texcoord for the original scene

	const static D3DVERTEXELEMENT9 Decl[4];
};

// Vertex declaration for post-processing
const D3DVERTEXELEMENT9 PPVERT::Decl[4] =
{
    { 0, 0,  D3DDECLTYPE_FLOAT4, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_POSITIONT, 0 },
    { 0, 16, D3DDECLTYPE_FLOAT2, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_TEXCOORD,  0 },
    { 0, 24, D3DDECLTYPE_FLOAT2, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_TEXCOORD,  1 },
    D3DDECL_END()
};

typedef std::chrono::high_resolution_clock Clock;
typedef std::chrono::milliseconds milliseconds;
Clock::time_point lastFPSCheck;
long framesRendered = 0;
LPCWSTR _lastFPSString = NULL;

void SaveBitmapToFile( BYTE* pBitmapBits, LONG lWidth, LONG lHeight,WORD wBitsPerPixel, LPCTSTR lpszFileName )
{
    RGBQUAD palette[256];
    for(int i = 0; i < 256; ++i)
    {
        palette[i].rgbBlue = (byte)i;
        palette[i].rgbGreen = (byte)i;
        palette[i].rgbRed = (byte)i;
    }

    BITMAPINFOHEADER bmpInfoHeader = {0};
    // Set the size
    bmpInfoHeader.biSize = sizeof(BITMAPINFOHEADER);
    // Bit count
    bmpInfoHeader.biBitCount = wBitsPerPixel;
    // Use all colors
    bmpInfoHeader.biClrImportant = 0;
    // Use as many colors according to bits per pixel
    bmpInfoHeader.biClrUsed = 0;
    // Store as un Compressed
    bmpInfoHeader.biCompression = BI_RGB;
    // Set the height in pixels
    bmpInfoHeader.biHeight = lHeight;
    // Width of the Image in pixels
    bmpInfoHeader.biWidth = lWidth;
    // Default number of planes
    bmpInfoHeader.biPlanes = 1;
    // Calculate the image size in bytes
    bmpInfoHeader.biSizeImage = lWidth* lHeight * (wBitsPerPixel/8);

    BITMAPFILEHEADER bfh = {0};
    // This value should be values of BM letters i.e 0x4D42
    // 0x4D = M 0×42 = B storing in reverse order to match with endian

    bfh.bfType = 'B'+('M' << 8);
    // <<8 used to shift ‘M’ to end

    // Offset to the RGBQUAD
    bfh.bfOffBits = sizeof(BITMAPINFOHEADER) + sizeof(BITMAPFILEHEADER) + sizeof(RGBQUAD) * 256;
    // Total size of image including size of headers
    bfh.bfSize = bfh.bfOffBits + bmpInfoHeader.biSizeImage;
    // Create the file in disk to write
    HANDLE hFile = CreateFile( lpszFileName,GENERIC_WRITE, 0,NULL,
        CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL,NULL);

    if( !hFile ) // return if error opening file
    {
        return;
    }

    DWORD dwWritten = 0;
    // Write the File header
    WriteFile( hFile, &bfh, sizeof(bfh), &dwWritten , NULL );
    // Write the bitmap info header
    WriteFile( hFile, &bmpInfoHeader, sizeof(bmpInfoHeader), &dwWritten, NULL );
    // Write the palette
    WriteFile( hFile, &palette[0], sizeof(RGBQUAD) * 256, &dwWritten, NULL );
    // Write the RGB Data
    if(lWidth%4 == 0)
    {
        WriteFile( hFile, pBitmapBits, bmpInfoHeader.biSizeImage, &dwWritten, NULL );
    }
    else
    {
        char* empty = new char[ 4 - lWidth % 4];
        for(int i = 0; i < lHeight; ++i)
        {
            WriteFile( hFile, &pBitmapBits[i * lWidth], lWidth, &dwWritten, NULL );
            WriteFile( hFile, empty,  4 - lWidth % 4, &dwWritten, NULL );
        }
    }
    // Close the file handle
    CloseHandle( hFile );
}

//-----------------------------------------------------------------------------
// PresentSwapChain
//
// Presents a swap chain that contains a video frame by doing a callback
// via the sink.
//
// pSwapChain: Pointer to the swap chain.
// pSurface: Pointer to the swap chain's back buffer surface.
//-----------------------------------------------------------------------------
HRESULT D3DPresentEngine::PresentSwapChain(IDirect3DSwapChain9* pSwapChain, IDirect3DSurface9* pSurface)
{
    HRESULT hr = S_OK;

	Clock::time_point tNow = Clock::now();
	milliseconds ms = std::chrono::duration_cast<milliseconds>(tNow - lastFPSCheck);

    if (m_hwnd == NULL)
    {
        return MF_E_INVALIDREQUEST;
    }

	if(!m_pRenderSurface)
	{
		D3DSURFACE_DESC desc;
		
		// Get the surface description
		pSurface->GetDesc(&desc);

		// Create a surface the same size as our sample
		hr = this->m_pDevice->CreateRenderTarget(desc.Width, 
												 desc.Height, 
												 desc.Format, 
												 desc.MultiSampleType, 
												 desc.MultiSampleQuality, 
												 true, 
												 &m_pRenderSurface, 
												 NULL);
		if(hr != S_OK)
			goto bottom;
	}

	if(m_pRenderSurface)
	{
		D3DSURFACE_DESC originalDesc;
		// Get the surface description of this sample
		pSurface->GetDesc(&originalDesc);

		D3DSURFACE_DESC renderDesc;
		// Get the surface description of the render surface
		m_pRenderSurface->GetDesc(&renderDesc);

		// Compare the descriptions to make sure they match
		if(originalDesc.Width != renderDesc.Width || 
		   originalDesc.Height != renderDesc.Height ||
		   originalDesc.Format != renderDesc.Format)
		{
			// Release the old render surface
			SAFE_RELEASE(m_pRenderSurface);
			
			// Create a new render surface that matches the size of this surface 
			hr = this->m_pDevice->CreateRenderTarget(originalDesc.Width, 
													 originalDesc.Height, 
													 originalDesc.Format, 
													 originalDesc.MultiSampleType, 
													 originalDesc.MultiSampleQuality, 
													 true, 
													 &m_pRenderSurface, 
													 NULL);

		if(hr != S_OK)
			goto bottom;
		}
	}

	if(m_pRenderSurface)
	{	
		//if (m_pEffect == NULL) 
		//{
		//	// Copy the passed surface to our rendered surface
		//	hr = D3DXLoadSurfaceFromSurface(m_pRenderSurface,
		//									NULL,
		//									NULL,
		//									pSurface,
		//									NULL,
		//									NULL,
		//									D3DX_DEFAULT,
		//									0);
		//}
		//else
		//{
			D3DSURFACE_DESC renderDesc;
			// Get the surface description of the render surface
			m_pRenderSurface->GetDesc(&renderDesc);
			PPVERT Quad[4] =
			{
				{ -0.5f,                        -0.5f,                         1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f },
				{ renderDesc.Width - 0.5f, -0.5,                          1.0f, 1.0f, 1.0f, 0.0f, 1.0f, 0.0f },
				{ -0.5,                         renderDesc.Height - 0.5f, 1.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f },
				{ renderDesc.Width - 0.5f, renderDesc.Height - 0.5f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f }
			};
			IDirect3DVertexBuffer9* pVB;
			hr = m_pDevice->CreateVertexBuffer( sizeof( PPVERT ) * 4,
												 D3DUSAGE_WRITEONLY | D3DUSAGE_DYNAMIC,
												 0,
												 D3DPOOL_DEFAULT,
												 &pVB,
												 NULL );
			LPVOID pVBData;
			if( SUCCEEDED( pVB->Lock( 0, 0, &pVBData, D3DLOCK_DISCARD ) ) )
			{
				CopyMemory( pVBData, Quad, sizeof( Quad ) );
				pVB->Unlock();
			}
			if( SUCCEEDED( m_pDevice->BeginScene() ) )
			{
				D3DXMATRIX Identity;
				D3DXMatrixIdentity(&Identity);
				hr = m_pDevice->SetTransform(D3DTS_WORLD, &Identity);
				hr = m_pDevice->SetTransform(D3DTS_PROJECTION, &Identity);
				hr = m_pDevice->SetTransform(D3DTS_VIEW, &Identity);
				D3DVIEWPORT9 view;
				view.X = 0;
                view.Y = 0;
                view.Width = renderDesc.Width;
                view.Height = renderDesc.Height;
                view.MinZ = 0;
                view.MaxZ = 1;
				hr = m_pDevice->SetViewport(&view);

				IDirect3DVertexDeclaration9* vDecl;
				if( FAILED( hr = m_pDevice->CreateVertexDeclaration( (D3DVERTEXELEMENT9*)PPVERT::Decl, &vDecl ) ) )
				{
					return hr;
				}
				// Set the vertex declaration
				hr = m_pDevice->SetVertexDeclaration( vDecl );
				SAFE_RELEASE( vDecl );

				RECT r;
				r.top = 0; r.left = 0;
				r.bottom = renderDesc.Height;
				r.right = renderDesc.Width;

				if (m_pEffect != NULL) 
				{
					hr = m_pEffect->SetTechnique( "PostProcess" );
					UINT cPasses, p;
					hr = m_pEffect->Begin( &cPasses, 0 );
					IDirect3DTexture9* pTexture;
					IDirect3DTexture9* pTexture2;
					hr = m_pDevice->CreateTexture(renderDesc.Width, renderDesc.Height, 1, D3DUSAGE_RENDERTARGET, renderDesc.Format, D3DPOOL_DEFAULT, &pTexture, NULL);
					hr = m_pDevice->CreateTexture(renderDesc.Width, renderDesc.Height, 1, D3DUSAGE_RENDERTARGET, renderDesc.Format, D3DPOOL_DEFAULT, &pTexture2, NULL);
					LPDIRECT3DSURFACE9 tempSurf;
					LPDIRECT3DSURFACE9 tempSurf2;
					hr = pTexture->GetSurfaceLevel(0,&tempSurf);
					hr = pTexture2->GetSurfaceLevel(0,&tempSurf2);
					//hr = D3DXLoadSurfaceFromSurface(tempSurf,NULL,NULL,pSurface,NULL,NULL,D3DX_FILTER_NONE,0);

					//hr = m_pDevice->Clear( 0L, NULL, D3DCLEAR_TARGET, D3DCOLOR_ARGB(255, 0, 0, 0), 1.0f, 0L );
					//hr = m_pDevice->StretchRect(pSurface, &r, m_pRenderSurface, &r, D3DTEXTUREFILTERTYPE::D3DTEXF_LINEAR);
					hr = m_pDevice->StretchRect(pSurface, &r, tempSurf, &r, D3DTEXTUREFILTERTYPE::D3DTEXF_LINEAR);
					//hr = m_pDevice->StretchRect(m_pRenderSurface, &r, tempSurf, &r, D3DTEXTUREFILTERTYPE::D3DTEXF_LINEAR);
					//D3DXLoadSurfaceFromSurface(m_pRenderSurface,NULL,NULL,pSurface,NULL,NULL,D3DX_FILTER_NONE,0);
					D3DXHANDLE hTxt = m_pEffect->GetParameterElement( NULL, 0);
					hr = m_pDevice->SetRenderTarget( 0, tempSurf2 );
					hr = m_pDevice->SetTexture(0, pTexture);
					//hr = m_pDevice->SetRenderTarget( 0, tempSurf );
					//hr = m_pEffect->SetTexture(hTxt, pTexture );

					for( p = 0; p < cPasses; ++p )
					{
						hr = m_pEffect->BeginPass( p );
						hr = m_pDevice->SetStreamSource( 0, pVB, 0, sizeof( PPVERT ) );
						hr = m_pDevice->DrawPrimitive( D3DPT_TRIANGLESTRIP, 0, 2 );
						hr = m_pEffect->EndPass();
						hr = m_pDevice->StretchRect(tempSurf2, &r, tempSurf, &r, D3DTEXTUREFILTERTYPE::D3DTEXF_LINEAR);
					}
					hr = m_pEffect->End();
					hr = m_pDevice->SetTexture(0, NULL);
					hr = m_pDevice->StretchRect(tempSurf2, &r, m_pRenderSurface, &r, D3DTEXTUREFILTERTYPE::D3DTEXF_LINEAR);

					SAFE_RELEASE( tempSurf );
					SAFE_RELEASE( tempSurf2 );
					SAFE_RELEASE( pTexture );
					SAFE_RELEASE( pTexture2 );
				}
				else
				{
		
					//GetBytesFromSurface(pSurface);

					//hr = m_pDevice->StretchRect(pSurface, &r, m_pRenderSurface, &r, D3DTEXTUREFILTERTYPE::D3DTEXF_LINEAR);
					hr = D3DXLoadSurfaceFromSurface(m_pRenderSurface,
													NULL,
													NULL,
													pSurface,
													NULL,
													NULL,
													D3DX_DEFAULT,
													0);
				}

				hr = m_pDevice->SetRenderTarget( 0, m_pRenderSurface );
				framesRendered++;

				/*if (framesRendered > 2) {
					_lastFPSString = L"";
				}*/

				//if (m_pSurfaceRepaint != pSurface) {
				AutoLock lock(m_ObjectLock);
				if (false && pFont)
				{
					RECT rc;
					SetRect( &rc, 50, 50, 228, 112 );
					std::wstringstream stringStream;
					stringStream << " 1 Sec AVG FPS: ";
					stringStream << ((float)(framesRendered * 1000000 / max(0.00001, ms.count())))/1000;
					stringStream << "\n";
					stringStream << " Bad frames (>1/4 frame late): ";
					stringStream << m_DroppedFrames;
					stringStream << "\n";
					stringStream << " Good frames: ";
					stringStream << m_GoodFrames;
					stringStream << "\n";
					stringStream << " Cumulative frame time delta: ";
					stringStream << m_AvgTimeDelta / 10000000;
					stringStream << "\n";
					stringStream << " Samples in queue: ";
					stringStream << m_FramesInQueue;

					//LPCWSTR text = stringStream.str().c_str();
					m_pDevice->ColorFill(m_pRenderSurface, &rc, D3DCOLOR_ARGB(255, 30, 30, 30));

					hr = pFont->DrawText( NULL, stringStream.str().c_str(), -1, &rc, DT_NOCLIP, D3DCOLOR_ARGB(255, 255, 255, 255) );
					
				}

				//if (res != NULL) 
				//{
				//	if (pFontBig && res->GetConfidence() > 200)
				//	{
				//		RECT rcL;
				//		RECT rcT;
				//		RECT rcR;
				//		RECT rcB;
				//		Shape shp = res->GetPlatePosition().Shape;
				//		SetRect( &rcL, shp.Left-5, shp.Top-5, shp.Left, shp.Bottom+5);
				//		SetRect( &rcR, shp.Right, shp.Top-5, shp.Right+5, shp.Bottom+5);
				//		SetRect( &rcT, shp.Left-5, shp.Top-5, shp.Right+5, shp.Top);
				//		SetRect( &rcB, shp.Left-5, shp.Bottom, shp.Right+5, shp.Bottom+5);
				//		m_pDevice->ColorFill(m_pRenderSurface, &rcL, D3DCOLOR_ARGB(127, 255,255,0));
				//		m_pDevice->ColorFill(m_pRenderSurface, &rcR, D3DCOLOR_ARGB(127, 255,255,0));
				//		m_pDevice->ColorFill(m_pRenderSurface, &rcT, D3DCOLOR_ARGB(127, 255,255,0));
				//		m_pDevice->ColorFill(m_pRenderSurface, &rcB, D3DCOLOR_ARGB(127, 255,255,0));
				//		//Draw texture
				//		
				//		//BlitD3D (&rc, 0xFFFF00FF, D3DXToRadian(res->GetPlatePosition().Shape.AngleDetectedVertical));
				//		//std::wstringstream stringStream;
				//		//stringStream << L"" << res->GetText() << L"";
				//		//hr = pFontBig->DrawText( NULL, stringStream.str().c_str(), -1, &rc, DT_NOCLIP, D3DCOLOR_ARGB(255, 0, 0, 0) );
				//	}
				//}
				//else
				//{
				//	if (pFont)
				//	{
				//		RECT rc;
				//		SetRect( &rc, renderDesc.Width - 100, 10, renderDesc.Width - 10, 30 );
				//		std::wstringstream stringStream;
				//		stringStream << " NO ALPR RESULT! ";
				//		hr = pFont->DrawText( NULL, stringStream.str().c_str(), -1, &rc, DT_NOCLIP, D3DCOLOR_ARGB(255, 255, 255, 255) );
				//	}
				//}

				lock.Unlock();
				//}
				hr = m_pDevice->EndScene();

				//hr = m_pDevice->StretchRect(m_pRenderSurface, &r, pSurface, &r, D3DTEXTUREFILTERTYPE::D3DTEXF_LINEAR);

			/*}*/
			hr = S_OK;
		}

	}
	else
	{
		hr = S_FALSE;
	}

	if(hr == S_OK)
	{
		// Do the callback, passing the rendered surface
		if(m_pCallback)
			hr = m_pCallback->PresentSurfaceCB(m_pRenderSurface);

		if (ms.count() > 1000) 
		{
			framesRendered = 0;
			lastFPSCheck = tNow;
		}

		LOG_MSG_IF_FAILED(L"D3DPresentEngine::PresentSwapChain failed.", hr);
	}

bottom:
    return hr;
}

HRESULT GetD3DSurfaceFromSample(IMFSample *pSample, IDirect3DSurface9 **ppSurface)
{
    *ppSurface = NULL;

    IMFMediaBuffer *pBuffer = NULL;

    HRESULT hr = pSample->GetBufferByIndex(0, &pBuffer);
    if (SUCCEEDED(hr))
    {
        hr = MFGetService(pBuffer, MR_BUFFER_SERVICE, IID_PPV_ARGS(ppSurface));
        pBuffer->Release();
    }

    return hr;
}

#ifdef ALPR

void D3DPresentEngine::AlprProcess(IMFSample *pSample)
{
	IDirect3DSurface9* pSurface;
	GetD3DSurfaceFromSample(pSample, &pSurface);
	ALPR(pSurface);
}

void D3DPresentEngine::ALPR(IDirect3DSurface9* pD3DSurface)
{
	//return; 

	D3DSURFACE_DESC surfaceDesc;
	pD3DSurface->GetDesc(&surfaceDesc);
	D3DLOCKED_RECT d3dlr;
	BYTE  *pSurfaceBuffer;
	HRESULT hr;

	IDirect3DSurface9* offscreenSurfaceOrig;
	hr = m_pDevice->CreateOffscreenPlainSurface(surfaceDesc.Width, surfaceDesc.Height, surfaceDesc.Format, D3DPOOL_SYSTEMMEM, &offscreenSurfaceOrig, NULL);
	if (FAILED(hr))
	{
		return;
	}

	hr = m_pDevice->GetRenderTargetData(pD3DSurface, offscreenSurfaceOrig);

	int newHeight = surfaceDesc.Height;
	int newWidth = surfaceDesc.Width;

	//int newHeight = min(surfaceDesc.Height, 440);
	//float ratio = (float)newHeight / surfaceDesc.Height;
	//int newWidth = (int)(surfaceDesc.Width * ratio);

	//IDirect3DSurface9* resizedRenderTarget;

	////hr = m_pDevice->CreateRenderTarget(newWidth, newHeight, surfaceDesc.Format, D3DMULTISAMPLE_NONE, 0, true, &resizedRenderTarget, NULL);
	//hr = m_pDevice->CreateOffscreenPlainSurface(newWidth, newHeight, surfaceDesc.Format, D3DPOOL_SYSTEMMEM, &resizedRenderTarget, NULL);
	//if (FAILED(hr))
	//{
	//	return;
	//}

	//IDirect3DSurface9* offscreenSurface;

	//hr = m_pDevice->CreateOffscreenPlainSurface(newWidth, newHeight, surfaceDesc.Format, D3DPOOL_SYSTEMMEM, &offscreenSurface, NULL);
	//if (FAILED(hr))
	//{
	//	resizedRenderTarget->Release();
	//	return;
	//}

	//RECT r1; r1.left = 0; r1.top = 0; r1.right = surfaceDesc.Width; r1.bottom = surfaceDesc.Height;
	//RECT r2; r2.left = 0; r2.top = 0; r2.right = newWidth; r2.bottom = newHeight;

	//m_pDevice->SetRenderTarget(0, offscreenSurfaceOrig);
	//hr = m_pDevice->StretchRect(offscreenSurfaceOrig, NULL, resizedRenderTarget, NULL, D3DTEXF_NONE);

	//if (FAILED(hr))
	//{
	//	offscreenSurfaceOrig->Release();
	//	resizedRenderTarget->Release();
	//	offscreenSurface->Release();
	//	return;
	//}

	//hr = m_pDevice->GetRenderTargetData(resizedRenderTarget, offscreenSurface);

	//if (FAILED(hr))
	//{
	//	resizedRenderTarget->Release();
	//	offscreenSurface->Release();
	//	return;
	//}

	//if (hr == D3DERR_INVALIDCALL || hr == D3DERR_WASSTILLDRAWING)
	//{
	//	offscreenSurface->Release();
	//	return;
	//}

	hr = offscreenSurfaceOrig->LockRect(&d3dlr, 0, D3DLOCK_DONOTWAIT);

	if (FAILED(hr))
	{
		offscreenSurfaceOrig->Release();
		return;
	}

	BYTE* pData = new BYTE[newWidth*newHeight];
	BYTE* pOriginal = pData;

	pSurfaceBuffer = (BYTE *) d3dlr.pBits;
	int m_lVidPitch = (newWidth * 4 + 4) & ~(4);

	for (int i=0;i<(int)newHeight;i++) 
	{
	
		BYTE *pDataOld = pData;
		BYTE *pSurfaceBufferOld = pSurfaceBuffer;

		for (int j = 0; j< (int)newWidth; j++)
		{
		
			pData[0] = (pSurfaceBuffer[0] + pSurfaceBuffer[1] + pSurfaceBuffer[2]) / 3;

			pData+=1; pSurfaceBuffer+=4;
		}
		pData = pDataOld + newWidth /**3*/;
		pSurfaceBuffer = pSurfaceBufferOld + d3dlr.Pitch;
	}
	
	offscreenSurfaceOrig->UnlockRect();

	ImageHandle img = CreateImage();
	img->Initialize(pOriginal, newWidth, newHeight, newWidth);

	AutoLock lock(m_ObjectLock);

		bool failed = false;
		try
		{
			auto dp = CreateLicencePlateDetectionParameters();
			auto rp = CreateLicencePlateRecognitionParameters();
			dp->Initialize(0, 10, 40, 300, newHeight / 3, newHeight / 10, newWidth / 10, newWidth / 10);
			dp->SetEnableRetryMechanism(false);
			dp->SetIncreasedPrecision(false);
			rp->SetEnableDebug(false);
			rp->SetIncreasedPrecision(false);
			rp->SetEnableOCRThreshold(false);
			rp->SetEnableRuleThreshold(false);
			rp->SetEnablePatternMatching(false);
			auto r = _pProcessor->DetectAndRecognizePlate(img, dp, rp);

			if (r != NULL && r->GetConfidence() >= 887 && (std::wstring(r->GetText()).size() >= 6 && std::wstring(r->GetText()).size() <= 8))
			{
				Shape s = r->GetPlatePosition().Shape;
				const wchar_t* txt = r->GetText();
				int l = s.Left;
				int t = s.Top;
				int rr = s.Right;
				int b = s.Bottom;
				int c = r->GetConfidence();
				float a = s.AngleDetectedVertical;
				const wchar_t nonat[] = L"";
				int natconf = 0;
				const wchar_t* nat = nonat;
				const wchar_t* natplate = nonat;
				if (r->GetNationalities().Count > 0)
				{
					nat = r->GetNationalities().Items[0].CountryCode;
					natconf = r->GetNationalities().Items[0].NationalityConfidence;
					natplate = r->GetNationalities().Items[0].OCRText;
				}
				else
				{

				}
				if (m_pCallback != NULL) m_pCallback->FoundPlate(txt, l, t, rr, b, a, c, nat, natconf, natplate); 
			}
			else
			{
				r->Release();
			}
			dp->Release();
			rp->Release();
		}
		catch (std::exception& e)
		{
			if (m_pCallback != NULL) m_pCallback->FoundPlate(L"std::exception", 0, 0, 0, 0, 0, 1000, L"", 0, L"");
		}
		catch (...)
		{
			if (m_pCallback != NULL) m_pCallback->FoundPlate(L"exception", 0, 0, 0, 0, 0, 1000, L"", 0, L"");
		}

	lock.Unlock();
		
	//offscreenSurface->Release();
	offscreenSurfaceOrig->Release();
	//resizedRenderTarget->Release();

	img->Release();

	delete[] pOriginal;
}

#endif

//-----------------------------------------------------------------------------
// GetSwapChainPresentParameters
//
// Given a media type that describes the video format, fills in the
// D3DPRESENT_PARAMETERS for creating a swap chain.
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::GetSwapChainPresentParameters(IMFMediaType *pType, D3DPRESENT_PARAMETERS* pPP)
{
    // Caller holds the object lock.

    HRESULT hr = S_OK; 

    UINT32 width = 0, height = 0;
    DWORD d3dFormat = 0;

    VideoTypeBuilder *pTypeHelper = NULL;

    if (m_hwnd == NULL)
    {
        return MF_E_INVALIDREQUEST;
    }

	ZeroMemory(pPP, sizeof(D3DPRESENT_PARAMETERS));

    // Create the helper object for reading the proposed type.
    CHECK_HR(hr = MediaTypeBuilder::Create(pType, &pTypeHelper));

    // Get some information about the video format.
    CHECK_HR(hr = pTypeHelper->GetFrameDimensions(&width, &height));
    CHECK_HR(hr = pTypeHelper->GetFourCC(&d3dFormat));

    ZeroMemory(pPP, sizeof(D3DPRESENT_PARAMETERS));
    pPP->BackBufferWidth = width;
    pPP->BackBufferHeight = height;
    pPP->Windowed = TRUE;
    pPP->SwapEffect = D3DSWAPEFFECT_COPY;
    pPP->BackBufferFormat = (D3DFORMAT)d3dFormat;
    pPP->hDeviceWindow = m_hwnd;
    pPP->Flags = D3DPRESENTFLAG_VIDEO;
    pPP->PresentationInterval = D3DPRESENT_INTERVAL_DEFAULT;

    D3DDEVICE_CREATION_PARAMETERS params;
    CHECK_HR(hr = m_pDevice->GetCreationParameters(&params));
    
    if (params.DeviceType != D3DDEVTYPE_HAL)
    {
        pPP->Flags |= D3DPRESENTFLAG_LOCKABLE_BACKBUFFER;
    }

done:
    SAFE_RELEASE(pTypeHelper);
    return S_OK;
}

HRESULT D3DPresentEngine::GetDirect3DDevice(LPDIRECT3DDEVICE9 *device)
{
	*device = m_pDevice;
	return S_OK;
}

HRESULT D3DPresentEngine::SetDeviceResetCallback(IDeviceResetCallback *pCallback)
{
	if(m_pDeviceResetCallback)
	{
		SAFE_RELEASE(m_pDeviceResetCallback);
	}

	m_pDeviceResetCallback = pCallback;

	if(m_pDeviceResetCallback)
		m_pDeviceResetCallback->AddRef();

	return S_OK;
}

//-----------------------------------------------------------------------------
// RegisterCallback
//
// Registers a callback sink for getting the D3D surface and
// is called for every video frame that needs to be rendered
//-----------------------------------------------------------------------------
HRESULT D3DPresentEngine::RegisterCallback(IEVRPresenterCallback *pCallback)
{
	if(m_pCallback)
	{
		SAFE_RELEASE(m_pCallback);
	}

	m_pCallback = pCallback;

	if(m_pCallback)
		m_pCallback->AddRef();

	return S_OK;
}

//-----------------------------------------------------------------------------
// SetBufferCount
//
// Sets the total number of buffers to use when the EVR
// custom presenter is running.
//-----------------------------------------------------------------------------
HRESULT D3DPresentEngine::SetBufferCount(int bufferCount)
{
	m_bufferCount = bufferCount;
	return S_OK;
}

//-----------------------------------------------------------------------------
// Static functions
//-----------------------------------------------------------------------------
BOOL IsVistaOrLater()
{
	OSVERSIONINFOW info;
	
	GetVersionEx(&info);

	if(info.dwMajorVersion >= 6)
		return true;
	else
		return false;
}

//-----------------------------------------------------------------------------
// FindAdapter
//
// Given a handle to a monitor, returns the ordinal number that D3D uses to 
// identify the adapter.
//-----------------------------------------------------------------------------

HRESULT FindAdapter(IDirect3D9 *pD3D9, HMONITOR hMonitor, UINT *puAdapterID)
{
	HRESULT hr = E_FAIL;
	UINT cAdapters = 0;
	UINT uAdapterID = (UINT)-1;

	cAdapters = pD3D9->GetAdapterCount();
	for (UINT i = 0; i < cAdapters; i++)
	{
        HMONITOR hMonitorTmp = pD3D9->GetAdapterMonitor(i);

        if (hMonitorTmp == NULL)
        {
            break;
        }
        if (hMonitorTmp == hMonitor)
        {
            uAdapterID = i;
            break;
        }
	}

	if (uAdapterID != (UINT)-1)
	{
		*puAdapterID = uAdapterID;
		hr = S_OK;
	}
	return hr;
}
