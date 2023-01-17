using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.NhsApiIntegration.Interfaces
{
    public interface INhsdFhirApiService
    {
        Task<Bundle> GetAttendedVaccinesBundleAsync(string identityToken, string apiKey);

        Task<Bundle> GetDiagnosticTestResultsAsync(string identityToken, string apiKey);

        Task<Bundle> GetUnattendedVaccinesBundleAsync(CovidPassportUser user, string apiKey);

        Task<Bundle> GetUnattendedDiagnosticTestResultsAsync(CovidPassportUser user, string apiKey);
    }
}
