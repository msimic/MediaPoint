using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPoint.Common.Interfaces
{
    public interface IEqualizer
    {
        void SetBand(int channel, int band, sbyte value);
    }
}
