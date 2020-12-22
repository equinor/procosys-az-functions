using System.Collections.Generic;

namespace Equinor.ProCoSys.Config
{
    public class ConfigurationSet
    {
        public Dictionary<string, string> Configuration { get; } = new Dictionary<string, string>();
        public Dictionary<string, bool> FeatureFlags { get; } = new Dictionary<string, bool>();
    }
}
