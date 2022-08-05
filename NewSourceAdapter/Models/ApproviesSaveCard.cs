using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewSourceAdapter.Models
{
    public class ApproviesSaveCard
    {
        public List<string> Approvies { get; set; }

        public ApproviesSaveCard() { }
        public ApproviesSaveCard(HashSet<string> approviesHashSet)
        {
            Approvies = approviesHashSet.ToList();
        }

        public HashSet<string> GetApproviesHashSet()
        {
            return Approvies.ToHashSet();
        }
    }
}
