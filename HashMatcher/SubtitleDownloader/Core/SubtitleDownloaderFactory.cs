using HashMatcher.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace HashMatcher
{
  public static class SubtitleDownloaderFactory
  {
    private static readonly Dictionary<string, ISubtitleDownloader> DownloaderInstances = new Dictionary<string, ISubtitleDownloader>();

    static SubtitleDownloaderFactory()
    {
      try
      {
        SubtitleDownloaderFactory.CreateDownloaderInstances(SubtitleDownloaderFactory.FindDownloaderImplementations());
      }
      catch (ReflectionTypeLoadException ex)
      {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Following loader exceptions were found: ");
        foreach (Exception exception in ex.LoaderExceptions)
          stringBuilder.AppendLine(exception.Message);
        throw new Exception(stringBuilder.ToString(), (Exception) ex);
      }
    }

    public static List<string> GetSubtitleDownloaderNames()
    {
      return new List<string>((IEnumerable<string>) SubtitleDownloaderFactory.DownloaderInstances.Keys);
    }

    public static ISubtitleDownloader GetSubtitleDownloader(string downloaderName)
    {
      if (!SubtitleDownloaderFactory.DownloaderInstances.ContainsKey(downloaderName))
        throw new ArgumentException("No subtitle downloader implementation found with downloader name: " + downloaderName);
      return SubtitleDownloaderFactory.DownloaderInstances[downloaderName];
    }

    private static IEnumerable<Type> FindDownloaderImplementations()
    {
      List<Type> list = new List<Type>();
      list.AddRange(SubtitleDownloaderFactory.TypesImplementingInterface(Assembly.GetExecutingAssembly(), typeof (ISubtitleDownloader)));
      string path1 = FileUtils.AssemblyDirectory + "\\SubtitleDownloaders";
      if (Directory.Exists(path1))
      {
        foreach (string path2 in Enumerable.Where<string>((IEnumerable<string>) Directory.GetFiles(path1), (Func<string, bool>) (s => s.EndsWith("dll"))))
        {
          try
          {
            Assembly assembly = Assembly.LoadFile(path2);
            list.AddRange(SubtitleDownloaderFactory.TypesImplementingInterface(assembly, typeof (ISubtitleDownloader)));
          }
          catch (Exception ex)
          {
          }
        }
      }
      return (IEnumerable<Type>) list;
    }

    private static void CreateDownloaderInstances(IEnumerable<Type> downloaderImplementations)
    {
      foreach (ISubtitleDownloader subtitleDownloader in Enumerable.Select<Type, ISubtitleDownloader>(downloaderImplementations, (Func<Type, ISubtitleDownloader>) (type => (ISubtitleDownloader) Activator.CreateInstance(type))))
        SubtitleDownloaderFactory.DownloaderInstances.Add(subtitleDownloader.GetName(), subtitleDownloader);
    }

    private static IEnumerable<Type> TypesImplementingInterface(Assembly assembly, Type desiredType)
    {
      return Enumerable.Where<Type>((IEnumerable<Type>) assembly.GetTypes(), (Func<Type, bool>) (type =>
      {
        if (desiredType.IsAssignableFrom(type))
          return SubtitleDownloaderFactory.IsRealClass(type);
        return false;
      }));
    }

    private static bool IsRealClass(Type testType)
    {
      if (!testType.IsAbstract && !testType.IsGenericTypeDefinition)
        return !testType.IsInterface;
      return false;
    }
  }
}
