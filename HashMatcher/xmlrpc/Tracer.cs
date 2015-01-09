using CookComputing.XmlRpc;
using System.Diagnostics;
using System.IO;

namespace xmlrpc
{
  public class Tracer : XmlRpcLogger
  {
    protected override void OnRequest(object sender, XmlRpcRequestEventArgs e)
    {
      this.DumpStream(e.RequestStream);
    }

    protected override void OnResponse(object sender, XmlRpcResponseEventArgs e)
    {
      this.DumpStream(e.ResponseStream);
    }

    private void DumpStream(Stream stm)
    {
      stm.Position = 0L;
      TextReader textReader = (TextReader) new StreamReader(stm);
      for (string message = textReader.ReadLine(); message != null; message = textReader.ReadLine())
        Trace.WriteLine(message);
    }
  }
}
