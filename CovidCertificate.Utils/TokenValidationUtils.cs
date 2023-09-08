using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace CovidCertificate.Backend.Utils
{
    public static class TokenValidationUtils
    {
        public static bool IsTokenCloseToExpire(JwtSecurityToken token,
            int minimumSecondsBeforeExpiry,
            DateTime utcNow)
        {
            return token.ValidTo.AddSeconds(-minimumSecondsBeforeExpiry) < utcNow;
        }

        public static bool CheckAudiencesMatch(JwtSecurityToken token,
            string audience)
        {
            return token.Audiences.FirstOrDefault() == audience;
        }

        public static DateTime ParseTokenClaimDobToDateTime(string dateOfBirthTicks)
        {
            if (dateOfBirthTicks != default)
            {
                var isLong = long.TryParse(dateOfBirthTicks, out var dobResult);
                if (isLong)
                {
                    var dateOfBirth = DateTime.FromFileTimeUtc(dobResult);
                    return dateOfBirth;
                }
            }
            throw new DataMisalignedException("Unable to parse " + dateOfBirthTicks + "to DateTime");
        }

        public static bool TokensBelongToSamePerson(string formattedToken,
            string idToken)
        {
            string nhsNumberToken = JwtTokenUtils.GetClaim(formattedToken, JwtTokenUtils.NhsNumberClaimName);
            string nhsNumberIdToken = JwtTokenUtils.GetClaim(idToken, JwtTokenUtils.NhsNumberClaimName);

            return nhsNumberToken.Equals(nhsNumberIdToken);
        }
    }
}
