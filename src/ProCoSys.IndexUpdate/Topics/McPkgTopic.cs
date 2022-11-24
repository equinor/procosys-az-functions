using System;
using System.Collections.Generic;

namespace ProCoSys.IndexUpdate.Topics
{
    class McPkgTopic
    {
        public string Plant { get; set; }
        public string PlantName { get; set; }
        public string ProjectName { get; set; }
        public string ProCoSysGuid { get; set; }
        public string Behavior { get; set; }
        public string McPkgNo { get; set; }
        public string CommPkgNo { get; set; }
        public string Description { get; set; }
        public string Remark { get; set; }
        public string ResponsibleCode { get; set; }
        public string ResponsibleDescription { get; set; }
        public string AreaCode { get; set; }
        public string AreaDescription { get; set; }
        public string Discipline { get; set; }
        public string CommPkgNoOld { get; set; }
        public string McPkgNoOld { get; set; }
        public List<string> ProjectNames { get; set; }
        public DateTime LastUpdated { get; set; }
        public const string TopicName = "mcpkg";
    }
}
