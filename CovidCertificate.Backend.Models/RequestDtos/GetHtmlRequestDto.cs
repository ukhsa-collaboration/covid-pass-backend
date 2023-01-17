using CovidCertificate.Backend.Models.Enums;
using System;

namespace CovidCertificate.Backend.Models.RequestDtos
{
    public class GetHtmlRequestDto
    {
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime Expiry { get; set; }
        public DateTime EligibilityPeriod { get; set; }
        public string QrCodeToken { get; set; }
        public string TemplateName { get; set; }
        public CertificateType CertificateType { get; set; }
        public string UniqueCertificateIdentifier { get; set; }
        public string LanguageCode { get; set; }
    }
}
