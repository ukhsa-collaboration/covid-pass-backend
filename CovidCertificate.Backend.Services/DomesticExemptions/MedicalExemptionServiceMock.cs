using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using Task = System.Threading.Tasks.Task;

namespace CovidCertificate.Backend.Services.DomesticExemptions
{
    public class MedicalExemptionServiceMock : IMedicalExemptionService
    {
        public Task<bool> IsUserMedicallyExemptAsync(CovidPassportUser user, string idToken)
        {
            return Task.FromResult(false);
        }

        public Task<IEnumerable<MedicalExemption>> GetMedicalExemptionsAsync(CovidPassportUser user, string idToken)
        {
            return Task.FromResult(Enumerable.Empty<MedicalExemption>());
        }

        public Task<IEnumerable<DomesticExemption>> GetValidMedicalExemptionsAsDomesticExemptionsAsync(CovidPassportUser user, string idToken)
        {
            return Task.FromResult(Enumerable.Empty<DomesticExemption>());
        }
    }
}
