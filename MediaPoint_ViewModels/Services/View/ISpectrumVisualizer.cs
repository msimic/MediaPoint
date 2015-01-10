using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPoint.MVVM.Services;

namespace MediaPoint.VM.ViewInterfaces
{
    public interface ISpectrumVisualizer : IService
    {
        void SetNumSamples(int num);
        void DisplayFFTData(float[] data);
        void SetStreamInfo(int channels, int bits, int frequency);
    }
}
