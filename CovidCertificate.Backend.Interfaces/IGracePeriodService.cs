using CovidCertificate.Backend.Models.ResponseDtos;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IGracePeriodService
    {
        Task<GracePeriodResponse> GetGracePeriodAsync(string nhsNumberDobHash);
        Task<GracePeriodResponse> ResetGracePeriodAsync(string nhsNumberDobHash);
    }
}
