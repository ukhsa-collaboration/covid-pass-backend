using CovidCertificate.Backend.Models.DataModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IDiagnosticTestResultsService
    {
        Task<IEnumerable<TestResultNhs>> GetDiagnosticTestResultsAsync(string idToken, string apiKey);     
        
        Task<IEnumerable<TestResultNhs>> GetUnattendedDiagnosticTestResultsAsync(CovidPassportUser user, string apiKey);
    }
}
