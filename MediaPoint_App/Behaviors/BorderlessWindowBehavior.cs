using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Interop;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using MediaPoint.Controls;
using Microsoft.Win32.SafeHandles;
using MediaPoint.MVVM.Services;
using MediaPoint.Common.Services;

namespace MediaPoint.App.Behaviors
{
	public sealed class SafeDC : SafeHandleZeroOrMinusOneIsInvalid
	{
		private static class NativeMethods
		{
			[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
			[DllImport("user32.dll")]
			public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

			[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
			[DllImport("user32.dll")]
			public static extern SafeDC GetDC(IntPtr hwnd);

			// Weird legacy function, documentation is unclear about how to use it...
			[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
			[DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
			public static extern SafeDC CreateDC([MarshalAs(UnmanagedType.LPWStr)] string lpszDriver, [MarshalAs(UnmanagedType.LPWStr)] string lpszDevice, IntPtr lpszOutput, IntPtr lpInitData);

			[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
			[DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			public static extern SafeDC CreateCompatibleDC(IntPtr hdc);

			[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
			[DllImport("gdi32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool DeleteDC(IntPtr hdc);
		}

		private IntPtr? _hwnd;
		private bool _created;

		public IntPtr Hwnd
		{
			set
			{
				_hwnd = value;
			}
		}

		private SafeDC() : base(true) { }

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		protected override bool ReleaseHandle()
		{
			if (_created)
			{
				return NativeMethods.DeleteDC(handle);
			}

			if (!_hwnd.HasValue || _hwnd.Value == IntPtr.Zero)
			{
				return true;
			}

			return NativeMethods.ReleaseDC(_hwnd.Value, handle) == 1;
		}

		[SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes"), SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public static SafeDC CreateDC(string deviceName)
		{
			SafeDC dc = null;
			try
			{
				// Should this really be on the driver parameter?
				dc = NativeMethods.CreateDC(deviceName, null, IntPtr.Zero, IntPtr.Zero);
			}
			finally
			{
				if (dc != null)
				{
					dc._created = true;
				}
			}

			if (dc.IsInvalid)
			{
				dc.Dispose();
				throw new SystemException("Unable to create a device context from the specified device information.");
			}

			return dc;
		}

		[SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes"), SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public static SafeDC CreateCompatibleDC(SafeDC hdc)
		{
			SafeDC dc = null;
			try
			{
				IntPtr hPtr = IntPtr.Zero;
				if (hdc != null)
				{
					hPtr = hdc.handle;
				}
				dc = NativeMethods.CreateCompatibleDC(hPtr);
				if (dc == null)
				{
					HRESULT.ThrowLastError();
				}
			}
			finally
			{
				if (dc != null)
				{
					dc._created = true;
				}
			}

			if (dc.IsInvalid)
			{
				dc.Dispose();
				throw new SystemException("Unable to create a device context from the specified device information.");
			}

			return dc;
		}

		public static SafeDC GetDC(IntPtr hwnd)
		{
			SafeDC dc = null;
			try
			{
				dc = NativeMethods.GetDC(hwnd);
			}
			finally
			{
				if (dc != null)
				{
					dc.Hwnd = hwnd;
				}
			}

			if (dc.IsInvalid)
			{
				// GetDC does not set the last error...
				HRESULT.E_FAIL.ThrowIfFailed();
			}

			return dc;
		}

		public static SafeDC GetDesktop()
		{
			return GetDC(IntPtr.Zero);
		}

		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public static SafeDC WrapDC(IntPtr hdc)
		{
			// This won't actually get released by the class, but it allows an IntPtr to be converted for signatures.
			return new SafeDC
			{
				handle = hdc,
				_created = false,
				_hwnd = IntPtr.Zero,
			};
		}
	}

	/// <summary>
	/// Wrapper for common Win32 status codes.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	internal struct Win32Error
	{
		[FieldOffset(0)]
		private readonly int _value;

		// NOTE: These public static field declarations are automatically
		// picked up by (HRESULT's) ToString through reflection.

		/// <summary>The operation completed successfully.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_SUCCESS = new Win32Error(0);
		/// <summary>Incorrect function.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_INVALID_FUNCTION = new Win32Error(1);
		/// <summary>The system cannot find the file specified.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_FILE_NOT_FOUND = new Win32Error(2);
		/// <summary>The system cannot find the path specified.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_PATH_NOT_FOUND = new Win32Error(3);
		/// <summary>The system cannot open the file.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_TOO_MANY_OPEN_FILES = new Win32Error(4);
		/// <summary>Access is denied.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_ACCESS_DENIED = new Win32Error(5);
		/// <summary>The handle is invalid.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_INVALID_HANDLE = new Win32Error(6);
		/// <summary>Not enough storage is available to complete this operation.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_OUTOFMEMORY = new Win32Error(14);
		/// <summary>There are no more files.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_NO_MORE_FILES = new Win32Error(18);
		/// <summary>The process cannot access the file because it is being used by another process.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_SHARING_VIOLATION = new Win32Error(32);
		/// <summary>The parameter is incorrect.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_INVALID_PARAMETER = new Win32Error(87);
		/// <summary>The data area passed to a system call is too small.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_INSUFFICIENT_BUFFER = new Win32Error(122);
		/// <summary>Cannot nest calls to LoadModule.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_NESTING_NOT_ALLOWED = new Win32Error(215);
		/// <summary>Illegal operation attempted on a registry key that has been marked for deletion.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_KEY_DELETED = new Win32Error(1018);
		/// <summary>Element not found.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_NOT_FOUND = new Win32Error(1168);
		/// <summary>There was no match for the specified key in the index.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_NO_MATCH = new Win32Error(1169);
		/// <summary>An invalid device was specified.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_BAD_DEVICE = new Win32Error(1200);
		/// <summary>The operation was canceled by the user.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_CANCELLED = new Win32Error(1223);
		/// <summary>The window class was already registered.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_CLASS_ALREADY_EXISTS = new Win32Error(1410);
		/// <summary>The specified datatype is invalid.</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly Win32Error ERROR_INVALID_DATATYPE = new Win32Error(1804);

		/// <summary>
		/// Create a new Win32 error.
		/// </summary>
		/// <param name="i">The integer value of the error.</param>
		public Win32Error(int i)
		{
			_value = i;
		}

		/// <summary>Performs HRESULT_FROM_WIN32 conversion.</summary>
		/// <param name="error">The Win32 error being converted to an HRESULT.</param>
		/// <returns>The equivilent HRESULT value.</returns>
		public static explicit operator HRESULT(Win32Error error)
		{
			// #define __HRESULT_FROM_WIN32(x) 
			//     ((HRESULT)(x) <= 0 ? ((HRESULT)(x)) : ((HRESULT) (((x) & 0x0000FFFF) | (FACILITY_WIN32 << 16) | 0x80000000)))
			if (error._value <= 0)
			{
				return new HRESULT((uint)error._value);
			}
			return HRESULT.Make(true, Facility.Win32, error._value & 0x0000FFFF);
		}

		// Method version of the cast operation
		/// <summary>Performs HRESULT_FROM_WIN32 conversion.</summary>
		/// <param name="error">The Win32 error being converted to an HRESULT.</param>
		/// <returns>The equivilent HRESULT value.</returns>
		public HRESULT ToHRESULT()
		{
			return (HRESULT)this;
		}

		/// <summary>Performs the equivalent of Win32's GetLastError()</summary>
		/// <returns>A Win32Error instance with the result of the native GetLastError</returns>
		[SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
		public static Win32Error GetLastError()
		{
			return new Win32Error(Marshal.GetLastWin32Error());
		}

		public override bool Equals(object obj)
		{
			try
			{
				return ((Win32Error)obj)._value == _value;
			}
			catch (InvalidCastException)
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return _value.GetHashCode();
		}

		/// <summary>
		/// Compare two Win32 error codes for equality.
		/// </summary>
		/// <param name="errLeft">The first error code to compare.</param>
		/// <param name="errRight">The second error code to compare.</param>
		/// <returns>Whether the two error codes are the same.</returns>
		public static bool operator ==(Win32Error errLeft, Win32Error errRight)
		{
			return errLeft._value == errRight._value;
		}

		/// <summary>
		/// Compare two Win32 error codes for inequality.
		/// </summary>
		/// <param name="errLeft">The first error code to compare.</param>
		/// <param name="errRight">The second error code to compare.</param>
		/// <returns>Whether the two error codes are not the same.</returns>
		public static bool operator !=(Win32Error errLeft, Win32Error errRight)
		{
			return !(errLeft == errRight);
		}
	}

	internal enum Facility
	{
		/// <summary>FACILITY_NULL</summary>
		Null = 0,
		/// <summary>FACILITY_RPC</summary>
		Rpc = 1,
		/// <summary>FACILITY_DISPATCH</summary>
		Dispatch = 2,
		/// <summary>FACILITY_STORAGE</summary>
		Storage = 3,
		/// <summary>FACILITY_ITF</summary>
		Itf = 4,
		/// <summary>FACILITY_WIN32</summary>
		Win32 = 7,
		/// <summary>FACILITY_WINDOWS</summary>
		Windows = 8,
		/// <summary>FACILITY_CONTROL</summary>
		Control = 10,
		/// <summary>MSDN doced facility code for ESE errors.</summary>
		Ese = 0xE5E,
		/// <summary>FACILITY_WINCODEC (WIC)</summary>
		WinCodec = 0x898,
	}

	/// <summary>Wrapper for HRESULT status codes.</summary>
	[StructLayout(LayoutKind.Explicit)]
	internal struct HRESULT
	{
		[FieldOffset(0)]
		private readonly uint _value;

		// NOTE: These public static field declarations are automatically
		// picked up by ToString through reflection.
		/// <summary>S_OK</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT S_OK = new HRESULT(0x00000000);
		/// <summary>S_FALSE</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT S_FALSE = new HRESULT(0x00000001);
		/// <summary>E_PENDING</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT E_PENDING = new HRESULT(0x8000000A);
		/// <summary>E_NOTIMPL</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT E_NOTIMPL = new HRESULT(0x80004001);
		/// <summary>E_NOINTERFACE</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT E_NOINTERFACE = new HRESULT(0x80004002);
		/// <summary>E_POINTER</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT E_POINTER = new HRESULT(0x80004003);
		/// <summary>E_ABORT</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT E_ABORT = new HRESULT(0x80004004);
		/// <summary>E_FAIL</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT E_FAIL = new HRESULT(0x80004005);
		/// <summary>E_UNEXPECTED</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT E_UNEXPECTED = new HRESULT(0x8000FFFF);
		/// <summary>STG_E_INVALIDFUNCTION</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT STG_E_INVALIDFUNCTION = new HRESULT(0x80030001);
		/// <summary>REGDB_E_CLASSNOTREG</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT REGDB_E_CLASSNOTREG = new HRESULT(0x80040154);

		/// <summary>DESTS_E_NO_MATCHING_ASSOC_HANDLER.  Win7 internal error code for Jump Lists.</summary>
		/// <remarks>There is no Assoc Handler for the given item registered by the specified application.</remarks>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT DESTS_E_NO_MATCHING_ASSOC_HANDLER = new HRESULT(0x80040F03);
		/// <summary>DESTS_E_NORECDOCS.  Win7 internal error code for Jump Lists.</summary>
		/// <remarks>The given item is excluded from the recent docs folder by the NoRecDocs bit on its registration.</remarks>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT DESTS_E_NORECDOCS = new HRESULT(0x80040F04);
		/// <summary>DESTS_E_NOTALLCLEARED.  Win7 internal error code for Jump Lists.</summary>
		/// <remarks>Not all of the items were successfully cleared</remarks>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT DESTS_E_NOTALLCLEARED = new HRESULT(0x80040F05);

		/// <summary>E_ACCESSDENIED</summary>
		/// <remarks>Win32Error ERROR_ACCESS_DENIED.</remarks>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT E_ACCESSDENIED = new HRESULT(0x80070005);
		/// <summary>E_OUTOFMEMORY</summary>
		/// <remarks>Win32Error ERROR_OUTOFMEMORY.</remarks>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT E_OUTOFMEMORY = new HRESULT(0x8007000E);
		/// <summary>E_INVALIDARG</summary>
		/// <remarks>Win32Error ERROR_INVALID_PARAMETER.</remarks>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT E_INVALIDARG = new HRESULT(0x80070057);
		/// <summary>INTSAFE_E_ARITHMETIC_OVERFLOW</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT INTSAFE_E_ARITHMETIC_OVERFLOW = new HRESULT(0x80070216);
		/// <summary>COR_E_OBJECTDISPOSED</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT COR_E_OBJECTDISPOSED = new HRESULT(0x80131622);
		/// <summary>WC_E_GREATERTHAN</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT WC_E_GREATERTHAN = new HRESULT(0xC00CEE23);
		/// <summary>WC_E_SYNTAX</summary>
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		public static readonly HRESULT WC_E_SYNTAX = new HRESULT(0xC00CEE2D);

		/// <summary>
		/// Create an HRESULT from an integer value.
		/// </summary>
		/// <param name="i"></param>
		public HRESULT(uint i)
		{
			_value = i;
		}

		public static HRESULT Make(bool severe, Facility facility, int code)
		{
			//#define MAKE_HRESULT(sev,fac,code) \
			//    ((HRESULT) (((unsigned long)(sev)<<31) | ((unsigned long)(fac)<<16) | ((unsigned long)(code))) )

			// Severity has 1 bit reserved.
			// bitness is enforced by the boolean parameter.

			// Facility has 11 bits reserved (different than SCODES, which have 4 bits reserved)
			// MSDN documentation incorrectly uses 12 bits for the ESE facility (e5e), so go ahead and let that one slide.
			// And WIC also ignores it the documented size...
			//Assert.Implies((int)facility != (int)((int)facility & 0x1FF), facility == Facility.Ese || facility == Facility.WinCodec);
			//// Code has 4 bits reserved.
			//Assert.AreEqual(code, code & 0xFFFF);

			return new HRESULT((uint)((severe ? (1 << 31) : 0) | ((int)facility << 16) | code));
		}

		/// <summary>
		/// retrieve HRESULT_FACILITY
		/// </summary>
		public Facility Facility
		{
			get
			{
				return GetFacility((int)_value);
			}
		}

		public static Facility GetFacility(int errorCode)
		{
			// #define HRESULT_FACILITY(hr)  (((hr) >> 16) & 0x1fff)
			return (Facility)((errorCode >> 16) & 0x1fff);
		}

		/// <summary>
		/// retrieve HRESULT_CODE
		/// </summary>
		public int Code
		{
			get
			{
				return GetCode((int)_value);
			}
		}

		public static int GetCode(int error)
		{
			// #define HRESULT_CODE(hr)    ((hr) & 0xFFFF)
			return (int)(error & 0xFFFF);
		}

		#region Object class override members

		/// <summary>
		/// Get a string representation of this HRESULT.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// Use reflection to try to name this HRESULT.
			// This is expensive, but if someone's ever printing HRESULT strings then
			// I think it's a fair guess that they're not in a performance critical area
			// (e.g. printing exception strings).
			// This is less error prone than trying to keep the list in the function.
			// To properly add an HRESULT's name to the ToString table, just add the HRESULT
			// like all the others above.
			//
			// CONSIDER: This data is static.  It could be cached 
			// after first usage for fast lookup since the keys are unique.
			//
			foreach (FieldInfo publicStaticField in typeof(HRESULT).GetFields(BindingFlags.Static | BindingFlags.Public))
			{
				if (publicStaticField.FieldType == typeof(HRESULT))
				{
					var hr = (HRESULT)publicStaticField.GetValue(null);
					if (hr == this)
					{
						return publicStaticField.Name;
					}
				}
			}

			// Try Win32 error codes also
			if (Facility == Facility.Win32)
			{
				foreach (FieldInfo publicStaticField in typeof(Win32Error).GetFields(BindingFlags.Static | BindingFlags.Public))
				{
					if (publicStaticField.FieldType == typeof(Win32Error))
					{
						var error = (Win32Error)publicStaticField.GetValue(null);
						if ((HRESULT)error == this)
						{
							return "HRESULT_FROM_WIN32(" + publicStaticField.Name + ")";
						}
					}
				}
			}

			// If there's no good name for this HRESULT,
			// return the string as readable hex (0x########) format.
			return string.Format(CultureInfo.InvariantCulture, "0x{0:X8}", _value);
		}

		public override bool Equals(object obj)
		{
			try
			{
				return ((HRESULT)obj)._value == _value;
			}
			catch (InvalidCastException)
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return _value.GetHashCode();
		}

		#endregion

		public static bool operator ==(HRESULT hrLeft, HRESULT hrRight)
		{
			return hrLeft._value == hrRight._value;
		}

		public static bool operator !=(HRESULT hrLeft, HRESULT hrRight)
		{
			return !(hrLeft == hrRight);
		}

		public bool Succeeded
		{
			get { return (int)_value >= 0; }
		}

		public bool Failed
		{
			get { return (int)_value < 0; }
		}

		public void ThrowIfFailed()
		{
			ThrowIfFailed(null);
		}

		[
			SuppressMessage(
				"Microsoft.Usage",
				"CA2201:DoNotRaiseReservedExceptionTypes",
				Justification = "Only recreating Exceptions that were already raised."),
			SuppressMessage(
				"Microsoft.Security",
				"CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")
		]
		public void ThrowIfFailed(string message)
		{
			if (Failed)
			{
				if (string.IsNullOrEmpty(message))
				{
					message = ToString();
				}
#if DEBUG
				else
				{
					message += " (" + ToString() + ")";
				}
#endif
				// Wow.  Reflection in a throw call.  Later on this may turn out to have been a bad idea.
				// If you're throwing an exception I assume it's OK for me to take some time to give it back.
				// I want to convert the HRESULT to a more appropriate exception type than COMException.
				// Marshal.ThrowExceptionForHR does this for me, but the general call uses GetErrorInfo
				// if it's set, and then ignores the HRESULT that I've provided.  This makes it so this
				// call works the first time but you get burned on the second.  To avoid this, I use
				// the overload that explicitly ignores the IErrorInfo.
				// In addition, the function doesn't allow me to set the Message unless I go through
				// the process of implementing an IErrorInfo and then use that.  There's no stock
				// implementations of IErrorInfo available and I don't think it's worth the maintenance
				// overhead of doing it, nor would it have significant value over this approach.
				Exception e = Marshal.GetExceptionForHR((int)_value, new IntPtr(-1));
				Debug.Assert(e != null);
				// ArgumentNullException doesn't have the right constructor parameters,
				// (nor does Win32Exception...)
				// but E_POINTER gets mapped to NullReferenceException,
				// so I don't think it will ever matter.
				Debug.Assert(e is ArgumentNullException);

				// If we're not getting anything better than a COMException from Marshal,
				// then at least check the facility and attempt to do better ourselves.
				if (e.GetType() == typeof(COMException))
				{
					switch (Facility)
					{
						case Facility.Win32:
							e = new Win32Exception(Code, message);
							break;
						default:
							e = new COMException(message, (int)_value);
							break;
					}
				}
				else
				{
					ConstructorInfo cons = e.GetType().GetConstructor(new[] { typeof(string) });
					if (null != cons)
					{
						e = cons.Invoke(new object[] { message }) as Exception;
						Debug.Assert(e != null);
					}
				}
				throw e;
			}
		}

		/// <summary>
		/// Convert the result of Win32 GetLastError() into a raised exception.
		/// </summary>
		public static void ThrowLastError()
		{
			((HRESULT)Win32Error.GetLastError()).ThrowIfFailed();
			// Only expecting to call this when we're expecting a failed GetLastError()
			Debug.Assert(1 == 2, "GetLastError Failed in ThrowLastError");
		}
	}

	/// <summary>
	/// Metro Styled Borderless Window Behavior
	/// </summary>
	public class BorderlessWindowBehavior : Behavior<Window>
	{
		#region Fixes

		/// <summary>
		/// WindowStyle values, WS_*
		/// </summary>
		[Flags]
		public enum WS : uint
		{
			OVERLAPPED = 0x00000000,
			POPUP = 0x80000000,
			CHILD = 0x40000000,
			MINIMIZE = 0x20000000,
			VISIBLE = 0x10000000,
			DISABLED = 0x08000000,
			CLIPSIBLINGS = 0x04000000,
			CLIPCHILDREN = 0x02000000,
			MAXIMIZE = 0x01000000,
			BORDER = 0x00800000,
			DLGFRAME = 0x00400000,
			VSCROLL = 0x00200000,
			HSCROLL = 0x00100000,
			SYSMENU = 0x00080000,
			THICKFRAME = 0x00040000,
			GROUP = 0x00020000,
			TABSTOP = 0x00010000,

			MINIMIZEBOX = 0x00020000,
			MAXIMIZEBOX = 0x00010000,

			CAPTION = BORDER | DLGFRAME,
			TILED = OVERLAPPED,
			ICONIC = MINIMIZE,
			SIZEBOX = THICKFRAME,
			TILEDWINDOW = OVERLAPPEDWINDOW,

			OVERLAPPEDWINDOW = OVERLAPPED | CAPTION | SYSMENU | THICKFRAME | MINIMIZEBOX | MAXIMIZEBOX,
			POPUPWINDOW = POPUP | BORDER | SYSMENU,
			CHILDWINDOW = CHILD,
		}

		/// <summary>
		/// Window style extended values, WS_EX_*
		/// </summary>
		[Flags]
		public enum WS_EX : uint
		{
			None = 0,
			DLGMODALFRAME = 0x00000001,
			NOPARENTNOTIFY = 0x00000004,
			TOPMOST = 0x00000008,
			ACCEPTFILES = 0x00000010,
			TRANSPARENT = 0x00000020,
			MDICHILD = 0x00000040,
			TOOLWINDOW = 0x00000080,
			WINDOWEDGE = 0x00000100,
			CLIENTEDGE = 0x00000200,
			CONTEXTHELP = 0x00000400,
			RIGHT = 0x00001000,
			LEFT = 0x00000000,
			RTLREADING = 0x00002000,
			LTRREADING = 0x00000000,
			LEFTSCROLLBAR = 0x00004000,
			RIGHTSCROLLBAR = 0x00000000,
			CONTROLPARENT = 0x00010000,
			STATICEDGE = 0x00020000,
			APPWINDOW = 0x00040000,
			LAYERED = 0x00080000,
			NOINHERITLAYOUT = 0x00100000, // Disable inheritence of mirroring by children
			LAYOUTRTL = 0x00400000, // Right to left mirroring
			COMPOSITED = 0x02000000,
			NOACTIVATE = 0x08000000,
			OVERLAPPEDWINDOW = (WINDOWEDGE | CLIENTEDGE),
			PALETTEWINDOW = (WINDOWEDGE | TOOLWINDOW | TOPMOST),
		}

		/// <summary>
		/// GetWindowLongPtr values, GWL_*
		/// </summary>
		public enum GWL
		{
			WNDPROC = (-4),
			HINSTANCE = (-6),
			HWNDPARENT = (-8),
			STYLE = (-16),
			EXSTYLE = (-20),
			USERDATA = (-21),
			ID = (-12)
		}

		[SuppressMessage("Microsoft.Interoperability", "CA1400:PInvokeEntryPointsShouldExist")]
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		[DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
		private static extern int GetWindowLongPtr32(IntPtr hWnd, GWL nIndex);

		[SuppressMessage("Microsoft.Interoperability", "CA1400:PInvokeEntryPointsShouldExist")]
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
		private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, GWL nIndex);

		// This is aliased as a macro in 32bit Windows.
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public static IntPtr GetWindowLongPtr(IntPtr hwnd, GWL nIndex)
		{
			IntPtr ret = IntPtr.Zero;
			if (8 == IntPtr.Size)
			{
				ret = GetWindowLongPtr64(hwnd, nIndex);
			}
			else
			{
				ret = new IntPtr(GetWindowLongPtr32(hwnd, nIndex));
			}
			if (IntPtr.Zero == ret)
			{
				throw new Win32Exception();
			}
			return ret;
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		[DllImport("user32.dll", EntryPoint = "AdjustWindowRectEx", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool _AdjustWindowRectEx(ref RECT lpRect, WS dwStyle, [MarshalAs(UnmanagedType.Bool)] bool bMenu,
		                                               WS_EX dwExStyle);

		public static RECT AdjustWindowRectEx(RECT lpRect, WS dwStyle, bool bMenu, WS_EX dwExStyle)
		{
			// Native version modifies the parameter in place.
			if (!_AdjustWindowRectEx(ref lpRect, dwStyle, bMenu, dwExStyle))
			{
				HRESULT.ThrowLastError();
			}

			return lpRect;
		}


		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		[DllImport("user32.dll", EntryPoint = "GetWindowRect", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool _GetWindowRect(IntPtr hWnd, out RECT lpRect);

		public static RECT GetWindowRect(IntPtr hwnd)
		{
			RECT rc;
			if (!_GetWindowRect(hwnd, out rc))
			{
				HRESULT.ThrowLastError();
			}
			return rc;
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		[DllImport("gdi32.dll")]
		public static extern int GetDeviceCaps(SafeDC hdc, DeviceCap nIndex);

		/// <summary>
		/// GetDeviceCaps nIndex values.
		/// </summary>
		public enum DeviceCap
		{
			/// <summary>Number of bits per pixel
			/// </summary>
			BITSPIXEL = 12,

			/// <summary>
			/// Number of planes
			/// </summary>
			PLANES = 14,

			/// <summary>
			/// Logical pixels inch in X
			/// </summary>
			LOGPIXELSX = 88,

			/// <summary>
			/// Logical pixels inch in Y
			/// </summary>
			LOGPIXELSY = 90,
		}


		/// <summary>
		/// Convert a point in system coordinates to a point in device independent pixels (1/96").
		/// </summary>
		/// <param name="logicalPoint">A point in the physical coordinate system.</param>
		/// <returns>Returns the parameter converted to the device independent coordinate system.</returns>
		public static Point DevicePixelsToLogical(Point devicePoint)
		{
			using (SafeDC desktop = SafeDC.GetDesktop())
			{
				int pixelsPerInchX = GetDeviceCaps(desktop, DeviceCap.LOGPIXELSX);
				int pixelsPerInchY = GetDeviceCaps(desktop, DeviceCap.LOGPIXELSY);
				var _transformToDip = Matrix.Identity;
				_transformToDip.Scale(96d/(double) pixelsPerInchX, 96d/(double) pixelsPerInchY);
				return _transformToDip.Transform(devicePoint);
			}
		}

		public static Rect DeviceRectToLogical(Rect deviceRectangle)
		{
			Point topLeft = DevicePixelsToLogical(new Point(deviceRectangle.Left, deviceRectangle.Top));
			Point bottomRight = DevicePixelsToLogical(new Point(deviceRectangle.Right, deviceRectangle.Bottom));

			return new Rect(topLeft, bottomRight);
		}

		private void _FixupFrameworkIssues(bool invert = false)
		{
			Window _window = AssociatedObject as Window;
			//Debug.Assert(_window != null);
			if (_window == null) return;

			// This margin is only necessary if the client rect is going to be calculated incorrectly by WPF.
			// This bug was fixed in V4 of the framework.
			if (Assembly.GetAssembly(typeof (Window)).GetName().Version >= new Version(4, 0))
			{
				return;
			}

			if (_window.Template == null)
			{
				// Nothing to fixup yet.  This will get called again when a template does get set.
				return;
			}

			// Guard against the visual tree being empty.
			if (VisualTreeHelper.GetChildrenCount(_window) == 0)
			{
				// The template isn't null, but we don't have a visual tree.
				// Hope that ApplyTemplate is in the queue and repost this, because there's not much we can do right now.
				_window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)(()=> _FixupFrameworkIssues()));
				return;
			}

			var rootElement = (FrameworkElement) VisualTreeHelper.GetChild(_window, 0);

			RECT rcWindow = GetWindowRect(m_hwnd);
			RECT rcAdjustedClient = _GetAdjustedWindowRect(rcWindow);

			Rect rcLogicalWindow = DeviceRectToLogical(new Rect(rcWindow.left, rcWindow.top, rcWindow.Width, rcWindow.Height));
			Rect rcLogicalClient =
				DeviceRectToLogical(new Rect(rcAdjustedClient.left, rcAdjustedClient.top, rcAdjustedClient.Width,
				                             rcAdjustedClient.Height));

			Thickness nonClientThickness = new Thickness(
				rcLogicalWindow.Left - rcLogicalClient.Left,
				rcLogicalWindow.Top - rcLogicalClient.Top,
				rcLogicalClient.Right - rcLogicalWindow.Right,
				rcLogicalClient.Bottom - rcLogicalWindow.Bottom);

			rootElement.Margin = new Thickness(
				0,
				0,
				-(nonClientThickness.Left + nonClientThickness.Right) * (invert ? -1 : 1),
				-(nonClientThickness.Top + nonClientThickness.Bottom) * (invert ? -1 : 1));

			// The negative thickness on the margin doesn't properly get applied in RTL layouts.
			// The width is right, but there is a black bar on the right.
			// To fix this we just add an additional RenderTransform to the root element.
			// This works fine, but if the window is dynamically changing its FlowDirection then this can have really bizarre side effects.
			// This will mostly work if the FlowDirection is dynamically changed, but there aren't many real scenarios that would call for
			// that so I'm not addressing the rest of the quirkiness.
			if (_window.FlowDirection == FlowDirection.RightToLeft)
			{
				rootElement.RenderTransform = new MatrixTransform(1, 0, 0, 1, -(nonClientThickness.Left + nonClientThickness.Right),
				                                                  0);
			}
			else
			{
				rootElement.RenderTransform = null;
			}

			if (!_isFixedUp)
			{
				_hasUserMovedWindow = false;
				_window.StateChanged += _FixupRestoreBounds;

				_isFixedUp = true;
			}
		}

		private bool _isFixedUp;
		private bool _hasUserMovedWindow;

		[StructLayout(LayoutKind.Sequential)]
		public class WINDOWPLACEMENT
		{
			public int length = Marshal.SizeOf(typeof (WINDOWPLACEMENT));
			public int flags;
			public SW showCmd;
			public POINT ptMinPosition;
			public POINT ptMaxPosition;
			public RECT rcNormalPosition;
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowPlacement(IntPtr hwnd, WINDOWPLACEMENT lpwndpl);

		/// <summary>
		/// ShowWindow options
		/// </summary>
		public enum SW
		{
			HIDE = 0,
			SHOWNORMAL = 1,
			NORMAL = 1,
			SHOWMINIMIZED = 2,
			SHOWMAXIMIZED = 3,
			MAXIMIZE = 3,
			SHOWNOACTIVATE = 4,
			SHOW = 5,
			MINIMIZE = 6,
			SHOWMINNOACTIVE = 7,
			SHOWNA = 8,
			RESTORE = 9,
			SHOWDEFAULT = 10,
			FORCEMINIMIZE = 11,
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public static WINDOWPLACEMENT GetWindowPlacement(IntPtr hwnd)
		{
			WINDOWPLACEMENT wndpl = new WINDOWPLACEMENT();
			if (GetWindowPlacement(hwnd, wndpl))
			{
				return wndpl;
			}
			throw new Win32Exception();
		}

		private void _FixupRestoreBounds(object sender, EventArgs e)
		{
			Window _window = AssociatedObject as Window;

			if (_window.WindowState == WindowState.Maximized || _window.WindowState == WindowState.Minimized)
			{
				// Old versions of WPF sometimes force their incorrect idea of the Window's location
				// on the Win32 restore bounds.  If we have reason to think this is the case, then
				// try to undo what WPF did after it has done its thing.
				if (_hasUserMovedWindow)
				{
					_hasUserMovedWindow = false;
					WINDOWPLACEMENT wp = GetWindowPlacement(m_hwnd);

					RECT adjustedDeviceRc = _GetAdjustedWindowRect(new RECT {bottom = 100, right = 100});
					Point adjustedTopLeft = DevicePixelsToLogical(
						new Point(
							wp.rcNormalPosition.left - adjustedDeviceRc.left,
							wp.rcNormalPosition.top - adjustedDeviceRc.top));

					_window.Top = adjustedTopLeft.Y;
					_window.Left = adjustedTopLeft.X;
				}
			}
		}

		private RECT _GetAdjustedWindowRect(RECT rcWindow)
		{
			var style = (WS) GetWindowLongPtr(m_hwnd, GWL.STYLE);
			var exstyle = (WS_EX) GetWindowLongPtr(m_hwnd, GWL.EXSTYLE);

			return AdjustWindowRectEx(rcWindow, style, false, exstyle);
		}

		#endregion

		#region Native Methods

		[StructLayout(LayoutKind.Sequential)]
		public struct MARGINS
		{
			public int leftWidth;
			public int rightWidth;
			public int topHeight;
			public int bottomHeight;
		}

		[DllImport("dwmapi.dll")]
		private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int x;
			public int y;

			/// <summary>
			/// Initializes a new instance of the <see cref="POINT"/> struct.
			/// </summary>
			/// <param name="x">The x.</param>
			/// <param name="y">The y.</param>
			public POINT(int x, int y)
			{
				this.x = x;
				this.y = y;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MINMAXINFO
		{
			public POINT ptReserved;
			public POINT ptMaxSize;
			public POINT ptMaxPosition;
			public POINT ptMinTrackSize;
			public POINT ptMaxTrackSize;
		};

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public class MONITORINFO
		{
			public int cbSize = Marshal.SizeOf(typeof (MONITORINFO));
			public RECT rcMonitor = new RECT();
			public RECT rcWork = new RECT();
			public int dwFlags = 0;
		}

		/// <summary> Win32 </summary>
		[StructLayout(LayoutKind.Sequential, Pack = 0)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
			public static readonly RECT Empty = new RECT();

			/// <summary>
			/// Gets the width. 
			/// </summary>
			public int Width
			{
				// Abs needed for BIDI OS
				get { return Math.Abs(right - left); }
			}

			/// <summary>
			/// Gets the height.
			/// </summary>
			public int Height
			{
				get { return bottom - top; }
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="RECT"/> struct.
			/// </summary>
			/// <param name="left">The left.</param>
			/// <param name="top">The top.</param>
			/// <param name="right">The right.</param>
			/// <param name="bottom">The bottom.</param>
			public RECT(int left, int top, int right, int bottom)
			{
				this.left = left;
				this.top = top;
				this.right = right;
				this.bottom = bottom;
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="RECT"/> struct.
			/// </summary>
			/// <param name="rcSrc">The rc SRC.</param>
			public RECT(RECT rcSrc)
			{
				this.left = rcSrc.left;
				this.top = rcSrc.top;
				this.right = rcSrc.right;
				this.bottom = rcSrc.bottom;
			}

			/// <summary>
			/// Gets a value indicating whether this instance is empty.
			/// </summary>
			/// <value>
			///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
			/// </value>
			public bool IsEmpty
			{
				get
				{
					// BUGBUG : On Bidi OS (hebrew arabic) left > right
					return left >= right || top >= bottom;
				}
			}

			/// <summary>
			/// Return a user friendly representation of this struct
			/// </summary>
			/// <returns>
			/// A <see cref="System.String"/> that represents this instance.
			/// </returns>
			public override string ToString()
			{
				if (this == RECT.Empty)
					return "RECT {Empty}";

				return String.Format("RECT {{ left : {0} / top : {1} / right : {2} / bottom : {3} }}", left, top, right, bottom);
			}

			/// <summary>
			/// Determine if 2 RECT are equal (deep compare)
			/// </summary>
			/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
			/// <returns>
			///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
			/// </returns>
			public override bool Equals(object obj)
			{
				if (!(obj is Rect))
					return false;

				return (this == (RECT) obj);
			}

			/// <summary>
			/// Return the HashCode for this struct (not guaranteed to be unique)
			/// </summary>
			/// <returns>
			/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
			/// </returns>
			public override int GetHashCode()
			{
				return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
			}

			/// <summary>
			/// Determine if 2 RECT are equal (deep compare)
			/// </summary>
			/// <param name="rect1">The rect1.</param>
			/// <param name="rect2">The rect2.</param>
			/// <returns>
			/// The result of the operator.
			/// </returns>
			public static bool operator ==(RECT rect1, RECT rect2)
			{
				return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right &&
				        rect1.bottom == rect2.bottom);
			}

			/// <summary>
			/// Determine if 2 RECT are different (deep compare)
			/// </summary>
			/// <param name="rect1">The rect1.</param>
			/// <param name="rect2">The rect2.</param>
			/// <returns>
			/// The result of the operator.
			/// </returns>
			public static bool operator !=(RECT rect1, RECT rect2)
			{
				return !(rect1 == rect2);
			}
		}

		/// <summary>
		/// Gets the monitor info.
		/// </summary>
		/// <param name="hMonitor">The h monitor.</param>
		/// <param name="lpmi">The lpmi.</param>
		/// <returns></returns>
		[DllImport("user32")]
		internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

		/// <summary>
		/// Monitors from window.
		/// </summary>
		/// <param name="handle">The handle.</param>
		/// <param name="flags">The flags.</param>
		/// <returns></returns>
		[DllImport("User32")]
		internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        [DllImport("shcore.dll", CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, ref uint xDpi, ref uint yDpi);

        protected enum MonitorDpiType
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }

        protected Point GetDpiForHwnd(IntPtr hwnd)
        {
            IntPtr monitor = MonitorFromWindow(hwnd, 2);

            uint newDpiX = 96;
            uint newDpiY = 96;
            if (GetDpiForMonitor(monitor, (int)MonitorDpiType.MDT_Effective_DPI, ref newDpiX, ref newDpiY) != 0)
            {
                return new Point
                {
                    X = 96.0,
                    Y = 96.0
                };
            }

            return new Point
            {
                X = (double)newDpiX,
                Y = (double)newDpiY
            };
        }

        private void WmGetMinMaxInfoAA(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
            // Adjust the maximized size and position to fit the work area of the correct monitor
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                var dpi = GetDpiForHwnd(hwnd);
                if (dpi.X != dpi.Y)
                {
                    dpi.X = dpi.Y;
                }
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                var metroWindow = AssociatedObject as Window;
                var ignoreTaskBar = true; // metroWindow != null && (metroWindow.IgnoreTaskbarOnMaximize || metroWindow.UseNoneWindowStyle);
                var x = ignoreTaskBar ? monitorInfo.rcMonitor.left : monitorInfo.rcWork.left;
                var y = ignoreTaskBar ? monitorInfo.rcMonitor.top : monitorInfo.rcWork.top;
                mmi.ptMaxSize.x = ignoreTaskBar ? Math.Abs(monitorInfo.rcMonitor.right - x) : Math.Abs(monitorInfo.rcWork.right - x);
                mmi.ptMaxSize.y = ignoreTaskBar ? Math.Abs(monitorInfo.rcMonitor.bottom - y) : Math.Abs(monitorInfo.rcWork.bottom - y);
                // only do this on maximize
                //if (!ignoreTaskBar && AssociatedObject.WindowState == WindowState.Maximized)
                //{
                //    mmi.ptMaxTrackSize.X = mmi.ptMaxSize.X;
                //    mmi.ptMaxTrackSize.Y = mmi.ptMaxSize.Y;
                //    mmi = AdjustWorkingAreaForAutoHide(monitor, mmi);
                //}
            }
            Marshal.StructureToPtr(mmi, lParam, true);
        }

        private void HandleMaximizeA()
        {
            if (AssociatedObject != null && AssociatedObject.WindowState == WindowState.Maximized)
            {
                // remove resize border and window border, so we can move the window from top monitor position
                AssociatedObject.BorderThickness = new Thickness(0);

                var handle = new WindowInteropHelper(AssociatedObject).Handle;
                // WindowChrome handles the size false if the main monitor is lesser the monitor where the window is maximized
                // so set the window pos/size twice
                IntPtr monitor = MonitorFromWindow(handle, 2);
                if (monitor != IntPtr.Zero)
                {
                    var monitorInfo = new MONITORINFO();
                    GetMonitorInfo(monitor, monitorInfo);
                    var metroWindow = AssociatedObject as Window;
                    var ignoreTaskBar = true;//metroWindow != null && (metroWindow.IgnoreTaskbarOnMaximize || metroWindow.UseNoneWindowStyle);
                    var x = ignoreTaskBar ? monitorInfo.rcMonitor.left : monitorInfo.rcWork.left;
                    var y = ignoreTaskBar ? monitorInfo.rcMonitor.top : monitorInfo.rcWork.top;
                    var cx = ignoreTaskBar ? Math.Abs(monitorInfo.rcMonitor.right - x) : Math.Abs(monitorInfo.rcWork.right - x);
                    var cy = ignoreTaskBar ? Math.Abs(monitorInfo.rcMonitor.bottom - y) : Math.Abs(monitorInfo.rcWork.bottom - y);
                    SetWindowPos(handle, new IntPtr(-2), x, y, cx, cy, 0x0040);
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);


		/// <summary>
		/// Wms the get min max info.
		/// </summary>
		/// <param name="hwnd">The HWND.</param>
		/// <param name="lParam">The l param.</param>
		private void WmGetMinMaxInfo(System.IntPtr hwnd, System.IntPtr lParam)
		{
			MINMAXINFO mmi = (MINMAXINFO) Marshal.PtrToStructure(lParam, typeof (MINMAXINFO));

			// Adjust the maximized size and position to fit the work area of the correct monitor
			int MONITOR_DEFAULTTONEAREST = 0x00000002;
			System.IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

			if (monitor != System.IntPtr.Zero)
			{
				MONITORINFO monitorInfo = new MONITORINFO();
				GetMonitorInfo(monitor, monitorInfo);
				RECT rcWorkArea = monitorInfo.rcWork;
				RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = 0; //(int)((double)rcMonitorArea.left / (120d / 96)); //(rcMonitorArea.left);
                mmi.ptMaxPosition.y = 0; //(int)((double)rcMonitorArea.top / (120d / 96));
                mmi.ptMaxSize.x = (int)((rcMonitorArea.right - rcMonitorArea.left));
                mmi.ptMaxSize.y = (int)((rcMonitorArea.bottom - rcMonitorArea.top));
                mmi.ptMaxTrackSize.x = mmi.ptMaxSize.x;
                mmi.ptMaxTrackSize.y = mmi.ptMaxSize.y;
                mmi.ptMinTrackSize.x = mmi.ptMaxTrackSize.x;
                mmi.ptMinTrackSize.y = mmi.ptMaxTrackSize.y;
            }

			Marshal.StructureToPtr(mmi, lParam, true);
		}

		/// <summary>
		/// Defs the window proc.
		/// </summary>
		/// <param name="hwnd">The HWND.</param>
		/// <param name="msg">The MSG.</param>
		/// <param name="wParam">The w param.</param>
		/// <param name="lParam">The l param.</param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		public static extern IntPtr DefWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

		#endregion

		private const int WM_GETMINMAXINFO = 0x24;
		private const int WM_NCACTIVATE = 0x86;
		private const int WM_NCCALCSIZE = 0x83;
		private const int WM_NCPAINT = 0x85;
		private const int WINDOWPOSCHANGING = 0x0046;
		private const int NOMOVE = 0x2;
		private int _lastWidth;
		private int _lastHeight;
		private HwndSource m_hwndSource;
		private IntPtr m_hwnd;
		private PresentationSource _source;
		private Matrix _transformFromDevice;
		private Matrix _transformToDevice;

		/// <summary>
		/// Called after the behavior is attached to an AssociatedObject.
		/// </summary>
		protected override void OnAttached()
		{
			if (AssociatedObject.IsInitialized)
			{
				AddHwndHook();
			}
			else
			{
				AssociatedObject.SourceInitialized += AssociatedObject_SourceInitialized;
			}

			//AssociatedObject.WindowStyle = WindowStyle.None;
			AssociatedObject.ResizeMode = ResizeWithGrip ? ResizeMode.CanResizeWithGrip : ResizeMode.CanResize;

            try
            {
                base.OnAttached();
            }
            catch
            { 
            }
		}

		private void _OnWindowPropertyChangedThatRequiresTemplateFixup(object sender, EventArgs e)
		{
			if (m_hwnd != IntPtr.Zero && AssociatedObject != null)
			{
				// Assume that when the template changes it's going to be applied.
				// We don't have a good way to externally hook into the template
				// actually being applied, so we asynchronously post the fixup operation
				// at Loaded priority, so it's expected that the visual tree will be
				// updated before _FixupFrameworkIssues is called.
				(AssociatedObject as Window).Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)(() => _FixupFrameworkIssues()));
			}
		}

		public static void AddDependencyPropertyChangeListener(object component, DependencyProperty property,
		                                                       EventHandler listener)
		{
			if (component == null)
			{
				return;
			}

			DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(property, component.GetType());
			dpd.AddValueChanged(component, listener);
		}

		/// <summary>
		/// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
		/// </summary>
		protected override void OnDetaching()
		{
			AssociatedObject.SetValue(Window.WindowStyleProperty, DependencyProperty.UnsetValue);
			DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(Window.TemplateProperty, AssociatedObject.GetType());
			dpd.RemoveValueChanged(AssociatedObject, _OnWindowPropertyChangedThatRequiresTemplateFixup);
			dpd = DependencyPropertyDescriptor.FromProperty(Window.TemplateProperty, AssociatedObject.GetType());
			dpd.RemoveValueChanged(AssociatedObject, _OnWindowPropertyChangedThatRequiresTemplateFixup);
			AssociatedObject.StateChanged -= _FixupRestoreBounds;
            AssociatedObject.SetValue(Window.ResizeModeProperty, DependencyProperty.UnsetValue);
			_FixupFrameworkIssues(true);
			RemoveHwndHook();
			base.OnDetaching();
		}

		/// <summary>
		/// Adds the HWND hook.
		/// </summary>
		private void AddHwndHook()
		{
			if (AssociatedObject.IsLoaded)
			{
				m_hwndSource = HwndSource.FromVisual(AssociatedObject) as HwndSource;
                if (m_hwndSource == null) return;
				m_hwndSource.AddHook(HwndHook);
				m_hwnd = new WindowInteropHelper(AssociatedObject).Handle;

				if (Assembly.GetAssembly(typeof(Window)).GetName().Version < new Version(4, 0))
				{
					Window _window = AssociatedObject as Window;
					// On older versions of the framework the client size of the window is incorrectly calculated.
					// We need to modify the template to fix this on behalf of the user.
					AddDependencyPropertyChangeListener(_window, Window.TemplateProperty,
														_OnWindowPropertyChangedThatRequiresTemplateFixup);
					AddDependencyPropertyChangeListener(_window, Window.FlowDirectionProperty,
														_OnWindowPropertyChangedThatRequiresTemplateFixup);
				}
				_source = PresentationSource.FromVisual(AssociatedObject);
				_transformFromDevice = _source.CompositionTarget.TransformFromDevice;
				_transformToDevice = _source.CompositionTarget.TransformToDevice;
				_FixupFrameworkIssues();
			}
			else
			{
				AssociatedObject.Loaded += new RoutedEventHandler(AssociatedObject_Loaded);
			}
		}

		void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
		{
			m_hwndSource = HwndSource.FromVisual(AssociatedObject) as HwndSource;
			m_hwndSource.AddHook(HwndHook);
			m_hwnd = new WindowInteropHelper(AssociatedObject).Handle;

			if (Assembly.GetAssembly(typeof(Window)).GetName().Version < new Version(4, 0))
			{
				Window _window = AssociatedObject as Window;
				// On older versions of the framework the client size of the window is incorrectly calculated.
				// We need to modify the template to fix this on behalf of the user.
				AddDependencyPropertyChangeListener(_window, Window.TemplateProperty,
													_OnWindowPropertyChangedThatRequiresTemplateFixup);
				AddDependencyPropertyChangeListener(_window, Window.FlowDirectionProperty,
													_OnWindowPropertyChangedThatRequiresTemplateFixup);
			}
			_source = PresentationSource.FromVisual(AssociatedObject);
			_transformFromDevice = _source.CompositionTarget.TransformFromDevice;
			_transformToDevice = _source.CompositionTarget.TransformToDevice;
			_FixupFrameworkIssues();
		}

		/// <summary>
		/// Removes the HWND hook.
		/// </summary>
		private void RemoveHwndHook()
		{
			if (m_hwndSource != null) m_hwndSource.RemoveHook(HwndHook);
			if (AssociatedObject != null) AssociatedObject.SourceInitialized -= AssociatedObject_SourceInitialized;
		}

		/// <summary>
		/// Handles the SourceInitialized event of the AssociatedObject control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void AssociatedObject_SourceInitialized(object sender, EventArgs e)
		{
			_source = PresentationSource.FromVisual(AssociatedObject);
			_transformFromDevice = _source.CompositionTarget.TransformFromDevice;
			_transformToDevice = _source.CompositionTarget.TransformToDevice;
			AddHwndHook();
		}


		[StructLayout(LayoutKind.Sequential)]
		private struct NCCALCSIZE_PARAMS
		{
			public RECT rgrc0, rgrc1, rgrc2;
			public IntPtr lppos;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct WINDOWPOS
		{
			public IntPtr hwnd;
			public IntPtr hwndInsertAfter;
			public int x;
			public int y;
			public int cx;
			public int cy;
			public int flags;
		}

		/// <summary>
		/// HWNDs the hook.
		/// </summary>
		/// <param name="hWnd">The h WND.</param>
		/// <param name="message">The message.</param>
		/// <param name="wParam">The w param.</param>
		/// <param name="lParam">The l param.</param>
		/// <param name="handled">if set to <c>true</c> [handled].</param>
		/// <returns></returns>
		private IntPtr HwndHook(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
            if (AssociatedObject == null) return IntPtr.Zero;

            IntPtr returnval = IntPtr.Zero;
            //const int SC_SCREENSAVE = 0xF140;
            //const int SC_MONITORPOWER = 0xF170;
            //const int WM_SYSCOMMAND = 0x0112;

            //if ((wParam == (IntPtr)SC_SCREENSAVE) && (message == WM_SYSCOMMAND))
            //    return (IntPtr)(-1); // prevent screensaver
            //else if ((wParam == (IntPtr)SC_MONITORPOWER) && (message == WM_SYSCOMMAND))
            //    return (IntPtr)(-1); // prevent monitor power-off
            
			switch (message)
			{
				case WINDOWPOSCHANGING:
					{
                        if (AssociatedObject != null && AssociatedObject.IsInitialized && AssociatedObject.WindowState == WindowState.Normal && AssociatedObject.WindowStyle == WindowStyle.None)
                        {
                            WINDOWPOS pos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));
                            if (/*(pos.flags & (int)NOMOVE) != 0 || */ AssociatedObject.Content == null || (pos.cx == 0 && pos.cy == 0))
                            {
                                return IntPtr.Zero;
                            }
                            var mu = AssociatedObject.FindName("mediaPlayer") as MediaUriElement;
                            if (mu != null && mu.NaturalVideoWidth != 0 && mu.NaturalVideoHeight != 0 && mu.HasVideo)
                            {
                                double r = (double)mu.NaturalVideoWidth / (double)mu.NaturalVideoHeight;

                                if ((double)pos.cx / pos.cy != r)
                                {
                                    pos.cy = (int)((double)pos.cx / r);
                                }

                                pos.cx = (int)(pos.cy * r);
                                pos.cy = (int)(pos.cx / r);

                                if (pos.cx == 0 || pos.cy == 0)
                                {
                                    handled = false;
                                    return IntPtr.Zero;
                                }

                                var ms = (Size)_transformToDevice.Transform(new Vector(AssociatedObject.MinWidth, AssociatedObject.MinHeight));

                                //ms.Width = ms.Width * (GetDpiForHwnd(hWnd).X / 120.0);
                                //ms.Height = ms.Height * (GetDpiForHwnd(hWnd).Y / 120.0);

                                if (pos.cx < ms.Width)
                                {
                                    pos.cx = (int)ms.Width;
                                    pos.cy = (int)(ms.Width / r);
                                }

                                if (pos.cy < ms.Height)
                                {
                                    pos.cy = (int)ms.Height;
                                    pos.cx = (int)(ms.Height * r);
                                }
                                _lastWidth = pos.cx;
                                _lastHeight = pos.cy;

                                Marshal.StructureToPtr(pos, lParam, true);
                                handled = true;
                                
                            }
                        }
					}
					break;
				case WM_NCCALCSIZE:
					{
                        if (AssociatedObject != null && AssociatedObject.WindowStyle == WindowStyle.None)
                        {
                            ServiceLocator.GetService<IMainWindow>().SetChildWindowsFollow(false);
                            handled = true;
                            AssociatedObject.Dispatcher.BeginInvoke((Action)(() =>
                            {
                                ServiceLocator.GetService<IMainWindow>().SetChildWindowsFollow(true);
                            }));
                        }
					}
					break;
				case WM_NCPAINT:
					{
						// Works for Windows Vista and higher
                        if (AssociatedObject != null && Environment.OSVersion.Version.Major >= 6 && AssociatedObject.WindowStyle == WindowStyle.None)
                        {
                            var m = new MARGINS { bottomHeight = 0, leftWidth = 0, rightWidth = 0, topHeight = 0 };
                            DwmExtendFrameIntoClientArea(m_hwnd, ref m);
                            handled = true;
                        }
                        
					}
					break;
				case WM_NCACTIVATE:
					{
                        if (AssociatedObject != null && AssociatedObject.WindowStyle == WindowStyle.None)
                        {
                            /* As per http://msdn.microsoft.com/en-us/library/ms632633(VS.85).aspx , "-1" lParam does not
                             * repaint the nonclient area to reflect the state change. */
                            returnval = DefWindowProc(hWnd, message, wParam, new IntPtr(-1));
                            handled = true;
                        }
					}
					break;
				case WM_GETMINMAXINFO:
					{
                        if (AssociatedObject != null && AssociatedObject.WindowStyle == WindowStyle.None && AssociatedObject.WindowState == WindowState.Maximized)
                        {
                            /* From Lester's Blog (thanks @aeoth):  
                             * http://blogs.msdn.com/b/llobo/archive/2006/08/01/maximizing-window-_2800_with-windowstyle_3d00_none_2900_-considering-taskbar.aspx */
                            WmGetMinMaxInfo(hWnd, lParam);
                        };
                        handled = true;
					}
					break;
			}

			return returnval;
		}

		public static DependencyProperty ResizeWithGripDependencyProperty = DependencyProperty.Register("ResizeWithGrip",
		                                                                                                typeof (bool),
		                                                                                                typeof (
		                                                                                                	BorderlessWindowBehavior
		                                                                                                	),
		                                                                                                new PropertyMetadata
		                                                                                                	(false,
		                                                                                                	 OnGripChanged));

		private static void OnGripChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
            d.Dispatcher.BeginInvoke((Action)(() => { (d as BorderlessWindowBehavior).OnAttached(); }), DispatcherPriority.Loaded);
		}


		/// <summary>
        /// Gets or sets a value indicating whether [resize with grip].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [resize with grip]; otherwise, <c>false</c>.
        /// </value>
        public bool ResizeWithGrip
        {
            get { return (bool)GetValue(ResizeWithGripDependencyProperty); }
            set { SetValue(ResizeWithGripDependencyProperty, value); }
        }
    }
}
