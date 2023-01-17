using System;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace CovidCertificate.Backend.Utils
{
    public static class JwtTokenUtils
    {
        public const string NhsNumberClaimName = "nhs_number";
        public const string NhsBirthDateClaimName = "birthdate";
        public const string IdTokenHeaderName = "NHSD-User-Identity";

        public static string GetClaim(string token, string claim)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadJwtToken(token);

            var stringClaimValue = securityToken.Claims.FirstOrDefault(c => c.Type == claim)?.Value;

            return stringClaimValue;
        }

        public static string CalculateHashFromIdToken(string token)
        {
            var birthDate = GetClaim(token, NhsBirthDateClaimName).Replace("-", String.Empty); // date is in format 'yyyy-MM-dd' so we need to remove '-'
            var nhsNumber = GetClaim(token, NhsNumberClaimName);
               
            var hash = HashUtils.GenerateHash(nhsNumber, birthDate);

            return hash;
        }

        public static string GetFormattedAuthToken(HttpRequest request)
        {
            string token = request.Headers["Authorization"];

            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            return token.Substring(token.ToLower().StartsWith("bearer") ? 7 : 0);
        }

    }
}
