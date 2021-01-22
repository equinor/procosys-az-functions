using System;
using System.Linq;

namespace Equinor.ProCoSys.Config
{
    public static class EnvironmentSelector
    {
        public static string GetEnvironment(string origin)
        {
            return "dev";
            var devOrigins = Environment.GetEnvironmentVariable("DevOrigins").Split(';').ToList();
            var testOrigins = Environment.GetEnvironmentVariable("TestOrigins").Split(';').ToList();
            var prodOrigins = Environment.GetEnvironmentVariable("ProdOrigins").Split(';').ToList();
            if (devOrigins.Contains(origin))
            {
                return "dev";
            }
            else if (testOrigins.Contains(origin))
            {
                return "test";
            }
            else if (prodOrigins.Contains(origin))
            {
                return "prod";
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
