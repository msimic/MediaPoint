using MediaPoint.MVVM.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPoint.Interfaces
{
    public interface IPlateProcessor : IService
    {
#if ALPR
        void ProcessPlate(string text, int left, int top, int right, int bottom, double angle, int confidence, string nattext, int natconf, string natplate);
#endif
    }
}
