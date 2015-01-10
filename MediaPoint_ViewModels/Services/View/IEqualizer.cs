using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPoint.MVVM.Services;

namespace MediaPoint.VM.ViewInterfaces
{
    public interface IEqualizer : IService
    {
        void SetBand(int channel, int band, sbyte value);
    }
}
