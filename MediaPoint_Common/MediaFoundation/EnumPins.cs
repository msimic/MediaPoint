using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using DirectShowLib;

namespace MediaPoint.Common.MediaFoundation
{
    [ComVisible(true)]
    public unsafe class EnumPins : IEnumPins
    {
        IPin[] _Items;
        int _Index;
        public EnumPins(IPin[] list)
        {
			GC.SuppressFinalize(this);
            _Index = 0;
            _Items = list;
        }

        #region IEnumPins Members

        public int Next(int cPins, IPin[] ppPins, IntPtr pcFetched)
        {

            

            int fetched = 0;

            if (_Items == null)
                throw new Exception("CEnumPins.List<CBasePin> is null. This should be impossible.");

            if (cPins == 0
                || ppPins==null)
				return 1;

           // ppPins = new IPin[cPins];
            int c = 0;
            for (int i = _Index; i < _Items.Length && c < ppPins.Length; i++)
            {
				GC.SuppressFinalize(_Items[i]);
                ppPins[c] = _Items[i];
                fetched++;
                c++;
            }

            //for (int i = _Index; i < _Items.Length && i < (ppPins.Length + _Index); i++)
            //{
            //    ppPins[_Index - i] = _Items[i];
            //    _Index++;
            //    fetched++;
            //}
            _Index += fetched;
			GC.SuppressFinalize(pcFetched);
			GC.SuppressFinalize(ppPins);
        	//*((int*)pcFetched.ToPointer()) = fetched;

            return (fetched == cPins ? 0 : 1);
        }

        public int Skip(int cPins)
        {
            if (_Index + cPins > _Items.Length)
                return 1;
            _Index += cPins;
            return 0;
        }

        public int Reset()
        {
            _Index = 0;
            return 0;
        }

        public int Clone(out IEnumPins ppEnum)
        {
            ppEnum = new EnumPins(_Items);
            return 0;
        }

        #endregion
    }

}
