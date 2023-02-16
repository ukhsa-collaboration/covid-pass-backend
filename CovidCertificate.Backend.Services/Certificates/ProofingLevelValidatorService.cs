using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.NhsApiIntegration.Services;
using CovidCertificate.Backend.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Security;

namespace CovidCertificate.Backend.Services.Certificates
{
    public class ProofingLevelValidatorService : IProofingLevelValidatorService
    {
        private readonly bool supportP5TestResults;
        private readonly bool allowVoluntaryDomesticCert;
        private readonly bool allowMandatoryCert;
        private readonly NhsTestResultsHistoryApiSettings settings;
        private readonly ILogger<ProofingLevelValidatorService> logger;


        public ProofingLevelValidatorService(
            NhsTestResultsHistoryApiSettings settings,
            ILogger<ProofingLevelValidatorService> logger,
            IConfiguration configuration)
        {
            this.settings = settings;
            this.logger = logger;
            bool.TryParse(configuration["SupportP5TestResults"], out supportP5TestResults); 
            bool.TryParse(configuration["AllowVoluntaryDomesticCert"], out allowVoluntaryDomesticCert); 
            bool.TryParse(configuration["AllowMandatoryCerts"], out allowMandatoryCert);
        }

        public void VerifyProofingLevel(string identityToken)
        {
            var identityProofingLevel = GetProofingLevel(identityToken);

            if (settings.DisableP5 && IsP5ProofingLevel(identityProofingLevel))
            {
                logger.LogWarning("P5 proofing level detected but is disabled.");

                throw new SecurityException(
                    "Users with P5 proofing level are not allowed to access Test Results History API.");
            }

            if (settings.DisableP5Plus && IsP5PlusProofingLevel(identityProofingLevel))
            {
                logger.LogWarning("P5Plus proofing level detected but is disabled.");

                throw new SecurityException(
                    "Users with P5Plus proofing level are not allowed to access Test Results History API.");
            }

            if (settings.DisableP9 && IsP9ProofingLevel(identityProofingLevel))
            {
                logger.LogWarning("P9 proofing level detected but is disabled.");

                throw new SecurityException(
                    "Users with P9 proofing level are not allowed to access Test Results History API.");
            }

            if (!settings.AllowAllOtherThanP5AndP5PlusAndP9 && !IsP9OrP5orP5PlusProofingLevel(identityProofingLevel))
            {
                logger.LogWarning(
                    $"Proofing level '{identityProofingLevel}' detected, but profiles other than P5 P5Plus and P9 are not allowed.");

                throw new SecurityException(
                    $"Users with '{identityProofingLevel}' proofing level are not allowed to access Test Results History API.");
            }
        }
        private bool IsP5ProofingLevel(IdentityProofingLevel identityProofingLevel)
    => identityProofingLevel == IdentityProofingLevel.P5;

        private bool IsP5PlusProofingLevel(IdentityProofingLevel identityProofingLevel)
            => identityProofingLevel == IdentityProofingLevel.P5Plus;

        private bool IsP9ProofingLevel(IdentityProofingLevel identityProofingLevel)
            => identityProofingLevel == IdentityProofingLevel.P9;

        private bool IsP9OrP5orP5PlusProofingLevel(IdentityProofingLevel identityProofingLevel)
            => IsP5ProofingLevel(identityProofingLevel) || IsP9ProofingLevel(identityProofingLevel) || IsP5PlusProofingLevel(identityProofingLevel);

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
