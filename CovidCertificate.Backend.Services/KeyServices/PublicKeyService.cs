using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;

namespace CovidCertificate.Backend.Services.KeyServices
{
    public class PublicKeyService : IPublicKeyService
    {
        private readonly ILogger<PublicKeyService> logger;
        private readonly IHttpClientFactory httpClientFactory;
        private IList<JsonWebKey> publicKeys;
        private string jwksUrl;
        private static readonly AsyncLock mutex = new AsyncLock();

        public PublicKeyService(ILogger<PublicKeyService> logger, IHttpClientFactory httpClientFactory,
            NhsLoginSettings nhsLoginSettings)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            jwksUrl = nhsLoginSettings.PublicKeyUrl;
        }

        public async Task<IList<JsonWebKey>> GetPublicKeysAsync()
        {
            if (publicKeys != default)
            {
                return publicKeys;
            }

            using (await mutex.LockAsync())
            {
                if (publicKeys == default)
                {
                    publicKeys = await GetPublicKeysFromNhsAsync();
                }
            }

            return publicKeys;
        }

        public async Task<IList<JsonWebKey>> RefreshPublicKeysAsync()
        {
            using (await mutex.LockAsync())
            {
                publicKeys = await GetPublicKeysFromNhsAsync();
            }

            return publicKeys;
        }

        private async Task<IList<JsonWebKey>> GetPublicKeysFromNhsAsync()
        {
            logger.LogInformation($"Sending request to {jwksUrl} to get public key for id-token validation");
            using var response = await httpClientFactory.CreateClient().GetAsync(jwksUrl);
            var responseString = await response.Content.ReadAsStringAsync();

            var jwks = new JsonWebKeySet(responseString);

            return jwks.Keys;
        }
    }
}
