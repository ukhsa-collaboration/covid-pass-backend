using Newtonsoft.Json;
using System;
using System.Security.Claims;
using CovidCertificate.Backend.Models.Enums;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class NhsUserInfo
    {
        [JsonProperty("email")]
        public string Email { get; private set; }
        [JsonProperty("family_name")]
        public string FamilyName { get; private set; }
        [JsonProperty("given_name")]
        public string GivenName { get; private set; }
        [JsonProperty("nhs_number")]
        public string NhsNumber { get; private set; }
        [JsonProperty("birthdate")]
        public DateTime Birthdate { get; private set; }
        [JsonProperty("phone_number")]
        public string PhoneNumber { get; private set; }
        [JsonProperty("identity_proofing_level")]
        public IdentityProofingLevel IdentityProofingLevel { get; private set; }
        [JsonProperty("phone_number_pds_matched")]
        public string PhoneNumberPdsMatched { get; private set; }
        [JsonProperty("gp_registration_details")]
        public GPRegistrationDetails GPRegistrationDetails { get; private set; }

        public ClaimsIdentity GetClaims()
        {
            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.Name, GivenName + " " + FamilyName));
            claims.AddClaim(new Claim(ClaimTypes.Surname, FamilyName));
            claims.AddClaim(new Claim(ClaimTypes.GivenName, GivenName));
            if (!string.IsNullOrEmpty(Email))
            {
                claims.AddClaim(new Claim(ClaimTypes.Email, Email));
            }
            if (!string.IsNullOrEmpty(PhoneNumber))
            {
                claims.AddClaim(new Claim(ClaimTypes.MobilePhone, PhoneNumber));
            }
            claims.AddClaim(new Claim(ClaimTypes.DateOfBirth, Birthdate.ToFileTimeUtc().ToString()));
            claims.AddClaim(new Claim("NHSNumber", NhsNumber));
            claims.AddClaim(new Claim("IdentityProofingLevel", IdentityProofingLevel.ToString()));
            if (!string.IsNullOrEmpty(PhoneNumberPdsMatched))
            {
                claims.AddClaim(new Claim("PhoneNumberPdsMatched", PhoneNumberPdsMatched));
            }
            if(!string.IsNullOrEmpty(GPRegistrationDetails?.GPODSCode))
            {
                claims.AddClaim(new Claim("GPODSCode", GPRegistrationDetails.GPODSCode));
            }
            return claims;
        }
    }

    public class GPRegistrationDetails
    {
        [JsonProperty("gp_ods_code")]
        public string GPODSCode { get; private set; }
    }
}
