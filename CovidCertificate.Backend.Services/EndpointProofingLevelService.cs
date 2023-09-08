using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Pocos;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services
{
    public class EndpointProofingLevelService : IEndpointProofingLevelService
    {
        private readonly ILogger<EndpointProofingLevelService> logger;
        private readonly IGracePeriodService gracePeriodService;
        private readonly IDomesticAccessService domesticAccessService;
        private readonly IList<string> allowedP5Endpoints;
        private readonly IList<string> allowedP5PlusEndpoints;
        private readonly IList<string> allowedNoDomesticEndPoints;
        private readonly IList<string> allowedP5U12Endpoints;
        private readonly IList<string> allowedP5PlusU12Endpoints;

        public EndpointProofingLevelService(IConfiguration configuration,
            IGracePeriodService gracePeriodService,
            IDomesticAccessService domesticAccessService,
            ILogger<EndpointProofingLevelService> logger)
        {
            this.gracePeriodService = gracePeriodService;
            this.domesticAccessService = domesticAccessService;
            this.logger = logger;

            allowedP5Endpoints = GetEndpointListFromString(configuration["AllowedP5EndPoints"]);
            allowedP5PlusEndpoints = GetEndpointListFromString(configuration["AllowedP5PlusEndPoints"]);
            allowedP5PlusU12Endpoints = GetEndpointListFromString(configuration["AllowedP5PlusU12Endpoints"]);
            allowedP5U12Endpoints = GetEndpointListFromString(configuration["AllowedP5U12Endpoints"]);
            allowedNoDomesticEndPoints = GetEndpointListFromString(configuration["AllowedNoDomesticEndPoints"]);
        }

        public async Task<ValidationResponsePoco> ValidateProofingLevel(UserProperties userProperties,
            string callingEndpoint,
            DateTime dateOfBirth,
            ClaimsPrincipal tokenClaims)
        {
            var domesticAccessLevel = await domesticAccessService.GetDomesticAccessLevelAsync(dateOfBirth);
            userProperties.DomesticAccessLevel = domesticAccessLevel;

            if (domesticAccessLevel == DomesticAccessLevel.NoAccess &&
                !IsValidNoDomesticAccessEndpoint(callingEndpoint))
            {
                return new ValidationResponsePoco(true, "The user is too young to access the domestic pass.");
            }

            var identityProofingLevel =
                await GetIdentityProofingLevelAsync(tokenClaims, userProperties);
            if (domesticAccessLevel == DomesticAccessLevel.U12)
            {
                return identityProofingLevel switch
                {
                    IdentityProofingLevel.P5 when !IsValidU12P5Endpoint(callingEndpoint) =>
                        new ValidationResponsePoco(true, "U12 P5 users cannot access this service"),
                    IdentityProofingLevel.P5Plus when !IsValidU12P5PlusEndpoint(callingEndpoint) =>
                        new ValidationResponsePoco(true, "U12 P5+ users cannot access this service"),
                    _ => new ValidationResponsePoco(tokenClaims, userProperties)
                };
            }

            return identityProofingLevel switch
            {
                IdentityProofingLevel.P5 when !IsValidP5Endpoint(callingEndpoint) =>
                    new ValidationResponsePoco(true, "P5 Users are forbidden to use this service."),
                IdentityProofingLevel.P5Plus when !IsValidP5PlusEndpoint(callingEndpoint) =>
                    new ValidationResponsePoco(true, "P5+ Users are forbidden to use this service."),

                _ => new ValidationResponsePoco(tokenClaims, userProperties)
            };
        }

        private async Task<IdentityProofingLevel> GetIdentityProofingLevelAsync(ClaimsPrincipal tokenClaims,
            UserProperties userProperties)
        {
            logger.LogTraceAndDebug($"{nameof(GetIdentityProofingLevelAsync)} was invoked");

            var identityProofingClaim = tokenClaims.FindFirst("IdentityProofingLevel");

            IdentityProofingLevel identityProofingLevel;
            var parsedIdentityProofingLevel = Enum.TryParse(identityProofingClaim.Value, true, out identityProofingLevel);

            if (!parsedIdentityProofingLevel)
            {
                throw new DataMisalignedException("IdentityProofingLevel not recognised. " + identityProofingClaim.Value);
            }

            if (identityProofingLevel == IdentityProofingLevel.P5)
            {

                identityProofingLevel = await IsUserP5PlusAsync(tokenClaims, userProperties) ?
                    IdentityProofingLevel.P5Plus : IdentityProofingLevel.P5;
            }

            logger.LogTraceAndDebug($"User identity proofing level: {identityProofingLevel}");
            userProperties.IdentityProofingLevel = identityProofingLevel;

            logger.LogTraceAndDebug($"{nameof(GetIdentityProofingLevelAsync)} has finished");
            return identityProofingLevel;
        }

        private bool IsValidNoDomesticAccessEndpoint(string path)
        {
            return allowedNoDomesticEndPoints.Contains(path);
        }

        private bool IsValidP5Endpoint(string path)
        {
            return allowedP5Endpoints.Contains(path);
        }
        private bool IsValidU12P5Endpoint(string path)
        {
            return allowedP5U12Endpoints.Contains(path);
        }

        private bool IsValidU12P5PlusEndpoint(string path)
        {
            return allowedP5PlusU12Endpoints.Contains(path);
        }

        private bool IsValidP5PlusEndpoint(string path)
        {
            return allowedP5PlusEndpoints.Contains(path);
        }

        private IList<string> GetEndpointListFromString(string allowedEndpoints)
        {
            var lstEndPoints = new List<string>();

            if (!string.IsNullOrEmpty(allowedEndpoints))
            {
                var endpoints = allowedEndpoints.Split(';');
                lstEndPoints.AddRange(endpoints);
            }
            return lstEndPoints;
        }

        private async Task<bool> IsUserP5PlusAsync(ClaimsPrincipal tokenClaims, UserProperties userProperties)
        {
            var phoneNumberMatchedWithPdsClaim = tokenClaims.FindFirst("PhoneNumberPdsMatched");
            var nhsNumber = tokenClaims.FindFirst("NHSNumber")?.Value;
            var dateOfBirth = TokenValidationUtils.ParseTokenClaimDobToDateTime(tokenClaims.FindFirst(ClaimTypes.DateOfBirth)?.Value);
            var nhsNumberDobHash = HashUtils.GenerateHash(nhsNumber, dateOfBirth);
            var phoneNumberMatchesPds = phoneNumberMatchedWithPdsClaim?.Value == "true";

            if (phoneNumberMatchesPds) // A P5 user is considered P5+ when phone number matches pds.
            {
                return true;
            }
            if (userProperties.DomesticAccessLevel == DomesticAccessLevel.U12) // U12 users do not have a grace period
            {
                userProperties.GracePeriod = null;
                return false;
            }
            var gracePeriod = await gracePeriodService.GetGracePeriodAsync(nhsNumberDobHash);

            userProperties.GracePeriod = gracePeriod;

            if (gracePeriod.IsActive) // If user is within his grace period, he gets upgraded from p5 -> p5+.
            {
                return true;
            }

            return false;
        }
    }
}
