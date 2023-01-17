using CovidCertificate.Backend.Models.DataModels;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IGracePeriodCache
    {
        Task<GracePeriod> GetGracePeriodAsync(string nhsNumberDobHash);
        Task<bool> AddToCacheAsync(GracePeriod gracePeriod, string nhsNumberDobHash);
    }
}
