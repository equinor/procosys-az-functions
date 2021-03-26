using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Equinor.ProCoSys.Config
{
    public static class EnvironmentSelector
    {
        public static string GetEnvironment(string origin)
        {
            var devOrigins = Environment.GetEnvironmentVariable("DevOrigins")?.Split(';').ToList();
            var testOrigins = Environment.GetEnvironmentVariable("TestOrigins")?.Split(';').ToList();
            var prodOrigins = Environment.GetEnvironmentVariable("ProdOrigins")?.Split(';').ToList();

            var templateStrings = devOrigins?.Where(d => d.Contains("*"));

            if ((
                from templateString in templateStrings
                where origin.ContainsLike(templateString) select templateString).Any())
            {
                return "dev";
            }

            if (devOrigins != null && devOrigins.Contains(origin))
            {
                return "dev";
            }

            if (testOrigins != null && testOrigins.Contains(origin))
            {
                return "test";
            }

            if (prodOrigins != null && prodOrigins.Contains(origin))
            {
                return "prod";
            }

            return string.Empty;
        }
    }

    public static class WildcardStringExtensions 
    {
        public static bool ContainsLike(this string source, string like)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(like))
            {
                return false;
            }

            return Regex.IsMatch(
                source, 
                "^" + like.Replace("*", "[0-9]+") + "$");
        }
	}
}
