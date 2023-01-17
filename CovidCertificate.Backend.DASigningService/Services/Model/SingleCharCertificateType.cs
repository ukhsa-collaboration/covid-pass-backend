using CovidCertificate.Backend.Models.Enums;

namespace CovidCertificate.Backend.DASigningService.Services.Model
{
    public sealed class SingleCharCertificateType
    {
        public static readonly SingleCharCertificateType Recovery = new SingleCharCertificateType("r", CertificateType.Recovery);
        public static readonly SingleCharCertificateType Vaccination = new SingleCharCertificateType("v", CertificateType.Vaccination);
        public static readonly SingleCharCertificateType Domestic = new SingleCharCertificateType("d", CertificateType.DomesticMandatory);
        public static readonly SingleCharCertificateType TestResult = new SingleCharCertificateType("t", CertificateType.TestResult);

        public string SingleCharValue { get; private set; }

        public CertificateType CertificateType { get; private set; }

        private SingleCharCertificateType(string value, CertificateType certificateType)
        {
            SingleCharValue = value;
            CertificateType = certificateType;
        }
    }
}
