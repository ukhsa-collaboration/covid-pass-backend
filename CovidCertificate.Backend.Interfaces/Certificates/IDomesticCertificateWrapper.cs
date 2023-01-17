using System.Threading.Tasks;
using CovidCertificate.Backend.Models.ResponseDtos;

namespace CovidCertificate.Backend.Interfaces.Certificates
{
    public interface IDomesticCertificateWrapper
    {
        Task<DomesticCertificateResponse> WrapAsync(CertificatesContainer certiifcate, bool expiredCertificateExists);
    }
}
