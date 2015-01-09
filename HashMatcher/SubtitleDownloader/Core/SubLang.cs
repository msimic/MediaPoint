namespace HashMatcher
{
  internal class SubLang
  {
    public string Code { get; private set; }

    public string Name { get; private set; }

    public SubLang(string code, string name)
    {
      this.Code = code;
      this.Name = name;
    }
  }
}
