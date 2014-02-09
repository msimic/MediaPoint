using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MediaPoint.Common.Interfaces;

namespace MediaPoint.Common.MediaPlayers
{
    [ComVisible(true)]
    public class AudioCallback : IDCDSPFilterPCMCallBack
    {
        public int PCMDataCB(ref int Buffer, int Length, ref int NewSize, ref TDSStream Stream)
        {
            return 0;
        }

        public int MediaTypeChanged(ref TDSStream Stream)
        {
            return 0;
        }

        public int Flush()
        {
            return 0;
        }
    }
}
