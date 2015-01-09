using CookComputing.XmlRpc;

namespace OpenSubtitlesSearch
{
  public class subInfo
  {
    [XmlRpcMissingMapping(MappingAction.Ignore)]
    public string sublanguageid { get; set; }

    public string moviehash { get; set; }

    [XmlRpcMissingMapping(MappingAction.Ignore)]
    public int? moviebytesize { get; set; }

    [XmlRpcMissingMapping(MappingAction.Ignore)]
    public int? imdbid { get; set; }

    public string query { get; set; }

    public subInfo(string sublanguageid, string moviehash, int? moviebytesize, int? imdbid, string query)
    {
      this.sublanguageid = sublanguageid;
      this.moviehash = moviehash;
      this.moviebytesize = moviebytesize;
      this.imdbid = imdbid;
      this.query = query;
    }
  }
}
