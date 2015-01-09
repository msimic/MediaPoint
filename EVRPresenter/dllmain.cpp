#pragma managed(push, off)

#include "EVRPresenter.h"

#include <initguid.h>
#include "EVRPresenterUuid.h"
#include "WindowShadowerUuid.h"

HMODULE g_hModule;                  // DLL module handle

DEFINE_CLASSFACTORY_SERVER_LOCK;    // Defines the static member variable for the class factory lock.

// Friendly name for COM registration.
WCHAR* g_sFriendlyName =  L"EVR Custom Presenter";

// g_ClassFactories: Array of class factory data.
// Defines a look-up table of CLSIDs and corresponding creation functions.

ClassFactoryData g_ClassFactories[] =
{
    {   &CLSID_CustomEVRPresenter, EVRCustomPresenter::CreateInstance },
	{   &CLSID_WindowShadower, CWndShadow::CreateInstance }
};
      
const DWORD g_numClassFactories = ARRAY_SIZE(g_ClassFactories);

// DllMain: Entry-point for the DLL.
BOOL APIENTRY DllMain( HANDLE hModule, 
                       DWORD  ul_reason_for_call, 
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        g_hModule = (HMODULE)hModule;
        //TRACE_INIT(L"evrlog.txt");
		//TRACE_INIT(NULL);
        break;

    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
        break;

    case DLL_PROCESS_DETACH:
        //TRACE_CLOSE();
        break;
    }
    return TRUE;
}

STDAPI DllCanUnloadNow()
{
    if (!ClassFactory::IsLocked())
    {
        return S_OK;
    }
    else
    {
        return S_FALSE;
    }
}


STDAPI DllRegisterServer()
{
    HRESULT hr;
    
    // Register the MFT's CLSID as a COM object.
    hr = RegisterObject(g_hModule, CLSID_CustomEVRPresenter, g_sFriendlyName, TEXT("Both"));

    return hr;
}

STDAPI DllUnregisterServer()
{
    // Unregister the CLSID
    UnregisterObject(CLSID_CustomEVRPresenter);

    return S_OK;
}

STDAPI DllGetClassObject(REFCLSID clsid, REFIID riid, void** ppv)
{
    ClassFactory *pFactory = NULL;

    HRESULT hr = CLASS_E_CLASSNOTAVAILABLE; // Default to failure

    // Find an entry in our look-up table for the specified CLSID.
    for (DWORD index = 0; index < g_numClassFactories; index++)
    {
        if (*g_ClassFactories[index].pclsid == clsid)
        {
            // Found an entry. Create a new class factory object.
            pFactory = new ClassFactory(g_ClassFactories[index].pfnCreate);
            if (pFactory)
            {
                hr = S_OK;
            }
            else
            {
                hr = E_OUTOFMEMORY;
            }
            break;
        }
    }

    if (SUCCEEDED(hr))
    {
        hr = pFactory->QueryInterface(riid, ppv);
    }
    SAFE_RELEASE(pFactory);

    return hr;
}


#pragma managed(pop)