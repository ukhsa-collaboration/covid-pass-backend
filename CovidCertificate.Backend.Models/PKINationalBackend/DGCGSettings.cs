using System.Collections.Generic;

namespace CovidCertificate.Backend.PKINationalBackend.Models
{
    public class DGCGSettings
    {
        public string BaseUrl { get; set; }
        public string TrustListEndpoint { get; set; }
        public string DGCGTLSCertThumbprint { get; set; }
        public int ApiRetryCount { get; set; }
        public int APIRetrySleepDurationMilliseconds { get; set; }
        public int APITimeoutMilliseconds { get; set; }
        public string ClientCertificateName { get; set; }
        public Dictionary<string, string> ValueSets { get; set; }
        public int TrustListCacheTimeSeconds { get; set; }
        public int ValueSetCacheTimeSeconds { get; set; }
        public string PolicyInformationBlobContainerName { get; set; }
        public string PolicyInformationBlobFileName { get; set; }
        public int PolicyCacheTimeSeconds { get; set; }
    }
}
