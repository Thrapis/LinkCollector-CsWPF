using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewSourceAdapter.Models
{
    public class ApplicationState
    {
        public string Prefix { get; set; }
        public string Url { get; set; }
        public string Pattern { get; set; }

        public ApplicationState(string prefix, string url, string pattern)
        {
            Prefix = prefix;
            Url = url;
            Pattern = pattern;
        }
    }
}
