using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Pocos;
using CovidCertificate.Backend.Models;

namespace CovidCertificate.Backend.Interfaces.TokenValidation
{
    public interface IAuthTokenValidationService
    {
        Task<ValidationResponsePoco> ValidateAuthTokenAsync(string formattedToken,
            UserProperties userProperties,
            string callingEndpoint);
    }
}
