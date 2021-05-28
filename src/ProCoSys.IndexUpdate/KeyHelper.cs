using System;
using System.Text;

namespace ProCoSys.IndexUpdate
{
    public static class KeyHelper
    {
        public static string GenerateKey(string keyString)
        {
            var keyBytes = Encoding.UTF8.GetBytes(keyString);
            return Convert.ToBase64String(keyBytes).Replace("/", "_").Replace("+", "-"); // URL safe base64
        }
    }
}
