using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.RequestDtos;
using CovidCertificate.Backend.Models.ResponseDtos;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces.Certificates
{
    public interface ICovidCertificateService
    {
        Task<bool> ExpiredCertificateExistsAsync(CovidPassportUser user, Certificate certificate);

        Task<CertificatesContainer> GetDomesticCertificateAsync(CovidPassportUser user, string idToken, MedicalResults medicalResults);

        Task<CertificatesContainer> GetInternationalCertificateAsync(CovidPassportUser user, string idToken, CertificateType? type, MedicalResults medicalResults);

        Task<CertificatesContainer> GetDomesticUnattendedCertificateAsync(CovidPassportUser user, MedicalResults medicalResults);

        Task<CertificatesContainer> GetInternationalUnattendedCertificateAsync(CovidPassportUser user, CertificateType? type, MedicalResults medicalResults);

        Task<bool> SendCertificateAsync(AddPdfCertificateRequestDto dto, string outputQueueName);
    }
}
