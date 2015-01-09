using CookComputing.XmlRpc;

namespace OpenSubtitlesSearch
{
  public class subdata
  {
    [XmlRpcMissingMapping(MappingAction.Ignore)]
    public subtitle[] data;

    public string status { get; set; }

    public double seconds { get; set; }
  }
}
