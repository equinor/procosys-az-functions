using System;
using System.Collections.Generic;

namespace ProCoSys.IndexUpdate.IndexModel
{
    class IndexDocument
    {
        public string Key { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Plant { get; set; }
        public string PlantName { get; set; }
        public string Project { get; set; }
        public List<string> ProjectNames { get; set; }
        public CommPkg CommPkg { get; set; }
        public McPkg McPkg { get; set; }
        public Tag Tag { get; set; }
    }
}
