using MediaPoint.MVVM.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPoint.VM.Services.Model
{
    public interface ISettings : IService
    {
        double SubtitleMinScore { get; set; }
        List<string> SubtitleLanguagesCodes { get; }
        bool PreferenceToHashMatchedSubtitle { get; }
    }
}
