using MediaPoint.Common.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace MediaPoint.Common.Helpers
{
    [ComVisible(true), ComImport, SuppressUnmanagedCodeSecurity,
     Guid("452782E7-49BE-4EA1-A19D-456466D93A99"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWindowShadow
    {
        [PreserveSig]
        int CreateForWindow(IntPtr hParentWnd);
		[PreserveSig]
        int Init(IntPtr hInstance);
        [PreserveSig]
        int UpdateWindow(IntPtr hParentWnd);
        [PreserveSig]
        int SetShadowSize(int size);
        [PreserveSig]
        int Show(IntPtr hParentWnd);
        
    }

    public class WindowShadow
    {
        private WindowShadow() { }

        /// <summary>
        /// Returns the bittage of this process, ie 32 or 64 bit
        /// </summary>
        private static int ProcessBits
        {
            get { return IntPtr.Size * 8; }
        }

        public IWindowShadow Shadower { get; private set; }

        /// <summary>
        /// Create a new EVR video presenter
        /// </summary>
        /// <returns></returns>
        public static WindowShadow CreateNew()
        {
            object comObject;

            int hr;

            /* Our exception var we use to hold the exception
             * until we need to throw it (after clean up) */
            Exception exception = null;

            /* A COM object we query form our native library */
            IClassFactory factory = null;

            /* Create our 'helper' class */
            var windowShadow = new WindowShadow();

            /* Call the DLL export to create the class factory */
            if (ProcessBits == 32)
            {
                hr = DllGetClassObject32(WINDOW_SHADOW_CLSID, IUNKNOWN_GUID, out comObject);
            }
            else if (ProcessBits == 64)
            {
                hr = DllGetClassObject64(WINDOW_SHADOW_CLSID, IUNKNOWN_GUID, out comObject);
            }
            else
            {
                exception = new Exception(string.Format("{0} bit processes are unsupported", ProcessBits));
                goto bottom;
            }

            /* Check if our call to our DLL failed */
            if (hr != 0 || comObject == null)
            {
                exception = new COMException("Could not create a new class factory.", hr);
                goto bottom;
            }

            /* Cast the COM object that was returned to a COM interface type */
            factory = comObject as IClassFactory;

            if (factory == null)
            {
                exception = new Exception("Could not QueryInterface for the IClassFactory interface");
                goto bottom;
            }

            /* Get the GUID of the class */
            Guid guid = typeof(IWindowShadow).GUID;

            /* Creates a new instance of the IMFVideoPresenter */
            factory.CreateInstance(null, ref guid, out comObject);

            /* QueryInterface for the IMFVideoPresenter */
            var shadower = comObject as IWindowShadow;

            /* Populate the shadower */
            windowShadow.Shadower = shadower;

        bottom:

            if (factory != null)
                Marshal.FinalReleaseComObject(factory);

            if (exception != null)
                throw exception;

            return windowShadow;
        }

        /// <summary>
        /// The GUID of our COM object
        /// </summary>
        public static readonly Guid WINDOW_SHADOW_CLSID = new Guid(0x9807fc9c, 0x807b, 0x41e3, 0x98, 0xa8, 0x75, 0x17, 0x6f, 0x95, 0xa1, 0x53);

        /// <summary>
        /// The GUID of IUnknown
        /// </summary>
        public static readonly Guid IUNKNOWN_GUID = new Guid("{00000000-0000-0000-C000-000000000046}");

        /// <summary>
        /// Static method in the 32 bit dll to create our IClassFactory
        /// </summary>
        [PreserveSig]
        [DllImport("EvrPresenter32.dll", EntryPoint = "DllGetClassObject")]
        public static extern int DllGetClassObject32([MarshalAs(UnmanagedType.LPStruct)] Guid clsid,
                                                      [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                                                      [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

        /// <summary>
        /// Static method in the 62 bit dll to create our IClassFactory
        /// </summary>
        [PreserveSig]
        [DllImport("EvrPresenter64.dll", EntryPoint = "DllGetClassObject")]
        public static extern int DllGetClassObject64([MarshalAs(UnmanagedType.LPStruct)] Guid clsid,
                                                      [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                                                      [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    }
}
