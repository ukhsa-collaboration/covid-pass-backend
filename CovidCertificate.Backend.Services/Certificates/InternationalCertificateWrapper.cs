using System.Collections.Generic;
using System.Linq;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.ResponseDtos;

namespace CovidCertificate.Backend.Services.Certificates
{
    public class InternationalCertificateWrapper : IInternationalCertificateWrapper
    {
        public QRcodeResponse WrapRecovery(CertificatesContainer certificatesContainer)
        {
            var certificate = certificatesContainer.GetSingleCertificateOrNull();

            var recoveryTestData = certificate.GetLatestDiagnosticResultFromEligibleResultsOrDefault();
            return new QRcodeResponse(certificate.ValidityEndDate.ToString("yyyy-MM-dd"),
                                                  new List<IntlRecoveryResponse> { new IntlRecoveryResponse(recoveryTestData, certificate.QrCodeTokens.First()) },
                                                  certificate.UniqueCertificateIdentifier,
                                                  QRResponseType.Recovery,
                                                  certificate.eligibilityEndDate.ToString("yyyy-MM-dd"));
        }

        public QRcodeResponse WrapVaccines(CertificatesContainer certificatesContainer)
        {
            var certificate = certificatesContainer.GetSingleCertificateOrNull();

            var vaccineResponses = certificate.GetAllVaccinationsFromEligibleResults().ToList()
                .Zip(certificate.QrCodeTokens, (vaccine, qrCode) => new { vaccine, qrCode })
                .Select(item => new IntlVaccineResponse(item.vaccine, item.qrCode)).ToList();

            return new QRcodeResponse(certificate.ValidityEndDate.ToString("yyyy-MM-dd"),
                                                    vaccineResponses,
                                                    certificate.UniqueCertificateIdentifier,
                                                    QRResponseType.Vaccination,
                                                    certificate.eligibilityEndDate.ToString("yyyy-MM-dd"));
        }
    }
}
