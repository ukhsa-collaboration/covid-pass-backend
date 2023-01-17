using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Pocos;
using Microsoft.AspNetCore.Http;

namespace CovidCertificate.Backend.Interfaces.EndpointValidation
{
    public interface IEndpointAuthorizationService
    {
        Task<ValidationResponsePoco> AuthoriseEndpointAsync(HttpRequest httpRequest, string tokenSchema = "CovidCertificate");

        string GetIdToken(HttpRequest request, bool isIdTokenRequired = false);

        bool ResponseIsInvalid(ValidationResponsePoco validationResponse);
    }
}
