using System.Collections.Generic;

namespace AzureNetTools
{
    public class Section
    {
        public bool StartGroup { get; set; }

        public string Title { get; set; }

        public List<Fact> Facts { get; set; }
    }
}
