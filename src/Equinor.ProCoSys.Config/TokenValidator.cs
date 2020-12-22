using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace Equinor.ProCoSys.Config
{
    public static class TokenValidator
    {
        public static bool TryValidate(string token, out JwtSecurityToken securityToken, string discoveryEndpoint, string audience, string issuer)
        {
            try
            {
                var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    discoveryEndpoint,
                    new OpenIdConnectConfigurationRetriever());

                OpenIdConnectConfiguration config = configManager.GetConfigurationAsync().Result;
                TokenValidationParameters validationParameters = new TokenValidationParameters
                {
                    //decode the JWT to see what these values should be
                    ValidAudience = audience,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    IssuerSigningKeys = config.SigningKeys,
                    ValidateLifetime = true
                };

                JwtSecurityTokenHandler tokendHandler = new JwtSecurityTokenHandler();

                tokendHandler.ValidateToken(token, validationParameters, out SecurityToken jwt);

                securityToken = jwt as JwtSecurityToken;
                return true;
            }
            catch (Exception)
            {
                securityToken = null;
                return false;
            }
        }
    }
}
