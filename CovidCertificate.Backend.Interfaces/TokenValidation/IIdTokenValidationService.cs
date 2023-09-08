using System.Security.Claims;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Pocos;
using CovidCertificate.Backend.Models;

namespace CovidCertificate.Backend.Interfaces.TokenValidation
{
    public interface IIdTokenValidationService
    {
        Task<ValidationResponsePoco> ValidateIdTokenAsync(string idToken,
            ClaimsPrincipal tokenClaims,
            UserProperties userProperties);
    }
}
