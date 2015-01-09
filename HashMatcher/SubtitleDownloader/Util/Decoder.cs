using System;
using System.IO;
using System.IO.Compression;

namespace HashMatcher.Util
{
  internal class Decoder
  {
    public static byte[] DecodeAndDecompress(string str)
    {
      return Decoder.Decompress(Decoder.Decode(str));
    }

    public static byte[] Decode(string str)
    {
      return Convert.FromBase64String(str);
    }

    public static byte[] Decompress(byte[] b)
    {
      using (MemoryStream memoryStream = new MemoryStream(b.Length))
      {
        memoryStream.Write(b, 0, b.Length);
        memoryStream.Seek(-4L, SeekOrigin.Current);
        byte[] buffer1 = new byte[4];
        memoryStream.Read(buffer1, 0, 4);
        int count = BitConverter.ToInt32(buffer1, 0);
        memoryStream.Seek(0L, SeekOrigin.Begin);
        byte[] buffer2 = new byte[count];
        using (GZipStream gzipStream = new GZipStream((Stream) memoryStream, CompressionMode.Decompress))
        {
          gzipStream.Read(buffer2, 0, count);
          return buffer2;
        }
      }
    }
  }
}
