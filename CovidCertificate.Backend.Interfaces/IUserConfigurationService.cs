using CovidCertificate.Backend.Models.DataModels;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IUserConfigurationService
    {
        Task<UserConfigurationResponse> GetUserConfigurationAsync(CovidPassportUser covidUser,string preferenceHash);
    }
}
