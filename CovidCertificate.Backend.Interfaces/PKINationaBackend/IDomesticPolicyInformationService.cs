using System.Threading.Tasks;
using CovidCertificate.Backend.Models.PKINationalBackend.DomesticPolicy;

namespace CovidCertificate.Backend.Interfaces.PKINationaBackend
{
    public interface IDomesticPolicyInformationService
    {
        Task<DomesticPolicyInformation> GetDomesticPolicyInformationAsync();
    }
}
