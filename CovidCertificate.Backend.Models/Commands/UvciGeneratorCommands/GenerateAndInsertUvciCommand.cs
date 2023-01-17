using System;
using CovidCertificate.Backend.Models.Enums;

namespace CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands
{
    public class GenerateAndInsertUvciCommand
    {
        public string IssuingInstituion { get; }

        public string UVCICountryCode { get;  }
        public string UserHash { get; }
        public CertificateType CertificateType { get; }
        public CertificateScenario CertificateScenario { get; }
        public DateTime DateOfCertificateExpiration { get; }

        public GenerateAndInsertUvciCommand(
            string issuingInstituion,
            string uvciCountryCode,
            string userHash,
            CertificateType certificateType,
            CertificateScenario certificateScenario,
            DateTime dateOfCertificateExpiration)
        {
            IssuingInstituion = issuingInstituion;
            UVCICountryCode = uvciCountryCode;
            UserHash = userHash;
            CertificateType = certificateType;
            CertificateScenario = certificateScenario;
            DateOfCertificateExpiration = dateOfCertificateExpiration;
        }
    }
}
