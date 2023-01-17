using CovidCertificate.Backend.Models.DataModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces.DomesticExemptions
{
    public interface IDomesticExemptionsParsingService
    {
        Task<(List<DomesticExemptionRecord> parsedExemptions, List<string> failedExemptions)> ParseAndValidateDomesticExemptionsAsync(
            string request, string reason = default);
    }
}
