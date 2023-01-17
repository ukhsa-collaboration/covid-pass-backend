using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces.Certificates
{
    public interface ICovidResultsService
    {
        Task<MedicalResults> GetMedicalResultsAsync(CovidPassportUser user, string idToken, CertificateScenario scenario, string apiKey, CertificateType? type = null);
    }
}
