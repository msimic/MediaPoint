using MediaPoint.MVVM.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace MediaPoint.Common.Services
{
    public interface IMainWindow : IService
    {
        Window GetWindow();
        IntPtr GetWindowHandle();
        void SetChildWindowsFollow(bool value);
    }
}
