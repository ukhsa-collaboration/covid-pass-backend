using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace CovidCertificate.Backend.Interfaces.JwtServices
{
    public interface IJwtValidationParameterFetcher
    {
        Task<TokenValidationParameters> GetValidationParametersAsync();
    }
}
