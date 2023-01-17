using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Utils;
using Microsoft.Extensions.Configuration;
using System;

namespace CovidCertificate.Backend.Services.Certificates
{
    public class ProofingLevelValidatorService : IProofingLevelValidatorService
    {
        private readonly bool supportP5TestResults;
        private readonly bool allowVoluntaryDomesticCert;
        private readonly bool allowMandatoryCert;

        public ProofingLevelValidatorService(IConfiguration configuration)
        {
            bool.TryParse(configuration["SupportP5TestResults"], out supportP5TestResults); 
            bool.TryParse(configuration["AllowVoluntaryDomesticCert"], out allowVoluntaryDomesticCert); 
            bool.TryParse(configuration["AllowMandatoryCerts"], out allowMandatoryCert);
        }

        public bool ValidateProofingLevel(string idToken)
        {
            var identityProofingLevel = GetProofingLevel(idToken);
            return ValidProofingLevel(identityProofingLevel) || ValidMandationCriteria();
        }

        private bool ValidMandationCriteria()
        {
            return allowMandatoryCert && !allowVoluntaryDomesticCert;
        }

        private bool ValidProofingLevel(IdentityProofingLevel identityProofingLevel)
        {
            return identityProofingLevel.Equals(IdentityProofingLevel.P9) || supportP5TestResults;
        }

        public IdentityProofingLevel GetProofingLevel(string idToken)
        {
            return Enum.Parse<IdentityProofingLevel>(JwtTokenUtils.GetClaim(idToken, "identity_proofing_level"));
        }
    }
}
