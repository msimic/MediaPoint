#pragma once

#include <windows.h>
#include <intsafe.h>
#include <math.h>

#include <mfapi.h>
#include <mfidl.h>
#include <mferror.h>
#include <d3d9.h>
#include <dxva2api.h>
#include <evr9.h>
#include <evcode.h> // EVR event codes (IMediaEventSink)

// Common helper code.
#define USE_LOGGING
#include "common.h"
#include "registry.h"
using namespace MediaFoundationSamples;

#define CHECK_HR(hr) IF_FAILED_GOTO(hr, done)

typedef ComPtrList<IMFSample>           VideoSampleList;

// Custom Attributes

// MFSamplePresenter_SampleCounter
// Data type: UINT32
//
// Version number for the video samples. When the presenter increments the version
// number, all samples with the previous version number are stale and should be
// discarded.
static const GUID MFSamplePresenter_SampleCounter = 
{ 0xb0bb83cc, 0xf10f, 0x4e2e, { 0xaa, 0x2b, 0x29, 0xea, 0x5e, 0x92, 0xef, 0x85 } };

// MFSamplePresenter_SampleSwapChain
// Data type: IUNKNOWN
// 
// Pointer to a Direct3D swap chain.
static const GUID MFSamplePresenter_SampleSwapChain = 
{ 0xad885bd1, 0x7def, 0x414a, { 0xb5, 0xb0, 0xd3, 0xd2, 0x63, 0xd6, 0xe9, 0x6d } };

MIDL_INTERFACE("B92D8991-6C42-4e51-B942-E61CB8696FCB")
IEVRPresenterCallback : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE PresentSurfaceCB(IDirect3DSurface9 *pSurface) = 0;
	virtual HRESULT STDMETHODCALLTYPE FoundPlate(const wchar_t* text, int left, int top, int right, int bottom, float angle, int confidence, const wchar_t* nat, int natconf, const wchar_t* natplate) = 0;
};

MIDL_INTERFACE("9019EA9C-F1B4-44b5-ADD5-D25704313E48")
IEVRPresenterRegisterCallback : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE RegisterCallback(IEVRPresenterCallback *pCallback) = 0;
};

MIDL_INTERFACE("4527B2E7-49BE-4b61-A19D-429066D93A99")
IEVRPresenterSettings : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE SetBufferCount(int bufferCount) = 0;
	virtual HRESULT STDMETHODCALLTYPE GetDirect3DDevice(LPDIRECT3DDEVICE9 *device) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetPixelShader(BSTR code, BSTR* errors) = 0;
	virtual HRESULT STDMETHODCALLTYPE HookEVR(IBaseFilter *evr) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetAdapter(POINT p) = 0;
};

MIDL_INTERFACE("452782E7-49BE-4bA1-A19D-429466D93A99")
IDeviceResetCallback : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE DeviceReset() = 0;
};

MIDL_INTERFACE("452782E7-49BE-4EA1-A19D-456466D93A99")
IWindowShadow : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE CreateForWindow(HWND hParentWnd) = 0;
	virtual HRESULT STDMETHODCALLTYPE Init(HINSTANCE hInstance) = 0;
	virtual HRESULT STDMETHODCALLTYPE UpdateWindow(HWND hParentWnd) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetShadowSize(int size) = 0;
	virtual HRESULT STDMETHODCALLTYPE Show(HWND hParentWnd) = 0;
};

// Project headers.
#include "Helpers.h"
#include "Scheduler.h"
#include "PresentEngine.h"
#include "Presenter.h"
#include "WindowShadow.h"


