using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IIneligibilityService
    {
        Task<IneligiblityResult> GetUserIneligibilityAsync(IEnumerable<TestResultNhs> tests);
    }
}
