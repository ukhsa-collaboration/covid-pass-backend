using System;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.PKINationalBackend
{
    public class DocumentSignerCertificate
    {
        public string CertificateType { get; set; }
        public string Country { get; set; }
        public string Kid { get; set; }
        public string RawData { get; set; }
        public string Signature { get; set; }
        public string Thumbprint { get; set; }
        public string Timestamp { get; set; }

        [JsonConstructor]
        public DocumentSignerCertificate(string certificateType, string country, string kid, string rawData, string signature, string thumbprint, string timestamp)
        {
            CertificateType = certificateType;
            Country = country;
            Kid = kid;
            RawData = rawData;
            Signature = signature;
            Thumbprint = thumbprint;
            Timestamp = timestamp;
        }

        public TrustListSubjectPublicKeyInfoDto ConvertToSubjectPublicKeyInfoDto()
        {
            var x509Certificate = new X509Certificate2(Convert.FromBase64String(RawData));

            string subjectPublicKeyInfoString;
            var potentialRSAPublicKey =  x509Certificate.GetRSAPublicKey();
            if(potentialRSAPublicKey != null)
            {
                subjectPublicKeyInfoString = Convert.ToBase64String(potentialRSAPublicKey.ExportSubjectPublicKeyInfo());
            }
            else
            {
                var ecdPublicKey = x509Certificate.GetECDsaPublicKey();
                subjectPublicKeyInfoString = Convert.ToBase64String(ecdPublicKey.ExportSubjectPublicKeyInfo());
            }
            
            return new TrustListSubjectPublicKeyInfoDto(Kid, subjectPublicKeyInfoString, Country);
        }
    }
}
