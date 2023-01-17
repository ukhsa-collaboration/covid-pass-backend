using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using CovidCertificate.Backend.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Services
{
    public class CertificateInMemoryCache : ICertificateCache
    {
        private readonly IMemoryCacheService memoryCache;
        private readonly IConfiguration configuration;
        private Uri vaultUri;
        private CertificateClient certificateClient;

        public CertificateInMemoryCache(IConfiguration configuration,
            IMemoryCacheService memoryCache)
        {
            this.memoryCache = memoryCache;
            this.configuration = configuration;

            vaultUri = new Uri(configuration["CertificateVaultUri"]);
        }

        public async Task<X509Certificate2> GetCertificateByNameAsync(string certificateName, bool importPrivateKey = true)
        {
            var cacheKey = $"Certificate:{certificateName}";
            var inMemoryTimeToLiveInMinutes = configuration.GetValue<int>("CertificateCacheInMemoryTimeToLive");

            return await memoryCache.GetOrCreateCacheAsync(cacheKey,
                async () => await GetCertificateByNameFromKeyVaultAsync(certificateName, importPrivateKey),
                DateTimeOffset.UtcNow.AddMinutes(inMemoryTimeToLiveInMinutes));
        }

        public async Task<X509Certificate2> GetCertificateByTagAsync(string certificateTag, bool importPrivateKey = true)
        {
            var cacheKey = $"Certificate:{certificateTag}";
            var inMemoryTimeToLiveInMinutes = configuration.GetValue<int>("CertificateCacheInMemoryTimeToLive");

            return await memoryCache.GetOrCreateCacheAsync(cacheKey,
                async () => await GetCertificateByTagFromKeyVaultAsync(certificateTag, importPrivateKey),
                DateTimeOffset.UtcNow.AddMinutes(inMemoryTimeToLiveInMinutes));
        }

        private async Task<X509Certificate2> GetCertificateByNameFromKeyVaultAsync(string certificateName, bool importPrivateKey)
        {
            var credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                ExcludeSharedTokenCacheCredential = true
            });

            certificateClient = new CertificateClient(vaultUri, credential);
            KeyVaultCertificateWithPolicy certificate = await certificateClient.GetCertificateAsync(certificateName);

            var secretName = ParseSecretName(certificate.SecretId);
            var secretClient = new SecretClient(vaultUri, credential);
            var secret = await secretClient.GetSecretAsync(secretName);

            return ParseCertificate(secret, importPrivateKey);
        }

        private async Task<X509Certificate2> GetCertificateByTagFromKeyVaultAsync(string certificateTag, bool importPrivateKey)
        {
            var credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                ExcludeSharedTokenCacheCredential = true
            });

            certificateClient = new CertificateClient(vaultUri, credential);

            KeyVaultCertificateWithPolicy certificate = null;
            certificateTag = certificateTag == "DSC-GB" ? "DSC-ENG-WAL" : certificateTag;
            foreach (var certificateProperties in certificateClient.GetPropertiesOfCertificates())
            {
                if (certificateProperties.Tags.ContainsKey("TYPE") && certificateProperties.Tags["TYPE"] == certificateTag)
                {
                    certificate = await certificateClient.GetCertificateAsync(certificateProperties.Name);
                }
            }

            if(certificate == null)
            {
                throw new Exception($"Certificate could not be found for tag {certificateTag}");
            }

            var secretName = ParseSecretName(certificate.SecretId);
            var secretClient = new SecretClient(vaultUri, credential);
            var secret = await secretClient.GetSecretAsync(secretName);

            return ParseCertificate(secret, importPrivateKey);
        }

        private static string ParseSecretName(Uri secretId)
        {
            if (secretId.Segments.Length < 3)
            {
                throw new InvalidOperationException($@"The secret ""{secretId}"" does not contain a valid name.");
            }

            return secretId.Segments[2].TrimEnd('/');
        }

        private static X509Certificate2 ParseCertificate(KeyVaultSecret secret, bool importPrivateKey)
        {
            var publicCertificateBytes = GetBytesFromPemCertificate(secret, "CERTIFICATE");
            var privateKeyBytes = GetBytesFromPemCertificate(secret, "PRIVATE KEY");

            var publicCertificate = new X509Certificate2(publicCertificateBytes);
            if (importPrivateKey)
            {
                using var rsa = ECDsa.Create();
                rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
                publicCertificate = publicCertificate.CopyWithPrivateKey(rsa);
                publicCertificate = new X509Certificate2(publicCertificate.Export(X509ContentType.Pkcs12));
            }           

            return publicCertificate;
        }

        private static byte[] GetBytesFromPemCertificate(KeyVaultSecret secret, string type)
        {
            var pem = secret.Value;
            var bytes = Encoding.Default.GetBytes(pem);
            pem = Encoding.UTF8.GetString(bytes);
            var header = String.Format("-----BEGIN {0}-----", type);
            var footer = String.Format("-----END {0}-----", type);
            if(pem.IndexOf(header) < 0)
            {
                return Convert.FromBase64String(pem);
            }
            var start = pem.IndexOf(header) + header.Length;
            var end = pem.IndexOf(footer, start);
            var base64 = pem.Substring(start, (end - start)).Replace("\n", "");

            return Convert.FromBase64String(base64);
        }
    }
}
