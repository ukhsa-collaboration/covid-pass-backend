using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.NhsApiIntegration.Interfaces
{
    public interface INhsTestResultsHistoryApiAccessTokenService
    {
        Task<string> GetAccessTokenAsync(NHSDAccessTokenConfigs accessTokenKey, string identityToken = default);
    }
}
