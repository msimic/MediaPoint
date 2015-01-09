using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace SubtitleDownloader
{
  [CompilerGenerated]
  [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
  [DebuggerNonUserCode]
  internal class Configuration
  {
    private static ResourceManager resourceMan;
    private static CultureInfo resourceCulture;

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static ResourceManager ResourceManager
    {
      get
      {
        if (object.ReferenceEquals((object) Configuration.resourceMan, (object) null))
          Configuration.resourceMan = new ResourceManager("SubtitleDownloader.Configuration", typeof (Configuration).Assembly);
        return Configuration.resourceMan;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo Culture
    {
      get
      {
        return Configuration.resourceCulture;
      }
      set
      {
        Configuration.resourceCulture = value;
      }
    }

    internal static string OpenSubtitlesUserAgent
    {
      get
      {
        return Configuration.ResourceManager.GetString("OpenSubtitlesUserAgent", Configuration.resourceCulture);
      }
    }

    internal static string S4UApiKey
    {
      get
      {
        return Configuration.ResourceManager.GetString("S4UApiKey", Configuration.resourceCulture);
      }
    }

    internal static string SublightApiKey
    {
      get
      {
        return Configuration.ResourceManager.GetString("SublightApiKey", Configuration.resourceCulture);
      }
    }

    internal static string SubLightClientId
    {
      get
      {
        return Configuration.ResourceManager.GetString("SubLightClientId", Configuration.resourceCulture);
      }
    }

    internal Configuration()
    {
    }
  }
}
