using System.Collections.Generic;
using CovidCertificate.Backend.Models.RequestDtos;

namespace CovidCertificate.Backend.Interfaces.DomesticExemptions
{
    public interface ICsvToDomesticExemptionsParsingService
    {
        public (List<DomesticExemptionDto> parsedExemptions, List<string> failedRows)
            ParseCsvToDomesticExemptions(string csvFileContent);
    }
}
