using CovidCertificate.Backend.Models.ResponseDtos;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IUserPreferenceService
    {
        Task UpdateTermsAndConditionsAsync(string id);

        Task UpdateLanguageCodeAsync(string id, string lang);

        Task<UserPreferenceResponse> GetPreferencesAsync(string id);

    }
}
