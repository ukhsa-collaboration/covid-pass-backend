using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.JwtServices;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CovidCertificate.Backend.Services.SecurityServices
{
    public class JwtValidationParametersFetcher : IJwtValidationParameterFetcher
    {
        private readonly ILogger<JwtValidationParametersFetcher> logger;
        private readonly IPublicKeyService publicKeyService;
        private IList<JsonWebKey> publicJwks;

        public JwtValidationParametersFetcher(ILogger<JwtValidationParametersFetcher> logger, IPublicKeyService publicKeyService)
        {
            this.logger = logger;
            this.publicKeyService = publicKeyService;
        }

        /// <summary>
        /// Gets the validation parameters for the token validation
        /// </summary>
        /// <returns></returns>
        public async Task<TokenValidationParameters> GetValidationParametersAsync()
        {
            try
            {
                publicJwks = await publicKeyService.GetPublicKeysAsync();
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "User token validation fail, unable to get public NHS key ");

                return null;
            }

            return new TokenValidationParameters()
            {
                ValidateAudience = false,
                ValidateActor = false,
                ValidateIssuer = false,
                ValidateLifetime = true,
                IssuerSigningKeys = publicJwks,
                ClockSkew = TimeSpan.Zero
            };
        }
    }
}
