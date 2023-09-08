using System.Collections.Generic;
using System.Linq;

namespace CovidCertificate.Backend.DASigningService.Models
{
    public class RegionConfig
    {
        public string SubscriptionKeyIdentifier { get; set; }
        public string IssuingInstituion { get; set; }
        public string UVCICountryCode { get; set; }
        public string IssuingCountry { get; set; }
        public string SigningCertificateIdentifier { get; set; }
        public string DefaultResultCountry { get; set; }
        public List<string> AllowedThumbprints { get; set; }
}
}
