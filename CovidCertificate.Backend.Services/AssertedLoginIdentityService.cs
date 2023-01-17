using CovidCertificate.Backend.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography;

namespace CovidCertificate.Backend.Services
{
    public class AssertedLoginIdentityService : IAssertedLoginIdentityService
    {
        private readonly IConfiguration configuration;
        private static readonly CryptoProviderFactory CryptoProviderFactory = new CryptoProviderFactory
        { CacheSignatureProviders = false };
        private readonly ILogger<AssertedLoginIdentityService> logger;

        public AssertedLoginIdentityService(
            IConfiguration configuration,
            ILogger<AssertedLoginIdentityService> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public string GenerateAssertedLoginIdentity(JwtSecurityToken jwtToken)
        {
            var jtiValue = jwtToken.Id;
            var configKey = configuration.GetValue<string>("NHSLoginKey");

            RSA rsaPrivateKey = RSA.Create();

            using (var reader = new StringReader(configKey))
            {
                var privateKey = (RsaPrivateCrtKeyParameters)new PemReader(reader)
                    .ReadObject();
                var rsaParams = DotNetUtilities.ToRSAParameters(privateKey);

                rsaPrivateKey.ImportParameters(rsaParams);
            }

            var jwtHeader = new JwtHeader(
                signingCredentials: new SigningCredentials(new RsaSecurityKey(rsaPrivateKey), SecurityAlgorithms.RsaSha512)
                {
                    CryptoProviderFactory = CryptoProviderFactory
                });

            var jwtPayload = new JwtPayload
            {
                {"code", jtiValue},
                {"iss", "healthrecords"},
                {"jti", Guid.NewGuid().ToString()},
                {"exp", new DateTimeOffset(DateTime.Now.AddSeconds(59)).ToUnixTimeSeconds()},
                {"iat", new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}
            };
            var jwt = new JwtSecurityToken(jwtHeader, jwtPayload);
            var assertedLoginIdentity = new JwtSecurityTokenHandler().WriteToken(jwt);

            return assertedLoginIdentity;
        }
    }
}
