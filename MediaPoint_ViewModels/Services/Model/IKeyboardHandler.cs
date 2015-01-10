using MediaPoint.MVVM.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace MediaPoint.VM.ViewInterfaces
{
    public interface IKeyboardHandler : IService
    {
        bool HandleKey(Key key, bool isControl, bool isAlt, bool isShift, bool isExternal);
    }
}
