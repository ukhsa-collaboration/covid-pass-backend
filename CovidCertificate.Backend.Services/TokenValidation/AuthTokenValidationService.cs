using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.DateTimeProvider;
using CovidCertificate.Backend.Interfaces.JwtServices;
using CovidCertificate.Backend.Interfaces.TokenValidation;
using CovidCertificate.Backend.Models;
using CovidCertificate.Backend.Models.Pocos;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CovidCertificate.Backend.Services.TokenValidation
{
    public class AuthTokenValidationService : IAuthTokenValidationService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<AuthTokenValidationService> logger;
        private readonly IJwtValidator jwtValidator;
        private readonly IDateTimeProviderService dateTimeProviderService;
        private readonly IOdsCodeService odsCodeService;
        private readonly IEndpointProofingLevelService endpointProofingLevelService;
        private readonly int minimumSecondsBeforeExpiry;

        public AuthTokenValidationService(IConfiguration configuration,
            ILogger<AuthTokenValidationService> logger,
            IJwtValidator jwtValidator,
            IDateTimeProviderService dateTimeProviderService,
            IOdsCodeService odsCodeService,
            IEndpointProofingLevelService endpointProofingLevelService)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.jwtValidator = jwtValidator;
            this.dateTimeProviderService = dateTimeProviderService;
            this.odsCodeService = odsCodeService;
            this.endpointProofingLevelService = endpointProofingLevelService;

            minimumSecondsBeforeExpiry = int.TryParse(configuration["MinimumSecondsBeforeTokenExpiry"], out minimumSecondsBeforeExpiry)
                ? this.minimumSecondsBeforeExpiry : 0;
        }

        /// <summary>
        /// Validates the Authorization token against our JWT validator
        /// </summary>
        /// <param name="formattedToken">The authentication token</param>
        /// <param name="userProperties">User's properties</param>
        /// <param name="callingEndpoint">Calling endpoint</param>
        /// <returns>A poco containing a http response or the claims if valid</returns>
        public async Task<ValidationResponsePoco> ValidateAuthTokenAsync(string formattedToken,
            UserProperties userProperties,
            string callingEndpoint)
        {
            logger.LogTraceAndDebug("ValidateAuthTokenAsync was invoked");

            if (string.IsNullOrEmpty(formattedToken))
            {
                return new ValidationResponsePoco("Does not contain token", new UserProperties());
            }

            try
            {
                var jwtToken = new JwtSecurityToken(formattedToken);

                if (TokenValidationUtils.IsTokenCloseToExpire(jwtToken, minimumSecondsBeforeExpiry, dateTimeProviderService.UtcNow))
                {
                    return new ValidationResponsePoco("Token (auth) expired or close to expiry", new UserProperties());
                }
                if (!TokenValidationUtils.CheckAudiencesMatch(jwtToken, configuration["Audience"]))
                    return new ValidationResponsePoco("Token Audience does not match", new UserProperties());
                var tokenSchema = jwtToken.Issuer;
                logger.LogTraceAndDebug($"tokenSchema is {tokenSchema}");

                if (!await jwtValidator.IsValidTokenAsync(formattedToken, tokenSchema))
                {
                    return new ValidationResponsePoco("The jwt token is not valid", new UserProperties());
                }

                var tokenClaims = await jwtValidator.GetClaimsAsync(formattedToken, tokenSchema);
                if (tokenClaims == null)
                {
                    return new ValidationResponsePoco("Invalid token claims", new UserProperties());
                }

                var dateOfBirth = TokenValidationUtils.ParseTokenClaimDobToDateTime(tokenClaims.FindFirst(ClaimTypes.DateOfBirth)?.Value);
                var odsCode = tokenClaims.FindFirst("GPODSCode")?.Value;
                var odsCodeCountry = await odsCodeService.GetCountryFromOdsCodeAsync(odsCode);
                userProperties.Country = odsCodeCountry;

                var minAppAccessAge = Int32.Parse(configuration["MinimumAppAccessAge"]);
                if (DateUtils.AgeIsBelowLimit(dateOfBirth, minAppAccessAge))
                {
                    return new ValidationResponsePoco($"The user is under {minAppAccessAge} years old.", userProperties);
                }

                logger.LogTraceAndDebug("ValidateAuthTokenAsync has finished");

                return await endpointProofingLevelService.ValidateProofingLevel(userProperties, callingEndpoint, dateOfBirth, tokenClaims);
            }
            catch (ArgumentException e)
            {
                logger.LogWarning(e, e.Message);
                return new ValidationResponsePoco("Invalid token", userProperties);
            }
            catch (SecurityTokenException e)
            {
                logger.LogWarning(e, e.Message);
                return new ValidationResponsePoco("Invalid token", userProperties);
            }
        }
    }
}
