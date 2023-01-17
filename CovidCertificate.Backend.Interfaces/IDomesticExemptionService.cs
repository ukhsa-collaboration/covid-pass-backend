using CovidCertificate.Backend.Models.DataModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IDomesticExemptionService
    {
        Task<bool> IsUserExemptAsync(CovidPassportUser user, string idToken);

        Task<IEnumerable<DomesticExemption>> GetAllExemptionsAsync(CovidPassportUser user, string idToken);
    }
}
