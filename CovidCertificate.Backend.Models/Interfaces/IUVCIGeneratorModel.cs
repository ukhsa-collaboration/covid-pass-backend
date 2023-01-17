using System;
using CovidCertificate.Backend.Models.Enums;

namespace CovidCertificate.Backend.Models.Interfaces
{
    public interface IUVCIGeneratorModel
    {
        string UniqueCertificateId { get; set; } 
        CertificateType CertificateType { get; set; }
        CertificateScenario CertificateScenario { get; set; }
        string CertificateIssuer { get; set; }
        string UserHash { get; set; }
        DateTime DateOfCertificateCreation { get; set; }
        DateTime DateOfCertificateExpiration { get; set; }
    }
}
