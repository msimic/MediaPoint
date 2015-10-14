using MediaPoint.MVVM.Services;
using SubtitleDownloader.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPoint.VM.Services.Model
{
    public interface ISubtitleDownloaderRegistrator : IService
    {
        void RegisterDownloader(ISubtitleDownloader downloader);
    }
}
