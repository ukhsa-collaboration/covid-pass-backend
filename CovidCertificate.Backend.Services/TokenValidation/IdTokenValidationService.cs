using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.DateTimeProvider;
using CovidCertificate.Backend.Interfaces.JwtServices;
using CovidCertificate.Backend.Interfaces.TokenValidation;
using CovidCertificate.Backend.Models.Pocos;
using CovidCertificate.Backend.Models;
using CovidCertificate.Backend.Utils.Extensions;
using CovidCertificate.Backend.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CovidCertificate.Backend.Services.TokenValidation
{
    public class IdTokenValidationService : IIdTokenValidationService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<IdTokenValidationService> logger;
        private readonly IJwtValidator jwtValidator;
        private readonly IDateTimeProviderService dateTimeProviderService;
        private readonly int minimumSecondsBeforeExpiry;

        public IdTokenValidationService(IConfiguration configuration,
            ILogger<IdTokenValidationService> logger,
            IJwtValidator jwtValidator,
            IDateTimeProviderService dateTimeProviderService)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.jwtValidator = jwtValidator;
            this.dateTimeProviderService = dateTimeProviderService;

            minimumSecondsBeforeExpiry = int.TryParse(configuration["MinimumSecondsBeforeTokenExpiry"], out minimumSecondsBeforeExpiry)
                ? this.minimumSecondsBeforeExpiry : 0;
        }

        /// <summary>
        /// Validates the Id token
        /// </summary>
        /// <param name="idToken">The Id token</param>
        /// <param name="tokenClaims">The token schema to use</param>
        /// <param name="userProperties">User's properties</param>
        /// <returns>A poco containing a http response or the claims if valid</returns>
        public async Task<ValidationResponsePoco> ValidateIdTokenAsync(string idToken,
            ClaimsPrincipal tokenClaims,
            UserProperties userProperties)
        {
            logger.LogTraceAndDebug("ValidateIdToken was invoked");

            if (string.IsNullOrEmpty(idToken))
                return new ValidationResponsePoco("Does not contain token (id-token)", new UserProperties());

            try
            {
                var jwtToken = new JwtSecurityToken(idToken);
                if (TokenValidationUtils.IsTokenCloseToExpire(jwtToken, minimumSecondsBeforeExpiry, dateTimeProviderService.UtcNow))
                    return new ValidationResponsePoco("Token (id-token) expired or close to expiry", new UserProperties());
                if (!TokenValidationUtils.CheckAudiencesMatch(jwtToken, configuration["Audience"]))
                    return new ValidationResponsePoco("Token Audience does not match", new UserProperties());
                var tokenSchema = "id-token";

                var isJwtTokenValid = await jwtValidator.IsValidTokenAsync(idToken, tokenSchema);

                if (!isJwtTokenValid)
                    return new ValidationResponsePoco("The jwt token (id-token) is not valid", new UserProperties());

                return new ValidationResponsePoco(tokenClaims, userProperties);
            }
            catch (ArgumentException e)
            {
                logger.LogWarning(e, e.Message);
                return new ValidationResponsePoco("Invalid token (id-token)", new UserProperties());
            }
            catch (SecurityTokenException e)
            {
                logger.LogWarning(e, e.Message);
                return new ValidationResponsePoco("Invalid token (id-token)", new UserProperties());
            }
        }
    }
}
