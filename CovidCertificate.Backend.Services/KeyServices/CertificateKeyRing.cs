using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CovidCertificate.Backend.Interfaces;
using System.Linq;

namespace CovidCertificate.Backend.Services.KeyServices
{
    public class CertificateKeyRing : IKeyRing
    {
        private string certificateName;
        private ICertificateCache certificateCache;
        private bool UsingDSCForSpecificRegions;

        public CertificateKeyRing(IConfiguration configuration, ICertificateCache certificateCache)
        {
            this.certificateName = configuration["QRSigningCertificateName"];
            this.certificateCache = certificateCache;
            UsingDSCForSpecificRegions = bool.TryParse(configuration["EnableDSCForSpecificRegions"], out var usingDSCForSpecificRegions) ? usingDSCForSpecificRegions : false;
        }

        public async Task<string> GetRandomKeyAsync()
        {
            // The key identifier (kid) is calculated when constructing the list of trusted public keys from DSC certificates and
            // consists of a truncated (first 8 bytes) SHA-256 fingerprint of the DSC encoded in DER (raw) format.
            var certificate = await GetCertificateByNameAsync(certificateName);
            var rawDataBytes = certificate.RawData;

            string kid;
            using (var sha256Hash = SHA256.Create())
            {
                var bytes = sha256Hash.ComputeHash(rawDataBytes);
                var truncatedHash = bytes.Take(8).ToArray();
                kid = Convert.ToBase64String(truncatedHash);
            }

            return kid;
        }

        public async Task<string> GetKeyByTagAsync(string certificateTag)
        {
            var certificate = UsingDSCForSpecificRegions ?
                await GetCertificateByTagAsync(certificateTag) :
                await GetCertificateByNameAsync(certificateName);
            var rawDataBytes = certificate.RawData;

            string kid;
            using (var sha256Hash = SHA256.Create())
            {
                var bytes = sha256Hash.ComputeHash(rawDataBytes);
                var truncatedHash = bytes.Take(8).ToArray();
                kid = Convert.ToBase64String(truncatedHash);
            }

            return kid;
        }

        public async Task<byte[]> SignDataAsync(string certificateTag, byte[] data)
        {
            certificateTag = certificateTag == "DSC-GB" ? "DSC-ENG-WAL" : certificateTag;
            var certificate = 
                string.IsNullOrWhiteSpace(certificateTag) || !UsingDSCForSpecificRegions ?
                    await GetCertificateByNameAsync(certificateName) : 
                    await GetCertificateByTagAsync(certificateTag);

            using var ECDsaPrivateKey = certificate.GetECDsaPrivateKey();

            return ECDsaPrivateKey.SignData(data, HashAlgorithmName.SHA256);
        }

        public async Task<bool> VerifyDataAsync(string keyId, byte[] data, byte[] signature)
        {
            using var certificate = await GetCertificateByNameAsync(certificateName);

            using var ECDsaPublicKey = certificate.GetECDsaPublicKey();

            return ECDsaPublicKey.VerifyData(
                data,
                signature,
                HashAlgorithmName.SHA256);
        }

        private async Task<X509Certificate2> GetCertificateByNameAsync(string certificateName)
        {
            return await certificateCache.GetCertificateByNameAsync(certificateName);
        }

        private async Task<X509Certificate2> GetCertificateByTagAsync(string certificateTag)
        {
            return await certificateCache.GetCertificateByTagAsync(certificateTag);
        }
    }
}
