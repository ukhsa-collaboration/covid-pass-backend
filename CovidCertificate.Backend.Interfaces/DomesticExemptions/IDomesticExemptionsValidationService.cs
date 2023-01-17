using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.RequestDtos;

namespace CovidCertificate.Backend.Interfaces.DomesticExemptions
{
    public interface IDomesticExemptionsValidationService
    {
        public Task<(List<DomesticExemptionDto> validDomesticExemptions, List<string> invalidDomesticExemptions)>
            ValidateExemptionsAsync(List<DomesticExemptionDto> records,
                string defaultExemptionReason);
    }
}
