using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using DirectShowLib;

namespace MediaPoint.Common.MediaFoundation
{
    [ComVisible(true)]
    public unsafe class EnumMediaTypes : IEnumMediaTypes
    {
        AMMediaType[] _types;
      
        int _Index;
        public EnumMediaTypes(AMMediaType[] types)
        {
            _Index = 0;
            _types = types;
        }

        #region IEnumPins Members

        public int Next(int cMediaTypes, AMMediaType[] pppMediaTypes, IntPtr pcFetched)
        {

            AMMediaType[] ppMediaTypes = new AMMediaType[cMediaTypes];
            var cFetched = 0;

			unchecked
        	{
        		if (cMediaTypes == 0)
        			return (int) HRESULT.E_INVALIDARG;
        	}

        	//*ppMediaTypes = new AMMediaType[cMediaTypes];
            for (int i = _Index; i < cMediaTypes && i < _types.Length; i++)
            {
                AMMediaType mt = _types[i];
				if (null == mt)
					break;
               
                ppMediaTypes[i] = mt;
            	pppMediaTypes[i] = mt;
                cFetched = cFetched + 1;
            }

			_Index += cFetched;
            return (cFetched == cMediaTypes ? (int)HRESULT.S_OK : (int)HRESULT.S_FALSE);
        }

        public int Skip(int cPins)
        {
  
            _Index += cPins;

            /*  See if we're over the end */
            return _types.Length > _Index ? (int)HRESULT.S_OK : (int)HRESULT.S_FALSE;
        }

        public int Reset()
        {
            _Index = 0;
            return (int)HRESULT.S_OK;
        }

        public int Clone(out IEnumMediaTypes ppEnum)
        {
            ppEnum = new EnumMediaTypes(_types);
            return (int)HRESULT.S_OK;
        }

        #endregion
    }
}
