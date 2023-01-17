using CovidCertificate.Backend.Models.DataModels;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces.Certificates
{
    public interface IDomesticExemptionCertificateGenerator
    {
        public Task<Certificate> GenerateDomesticExemptionCertificateAsync(CovidPassportUser user, DomesticExemption exemption);
    }
}
