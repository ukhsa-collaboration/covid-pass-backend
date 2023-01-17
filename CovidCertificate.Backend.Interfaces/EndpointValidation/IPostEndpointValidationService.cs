using System.Threading.Tasks;
using FluentValidation;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CovidCertificate.Backend.Interfaces.EndpointValidation
{
    public interface IPostEndpointValidationService
    {
        Task<IActionResult> ValidatePostWithFhirPayloadAsync<T, VT>(HttpRequest req) where T : Base where VT : AbstractValidator<T>, new();

        Task<IActionResult> ValidatePostAsync<T, VT>(HttpRequest req) where VT : AbstractValidator<T>, new();

        Task<T> CastRequestToObjectAsync<T>(HttpRequest request);
    }
}
