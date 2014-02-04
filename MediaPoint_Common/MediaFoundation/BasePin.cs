/*
 * C# filter Pin base class
 * =============================================
 * Requires DirectShowLib classes, but you'll want to edit them.
 * 
 * Author: Simon Bond <simon at sichbo dot see eh>
 * License: Free source - go nuts.
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DirectShowLib;
using System.Security.Permissions;

namespace MediaPoint.Common.MediaFoundation
{
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    [ComVisible(true)]
    public abstract unsafe class BasePin : IPin
    {
        public string _Name;
        internal AMMediaType _MediaType;
        internal IPin _ConnectedPin;
        internal PinDirection _Direction;
        internal BaseFilter _Filter;
        internal Guid _ConnectedFilterClassID;
        internal IBaseFilter _ConnectedFilter;
        internal string _ConnectedFilterName;
        internal bool AcceptsAnyMedia;
        internal List<AMMediaType> MediaTypes = new List<AMMediaType>();

        public BasePin(PinDirection direction, BaseFilter filter)
        {
        	AcceptsAnyMedia = true;
			MediaTypes.Add(new AMMediaType());
            _Direction = direction;
            _Filter = filter;
        }

        public override string ToString()
        {
            return _Name;
        }

        public bool IsConnected
        {
            get { return _ConnectedPin != null; }
        }

        /// <summary>
        /// Helper method to create a mem allocator, I usually
        /// just pull one off the pin I'm connecting to.
        /// </summary>
        public static IMemAllocator CreateIMemAllocator()
        {
            return (IMemAllocator)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(0x1e651cc0, 0xb199, 0x11d0, 0x82, 0x12, 0x00, 0xc0, 0x4f, 0xc3, 0x2c, 0x45)));
        }

        /// <summary>
        /// Implemented by the Pin implementation. Do your 
        /// connection acceptance testing and initialisation 
        /// here.
        /// </summary>
        public abstract HRESULT OnReceiveConnection(IPin pReceivePin, AMMediaType pmt);

        /// <summary>
        /// Implemented by the Pin implementation. Clean up
        /// here.
        /// </summary>
        public abstract HRESULT OnDisconnect();

        /// <summary>
        /// This is your start command. Begin your
        /// delivery/processing thread.
        /// </summary>
        internal virtual int Active()
        {
            return (int)HRESULT.S_OK;
        }

        /// <summary>
        /// This is your command. Close threads
        /// and stop pumping samples.
        /// </summary>
        internal virtual int Inactive()
        {
            return (int)HRESULT.S_OK;
        }

        #region IPin Members
		
		public static int SizeOfAMMediaType
		{
			get { return Marshal.SizeOf(typeof(AMMediaType)); }
		}

		public static void ToPtr(AMMediaType[] ar, IntPtr ptr)
		{
			for (int i = 0; i < ar.Length; i++)
			{
				if (null == (ar[i]))
					continue;

				IntPtr p = Marshal.AllocHGlobal(SizeOfAMMediaType);
				Marshal.StructureToPtr(ar[i], p, true);

				Marshal.WriteIntPtr(ptr, i, p);
				// IntPtr p = Marshal.ReadIntPtr(ptr, i * IntPtr.Size);

				// IntPtr thisPtr = (IntPtr)((long)ptr + (size * i));

			}
		}

		public static IntPtr ToPtr(AMMediaType[] ar)
		{
			if (ar == null
				|| ar.Length == 0)
				return IntPtr.Zero;

			int size = SizeOfAMMediaType;
			IntPtr ptr = Marshal.AllocHGlobal(IntPtr.Size * ar.Length);
			ToPtr(ar, ptr);
			return ptr;
		}

		public static AMMediaType[] FromIntPtr(IntPtr ptr, int length)
		{
			int size = SizeOfAMMediaType;
			List<AMMediaType> ar = new List<AMMediaType>();
			for (int i = 0; i < length; i++)
			{
				IntPtr p = Marshal.ReadIntPtr(ptr, i * IntPtr.Size);
				if (p != IntPtr.Zero)
				{
					// IntPtr thisPtr = (IntPtr)((long)ptr + (size * i));
					AMMediaType mt = (AMMediaType)Marshal.PtrToStructure(p, typeof(AMMediaType));
					ar.Add(mt);
				}
				else
				{
					ar.Add(new AMMediaType());
				}
			}
			return ar.ToArray();
		}

		public static AMMediaType FromIntPtr(IntPtr ptr)
		{
			// int size = Marshal.SizeOf(typeof(AMMediaType));
			AMMediaType mt = (AMMediaType)Marshal.PtrToStructure(ptr, typeof(AMMediaType));
			return mt;
		}

		public static bool MatchesPartial(AMMediaType source, AMMediaType other)
		{
			if (null == (other))
				return false;

			return source.majorType == other.majorType
				&& source.subType == other.subType
				&& source.formatType == other.formatType; // if the format block is specified then it must match exactly
		}

        public int Connect(IPin pReceivePin, AMMediaType pmt)
        {
			unchecked
			{
				if (_ConnectedPin != null)
				{
					return (int) HRESULT.VFW_E_ALREADY_CONNECTED;
				}

				PinDirection pd;
				pReceivePin.QueryDirection(out pd);
				if (pd == _Direction)
					return (int) HRESULT.VFW_E_INVALID_DIRECTION;

				PinInfo pi;
				pReceivePin.QueryPinInfo(out pi);
				FilterInfo fi;
				pi.filter.QueryFilterInfo(out fi);
				Debug.WriteLine(fi.achName + "." + pi.name, _Name + " connect attempt from");

				if (pi.name == "Subpicture Input")
				{

				}

				HRESULT hr = HRESULT.VFW_E_NO_ACCEPTABLE_TYPES;



				//
				// Try match our media types with the specific one requested
				// if any...
				//
				if (null!=(pmt))
				{
					for (int i = 0; i < MediaTypes.Count; i++)
					{
						if (MatchesPartial(MediaTypes[i], pmt))
						{
							hr = OnReceiveConnection(pReceivePin, MediaTypes[i]);
							if (hr != HRESULT.S_OK)
							{
								// pReceivePin.Disconnect();
							}
							else
								break;
						}
					}
				}

				//
				// Otherwise try force-connect all of the ones we like.
				//
				if (hr != HRESULT.S_OK)
				{
					for (int i = 0; i < MediaTypes.Count; i++)
					{

						AMMediaType newMT = new AMMediaType();
						Copy(MediaTypes[i], newMT);

						hr = (HRESULT)pReceivePin.QueryAccept(newMT);
						if (hr == HRESULT.S_OK)
						{
							hr = OnReceiveConnection(pReceivePin, newMT);
							if (hr != HRESULT.S_OK)
							{
								//pReceivePin.Disconnect();
							}
							else
								break;
						}
					}
				}

				//
				// Failing that, try connect with any of their numerous
				// supported types..
				//

				if (hr != HRESULT.S_OK)
				{
					IEnumMediaTypes en;
					pReceivePin.EnumMediaTypes(out en);
					en.Reset();

					/*
					 * Unsafe pointer edition
					 * ======================
					 * I was experminting AMMediaType as a value struct type
					 * for better marshalling.
					 * 
					AMMediaType* types = null;
					int fetched = 0;
					do
					{
						HRESULT r = en.Next(1, (AMMediaType**)&types, out fetched);
						if (r != HRESULT.S_OK
							|| fetched == 0)
							break;
						AMMediaType tmp = *types;
						for (int x = 0; x < MediaTypes.Count; x++)
						{
							if ((MediaTypes[x].majorType == tmp.majorType
								   && MediaTypes[x].subType == tmp.subType)
								   || AcceptsAnyMedia)
							{
								hr = OnReceiveConnection(pReceivePin, tmp); //MediaTypes[x]
                            
								if (hr != HRESULT.S_OK)
								{
									pReceivePin.Disconnect();
								}
								else
									break;
							}
						}
						AMMediaType.Free(tmp);
					} while (fetched > 0);*/

					AMMediaType[] types = new AMMediaType[100];
					IntPtr ptr = ToPtr(types);
					IntPtr fetched = IntPtr.Zero;
					en.Next(10, types, fetched);
					types = FromIntPtr(ptr, fetched.ToInt32());

					if (fetched.ToInt32() > 0
					    && types != null)
					{
						for (int x = 0; x < MediaTypes.Count; x++)
						{
							for (int i = 0; i < fetched.ToInt32(); i++)
							{
								if ((MediaTypes[x].majorType == types[i].majorType
								     && MediaTypes[x].subType == types[i].subType)
								    || AcceptsAnyMedia)
								{
									hr = OnReceiveConnection(pReceivePin, types[i]);
									if (hr != HRESULT.S_OK)
									{
										//pReceivePin.Disconnect();
									}
									else
										break;
								}
							}

							if (hr == HRESULT.S_OK)
								break;
						}
					}

					for (int i = 0; i < fetched.ToInt32(); i++)
						Free(types[i]);


					if (Marshal.IsComObject(en))
						Marshal.ReleaseComObject(en);
				}


				if (hr != HRESULT.S_OK)
				{
					// Final clean up
					this.Disconnect();

					if (Marshal.IsComObject(pReceivePin))
						Marshal.ReleaseComObject(pReceivePin);

					// Connect failed, but may have returned S_FALSE
					// so this ensures a better return code.
					hr = HRESULT.VFW_E_NO_ACCEPTABLE_TYPES;
				}

				if (hr == HRESULT.S_OK)
				{
					ResolveConnectedFilter();
				}

				return (int)hr;
			}
        }

        /// <summary>
        /// Another filter's output wants to connect to us
        /// </summary>
        public int ReceiveConnection(IPin pReceivePin, AMMediaType pmt)
        {
            _MediaType = pmt;
            _ConnectedPin = pReceivePin;

            HRESULT hr = OnReceiveConnection(pReceivePin, pmt);

            if (!HR.SUCCESS(hr))
            {
                _ConnectedPin = null;
                _MediaType = null;
            }
            else
            {
                ResolveConnectedFilter();
            }

			unchecked
			{
				return (int) hr;
			}
        }


        /// <summary>
        /// Helper method to obtain a little information about who 
        /// we just connected to.
        /// </summary>
        public void ResolveConnectedFilter()
        {
            if (_ConnectedPin == null)
                return;

            PinInfo pi;
            _ConnectedPin.QueryPinInfo(out pi);
            FilterInfo fi;
            pi.filter.QueryFilterInfo(out fi);
            _ConnectedFilter = pi.filter;
            _ConnectedFilterName = fi.achName;

            pi.filter.GetClassID(out _ConnectedFilterClassID);

            Debug.WriteLine(_ConnectedFilterClassID + " " + _ConnectedFilterName, "ResolveConnectedFilter"); 
        }


        public int Disconnect()
        {
            try
            {
                OnDisconnect();

                if (_ConnectedPin != null)
                {
                    _ConnectedPin.BeginFlush();
                    //_ConnectedPin.EndOfStream(); // deadlock - dont do this.
                    _ConnectedPin.Disconnect();
                    _ConnectedPin.EndFlush();
                }

                if (_ConnectedPin != null
                     && Marshal.IsComObject(_ConnectedPin))
                {
                    GC.ReRegisterForFinalize(_ConnectedPin);
                    Marshal.FinalReleaseComObject(_ConnectedPin);
                }

                _ConnectedPin = null;
            }
            catch (Exception x)
            {
                Debug.WriteLine("Failed to disconnect pin " + x);
            }

			return (int)HRESULT.S_OK; // Disconnect must not fail
        }

        public int ConnectedTo(out IPin ppPin)
        {
            ppPin = null;
			unchecked
			{
				try
				{
					if (_ConnectedPin != null)
						Marshal.AddRef(Marshal.GetComInterfaceForObject(_ConnectedPin, typeof (IPin)));
					ppPin = _ConnectedPin;
					return (ppPin != null) ? (int) HRESULT.S_OK : (int) HRESULT.VFW_E_NOT_CONNECTED;
				}
				catch (InvalidComObjectException)
				{
					Debug.WriteLine(_Name + ".ConnectedTo was called but the com object is no good.");
					_ConnectedPin = null;

					return (int) HRESULT.VFW_E_NOT_CONNECTED;
				}
			}
        }


		public static HRESULT Copy(AMMediaType source, AMMediaType dest)
		{
			Free(dest);

			dest.majorType = source.majorType;
			dest.subType = source.subType;
			dest.fixedSizeSamples = source.fixedSizeSamples;
			dest.temporalCompression = source.temporalCompression;
			dest.sampleSize = source.sampleSize;
			dest.formatType = source.formatType;

			dest.unkPtr = source.unkPtr; // IUnknown Pointer
			if (source.formatSize != 0
				&& source.formatPtr != IntPtr.Zero)
			{
				dest.formatSize = source.formatSize;
				dest.formatPtr = Marshal.AllocCoTaskMem(dest.formatSize);
				//Marshal.Copy(
				CopyMemory(dest.formatPtr, source.formatPtr, dest.formatSize);
				//Marshal.Copy(new IntPtr[] {source.formatPtr},  -1,dest.formatPtr, dest.formatSize);
			}
			return HRESULT.S_OK;
		}
		[DllImport("kernel32.dll", SetLastError = false)]
		private static unsafe extern bool CopyMemory(IntPtr dst, IntPtr src, int len);

        public int ConnectionMediaType(AMMediaType pmt)
        {
            if (_ConnectedPin != null)
            {
                Copy(_MediaType, pmt);
                return (int)HRESULT.S_OK;
            }
            else
            {
				unchecked
				{
					return (int) HRESULT.VFW_E_NOT_CONNECTED;
				}
            }
        }

		public int QueryPinInfo(out PinInfo pInfo)
        {
            pInfo = new PinInfo();
            pInfo.filter = (IBaseFilter)_Filter;
            pInfo.dir = _Direction;
            pInfo.name = _Name;
			return (int)HRESULT.S_OK;
        }

		public int QueryDirection(out PinDirection pPinDir)
        {
            pPinDir = _Direction;
			return (int)HRESULT.S_OK;
        }

		public int QueryId(out string Id)
        {
            Id = _Name;
			return (int)HRESULT.S_OK;
        }

		public virtual int QueryAccept(AMMediaType pmt)
        {
            for (int i = 0; i < MediaTypes.Count; i++)
            {
                if (MediaTypes[i].formatType == pmt.formatType
                    && MediaTypes[i].majorType == pmt.majorType
                    && MediaTypes[i].subType == pmt.subType)
					return (int)HRESULT.S_OK;
            }
			unchecked
			{
				return (int) HRESULT.VFW_E_TYPE_NOT_ACCEPTED;
			}
        }

		public int EnumMediaTypes(out IEnumMediaTypes ppEnum)
        {
            ppEnum = new EnumMediaTypes(MediaTypes.ToArray());
			return (int)HRESULT.S_OK;
        }

        public int QueryInternalConnections(IPin[] ppPins, ref int nPin)
        {
            Debug.WriteLine("QueryInternalConnections", _Name);
            nPin = 0;
			unchecked
			{
				return (int) HRESULT.E_NOTIMPL;
			}
        }

		public virtual int EndOfStream()
        {
			return (int)HRESULT.S_OK;
        }

		public virtual int BeginFlush()
        {
			return (int)HRESULT.S_OK;
        }


		public virtual int EndFlush()
        {
			return (int)HRESULT.S_OK;
        }

		public virtual int NewSegment(long tStart, long tStop, double dRate)
        {
			return (int)HRESULT.S_OK;
        }


        #endregion

		public static void Free(AMMediaType pmt)
		{
			if (pmt.unkPtr != IntPtr.Zero)
			{
				Marshal.Release(pmt.unkPtr);
				pmt.unkPtr = IntPtr.Zero;
			}
		}

        ~BasePin()
        {
            if (_MediaType != null)
                Free(_MediaType);
            for (int i = 0; i < MediaTypes.Count; i++)
                Free(MediaTypes[i]);
        }
    }
}
