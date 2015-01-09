using HashMatcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace HashMatcher.Util
{
  public class FileUtils
  {
    private static string TEMP_FILE = "subtitledownloader_temp";

    public static string AssemblyDirectory
    {
      get
      {
        return Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
      }
    }

    public static string GetTempFileName()
    {
      int num = new Random().Next(0, 10);
      return Path.Combine(Path.GetTempPath(), FileUtils.TEMP_FILE + (object) num);
    }

    public static string GetFileNameForSubtitle(string subtitleFile, string languageCode, string videoFile)
    {
      string path = videoFile;
      string withoutExtension = Path.GetFileNameWithoutExtension(path);
      string directoryName = Path.GetDirectoryName(path);
      string extension = Path.GetExtension(subtitleFile);
      string languageName = Languages.GetLanguageName(languageCode);
      return directoryName + (object) Path.DirectorySeparatorChar + withoutExtension + "." + languageName + extension;
    }

    public static void WriteNewFile(string fileNameWithPath, byte[] fileData)
    {
      using (FileStream fileStream = new FileStream(fileNameWithPath, FileMode.CreateNew))
      {
        using (BinaryWriter binaryWriter = new BinaryWriter((Stream) fileStream))
          binaryWriter.Write(fileData);
      }
    }

    private static byte[] ComputeMovieHash(string filename)
    {
        byte[] result;
        using (Stream input = File.OpenRead(filename))
        {
            result = ComputeMovieHash(input);
        }
        return result;
    }

    private static byte[] ComputeMovieHash(Stream input)
    {
        long lhash, streamsize;
        streamsize = input.Length;
        lhash = streamsize;

        long i = 0;
        byte[] buffer = new byte[sizeof(long)];
        while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
        {
            i++;
            lhash += BitConverter.ToInt64(buffer, 0);
        }

        input.Position = Math.Max(0, streamsize - 65536);
        i = 0;
        while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
        {
            i++;
            lhash += BitConverter.ToInt64(buffer, 0);
        }
        input.Close();
        byte[] result = BitConverter.GetBytes(lhash);
        Array.Reverse(result);
        return result;
    }

    public static string HexadecimalHash(string filename)
    {
        return ToHexadecimal(ComputeMovieHash(filename));
    }

    private static string ToHexadecimal(byte[] bytes)
    {
        StringBuilder hexBuilder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            hexBuilder.Append(bytes[i].ToString("x2"));
        }
        return hexBuilder.ToString();
    }
  }
}
