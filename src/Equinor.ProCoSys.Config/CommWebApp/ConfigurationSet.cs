using System.Collections.Generic;

namespace Equinor.ProCoSys.Config.CommWebApp
{
    public class ConfigurationSet
    {
        public Dictionary<string, object> Configuration { get; } = new Dictionary<string, object>();
        public Dictionary<string, bool> FeatureFlags { get; } = new Dictionary<string, bool>();
    }
}
