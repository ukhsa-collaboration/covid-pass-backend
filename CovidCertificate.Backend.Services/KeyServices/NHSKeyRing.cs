using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography;
using CovidCertificate.Backend.Interfaces;

namespace CovidCertificate.Backend.Services.KeyServices
{
    public class NhsKeyRing : INhsKeyRing
    {
        private readonly string key;
        private readonly ILogger<NhsKeyRing> logger;

        public NhsKeyRing(IConfiguration configuration, ILogger<NhsKeyRing> logger)
        {
            key = configuration.GetValue<string>("NHSLoginKey");
            this.logger = logger;
        }

        public string SignData(Dictionary<string, object> payload)
        {
            logger.LogTraceAndDebug("SignData was invoked");
            using (var reader = new StringReader(this.key))
            using (var rsa = new RSACryptoServiceProvider())
            {
                var key = (RsaPrivateCrtKeyParameters)new PemReader(reader)
                    .ReadObject();
                var rsaParams = DotNetUtilities.ToRSAParameters(key);
                rsa.ImportParameters(rsaParams);
                logger.LogTraceAndDebug("SignData has finished");

                var jwtHeader = new JwtHeader(
                    signingCredentials: new SigningCredentials(new RsaSecurityKey(rsa),
                        SecurityAlgorithms.RsaSha512)
                    {
                        CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
                    });
                
                var jwtPayload = new JwtPayload
                {
                    {"sub", payload["sub"]},
                    {"aud", payload["aud"]},
                    {"iss", payload["iss"]},
                    {"exp",  payload["exp"]},
                    {"jti", payload["jti"]},
                };

                var jwt = new JwtSecurityToken(jwtHeader, jwtPayload);
                var token = new JwtSecurityTokenHandler().WriteToken(jwt);

                return token;
            }
        }
    }
}
