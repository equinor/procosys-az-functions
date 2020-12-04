using System.Collections.Generic;

namespace Equinor.ProCoSys.Config
{
    public class Feature
    {
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public string Id { get; set; }
        public Conditions Conditions { get; set; }
    }

    public class Conditions
    {
        public IEnumerable<ClientFilters> Client_Filters { get; set; }
    }

    public class ClientFilters
    {
        public string Name { get; set; }
        public Parameters Parameters { get; set; }
    }

    public class Parameters
    {
        public Audience Audience { get; set; }
    }

    public class Audience
    {
        public IEnumerable<string> Users { get; set; }
        public IEnumerable<Group> Groups { get; set; }
        public int DefaultRolloutPercentage { get; set; }
    }

    public class Group
    {
        public string Name { get; set; }
        public int RolloutPercentage { get; set; }
    }
}
