using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPoint.Common.Interfaces
{
    public interface ITag
    {
        string Id { get; set; }
        string Name { get; set; }
    }

    public class Tag : ITag
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public Tag() { }
        
        public Tag(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
