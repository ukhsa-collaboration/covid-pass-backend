using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IMedicalExemptionService
    {
        Task<bool> IsUserMedicallyExemptAsync(CovidPassportUser user, string idToken);

        Task<IEnumerable<MedicalExemption>> GetMedicalExemptionsAsync(CovidPassportUser user, string idToken);

        Task<IEnumerable<DomesticExemption>> GetValidMedicalExemptionsAsDomesticExemptionsAsync(CovidPassportUser user, string idToken);
    }
}
