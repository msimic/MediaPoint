using CookComputing.XmlRpc;

namespace OpenSubtitlesSearch
{
  public interface IOpenSubtitlesProxy : IXmlRpcProxy
  {
    [XmlRpcMethod("ServerInfo")]
    XmlRpcStruct ServerInfo();

    [XmlRpcMethod("LogIn")]
    XmlRpcStruct LogIn(string username, string password, string language, string useragent);

    [XmlRpcMethod("LogOut")]
    XmlRpcStruct LogOut(string token);

    [XmlRpcMethod("SearchSubtitles")]
    subrt SearchSubtitles(string token, subInfo[] subs);

    [XmlRpcMethod("DownloadSubtitles")]
    subdata DownloadSubtitles(string token, string[] subs);
  }
}
