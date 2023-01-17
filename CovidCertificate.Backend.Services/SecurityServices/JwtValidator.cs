using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.JwtServices;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CovidCertificate.Backend.Services.SecurityServices
{
    public class JwtValidator : IJwtValidator
    {
        private readonly ILogger<JwtValidator> logger;
        private readonly IPublicKeyService publicKeyService;
        private readonly IJwtValidationParameterFetcher jwtValidationParameterFetcher;
        private readonly INhsLoginService nhsLoginService;

        private IList<string> AcceptedIdentityProofingLevel { get; set; }

        public JwtValidator(ILogger<JwtValidator> logger,
            IPublicKeyService publicKeyService, IJwtValidationParameterFetcher jwtValidationParameterFetcher, NhsLoginSettings nhsLoginSettings, INhsLoginService nhsLoginService)
        {
            this.logger = logger;
            this.publicKeyService = publicKeyService;
            this.jwtValidationParameterFetcher = jwtValidationParameterFetcher;
            AcceptedIdentityProofingLevel = GetAccepttedIdentityProofingLevels(nhsLoginSettings.AcceptedIdentityProofingLevel);
            this.nhsLoginService = nhsLoginService;
        }

        public async Task<ClaimsPrincipal> GetClaimsAsync(string token, string authSchema = "CovidCertificate")
        {
            logger.LogTraceAndDebug("GetClaimsAsync was invoked");
            try
            {
                NhsUserInfo userInfo = await nhsLoginService.GetUserInfoAsync(token);
                logger.LogTraceAndDebug("GetClaimsAsync has finished");

                if (!AcceptedIdentityProofingLevel.Contains(userInfo.IdentityProofingLevel))
                    return default;

                return new ClaimsPrincipal(userInfo.GetClaims());
            }
            catch(UnauthorizedException ue)
            {
                logger.LogError(ue, $"UnauthorizedException occured during token validation: {ue.Message}");
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Unexpected error during token validation: {e.Message}");
                throw;
            }
        }

        protected IList<string> GetAccepttedIdentityProofingLevels(string settings)
        {
            var acceptedIdentityProofingLevels = new List<string>();
            if (!string.IsNullOrEmpty(settings))
            {
                var levels = settings.Split(';');
                acceptedIdentityProofingLevels.AddRange(levels);
            }
            return acceptedIdentityProofingLevels;
        }

        /// <summary>
        /// Checks if a validation token is valid based off the current app settings
        /// </summary>
        /// <param name="token">The input JWT</param>
        /// <returns>If the token is valid</returns>
        public async Task<bool> IsValidTokenAsync(string token, string authSchema = "CovidCertificate")
        {
            var validationParameters = await jwtValidationParameterFetcher.GetValidationParametersAsync();
            if (validationParameters == null)
            {
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var claims = tokenHandler.ValidateToken(token, validationParameters, out var _);
                logger.LogInformation("User token validation was successful.");

                return true;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                logger.LogWarning("User token validation fail (invalid signature), refreshing public key.");

                return await RefreshPublicKeyAndRetryValidationAsync(token, tokenHandler);
            }
            catch (SecurityTokenException)
            {
                logger.LogWarning("Failed to validate token.");

                return false;
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Unexpected user token validation exception: " + e.Message);

                throw;
            }
        }

        private async Task<bool> RefreshPublicKeyAndRetryValidationAsync(string token, JwtSecurityTokenHandler tokenHandler)
        {
            try
            {
                var publicJwk = await publicKeyService.RefreshPublicKeysAsync();
                var validationParameters = await jwtValidationParameterFetcher.GetValidationParametersAsync();
                validationParameters.IssuerSigningKeys = publicJwk;
                tokenHandler.ValidateToken(token, validationParameters, out _);
                return true;
            }
            catch (SecurityTokenInvalidSignatureException e)
            {
                logger.LogWarning(e, "User token validation failed (invalid signature) with fresh key. Token invalid");

                return false;
            }
        }
    }
}
