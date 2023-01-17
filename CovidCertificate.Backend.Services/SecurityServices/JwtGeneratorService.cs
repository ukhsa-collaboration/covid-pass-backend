using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.JwtServices;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.ResponseDtos;
using CovidCertificate.Backend.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CovidCertificate.Backend.Services.SecurityServices
{
    public class JwtGeneratorService : IJwtGenerator
    {
        private readonly CovidJwtSettings covidJwtSettings;
        private readonly ILogger<JwtGeneratorService> logger;
        private readonly IKeyRing keyRing;

        public JwtGeneratorService(CovidJwtSettings covidJwtSettings,
                          ILogger<JwtGeneratorService> logger,
                          IKeyRing keyRing)
        {
            this.covidJwtSettings = covidJwtSettings;
            this.logger = logger;
            this.keyRing = keyRing;
        }

        public async Task<ValidateTwoFactorResponseDto> IssueAsync<T>(T user) where T : IJwtTokenData
        {
            logger.LogInformation("Issue was invoked");

            var keyId = await keyRing .GetRandomKeyAsync();
            var header = new JwtHeader
            {
                { JwtHeaderParameterNames.Alg, "ES256" },
                { JwtHeaderParameterNames.Kid, keyId },
                { JwtHeaderParameterNames.Typ, "JWT" }
            };

            var payload = new JwtPayload(user.GetClaims());
            payload.AddClaim(new Claim("iss", covidJwtSettings.Issuer));
            payload.AddClaim(new Claim("aud", covidJwtSettings.Audience));
            payload.AddClaim(new Claim("exp", DateTimeOffset.Now.AddDays(covidJwtSettings.ExpiryInDays).ToUnixTimeSeconds().ToString()));

            var token = new JwtSecurityToken(header, payload);
            var signature = await keyRing.SignDataAsync(keyId, Encoding.Unicode.GetBytes($"{token.EncodedHeader}.{token.EncodedPayload}")).ConfigureAwait(false);

            logger.LogInformation("Issue has finished");

            return new ValidateTwoFactorResponseDto
            {
                Token = $"{token.EncodedHeader}.{token.EncodedPayload}.{Base64UrlEncoder.Encode(signature)}"
            };
        }
    }
}
