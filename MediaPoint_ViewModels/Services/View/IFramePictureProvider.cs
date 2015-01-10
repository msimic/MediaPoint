using MediaPoint.MVVM.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace MediaPoint.VM.ViewInterfaces
{
    public interface IFramePictureProvider : IService
    {
        BitmapSource GetBitmapOfVideoElement();
    }
}
