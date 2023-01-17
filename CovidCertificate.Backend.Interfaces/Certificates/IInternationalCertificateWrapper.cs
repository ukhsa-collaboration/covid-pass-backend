using CovidCertificate.Backend.Models.ResponseDtos;

namespace CovidCertificate.Backend.Interfaces.Certificates
{
    public interface IInternationalCertificateWrapper
    {
        QRcodeResponse WrapVaccines(CertificatesContainer certificatesContainer);

        QRcodeResponse WrapRecovery(CertificatesContainer certificatesContainer);
    }
}
