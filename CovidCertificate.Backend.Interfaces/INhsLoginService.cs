using CovidCertificate.Backend.Models.DataModels;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface INhsLoginService
    {
        Task<NhsLoginToken> GetAccessTokenAsync(string refreshToken);
        Task<NhsLoginToken> GetAccessTokenAsync(string authorisationCode, string redirectUri);
        Task<NhsUserInfo> GetUserInfoAsync(string accessToken);
    }
}