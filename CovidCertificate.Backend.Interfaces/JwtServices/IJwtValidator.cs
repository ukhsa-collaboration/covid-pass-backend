using System.Security.Claims;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces.JwtServices
{
    public interface IJwtValidator
    {
        Task<bool> IsValidTokenAsync(string token, string authSchema = "CovidCertificate");

        Task<ClaimsPrincipal> GetClaimsAsync(string token, string authSchema = "CovidCertificate");
    }
}
