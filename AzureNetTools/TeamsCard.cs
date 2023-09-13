using System.Collections.Generic;

namespace AzureNetTools
{
    public class TeamsCard
    {
        public string Type { get; set; }

        public string Context { get; set; }

        public string ThemeColor { get; set; }

        public string Title { get; set; }

        public string Text { get; set; }

        public List<Section> Sections { get; set; }
    }
}
