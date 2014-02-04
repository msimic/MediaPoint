/*
 * C# filter base class
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

namespace MediaPoint.Common.MediaFoundation
{
    [ComVisible(true)]
    public class BaseFilter : IBaseFilter
    {
        public static readonly Guid TIME_FORMAT_MEDIA_TIME = new Guid(0x7b785574, 0x8c82, 0x11cf, 0xbc, 0xc, 0x0, 0xaa, 0x0, 0xac, 0x74, 0xf6);

        internal FilterState _State;
        internal IReferenceClock _Clock;
        internal IFilterGraph _Graph;
        internal List<BasePin> Pins = new List<BasePin>();
        internal string _Name;
        internal long m_tStart;

        // Locking will often cause deadlocks. As a rule
        // I only lock when absolutely necessary, and that
        // changes on filter-by-filter basis. More often
        // than not, you can avoid lock complexity by 
        // designing your parsing code carefully.
        internal object _LockObj = new object();

        /// <summary>
        /// Override this in your Filter
        /// </summary>
        public virtual int OnStop()
        {
            return (int)HRESULT.S_OK;
        }

        /// <summary>
        /// Override this in your Filter
        /// </summary>
        public virtual int OnPause()
        {
			return (int)HRESULT.S_OK;
        }

        /// <summary>
        /// Override this in your Filter
        /// </summary>
        public virtual int OnRun(long tStart)
        {
			return (int)HRESULT.S_OK;
        }

        #region IBaseFilter Members

        public int GetClassID(out Guid pClassID)
        {
            pClassID = this.GetType().GUID;
			return (int)HRESULT.S_OK;
        }

        public int Stop()
        {
            //lock (_LockObj)
            //{
                _State = FilterState.Stopped;
                OnStop();
				return (int)HRESULT.S_OK;
            //}
        }

        public int Pause()
        {
            Debug.WriteLine("Pause", _Name);

            //lock (_LockObj)
            //{
                // Notify all pins of the change to active state
                if (_State == FilterState.Stopped)
                {
                    for (int i = 0; i < Pins.Count; i++)
                    {
                        // Disconnected pins are not activated - this saves pins
                        // worrying about this state themselves
                        if (Pins[i].IsConnected)
                        {
							HRESULT hr = (HRESULT)Pins[i].Active();
                            if (HR.FAILED(hr))
                            {
								unchecked
								{
									return (int) hr;
								}
                            }
                        }
                    }
                }
                _State = FilterState.Paused;

                return OnPause();
           // }
        }

        public int Run(long tStart)
        {
            Debug.WriteLine("Run " + tStart, _Name);

            lock (_LockObj)
            {
                // Remember the stream time offset
                m_tStart = tStart;

                if (_State == FilterState.Stopped)
                {
                    HRESULT hr = (HRESULT)Pause();

					unchecked
					{
						if (HR.FAILED(hr))
							return (int) hr;
					}

                }

                _State = FilterState.Running;

                return OnRun(tStart);
            }
           
        }

        public int GetState(int dwMilliSecsTimeout, out FilterState filtState)
        {
			unchecked
			{
				//lock (_LockObj)
				//{
				filtState = _State;
				return (int) HRESULT.E_UNEXPECTED; //.S_OK;
				//}
			}
        }


        public virtual int SetSyncSource(IReferenceClock pClock)
        {
            _Clock = pClock;
			return (int)HRESULT.S_OK;
        }

        public virtual int GetSyncSource(out IReferenceClock pClock)
        {
            pClock = _Clock;
			return (int)HRESULT.S_OK;
        }

        public int EnumPins(out IEnumPins ppEnum)
        {
            ppEnum = (IEnumPins)(new EnumPins(Pins.ToArray()));
			return (int)HRESULT.S_OK;
        }

        public int FindPin(string Id, out IPin ppPin)
        {
            ppPin = null;
            for (int i = 0; i < Pins.Count; i++)
                if (String.Compare(Pins[i]._Name, Id, true) == 0)
                {
                    ppPin = Pins[i];
					return (int)HRESULT.S_OK;
                }
			return (int)HRESULT.S_FALSE;
        }

        public int QueryFilterInfo(out FilterInfo pInfo)
        {
            pInfo = new FilterInfo();
            pInfo.achName = _Name;
            pInfo.pGraph = _Graph;
			return (int)HRESULT.S_OK;
        }

        public int JoinFilterGraph(IFilterGraph pGraph, string pName)
        {
            _Graph = pGraph;
            _Name = pName;

            return OnJoinFilterGraph();
        }

        protected virtual int OnJoinFilterGraph()
        {
			return (int)HRESULT.S_OK;
        }

        public int QueryVendorInfo(out string pVendorInfo)
        {
			unchecked
			{
				pVendorInfo = null;
				return (int) HRESULT.E_NOTIMPL;
			}
        }

        #endregion
    }
}
