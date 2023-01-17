using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.DomesticExemptions;
using CovidCertificate.Backend.Models.RequestDtos;
using CovidCertificate.Backend.Models.Validators;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.DomesticExemptions
{
    public class DomesticExemptionsValidationService : IDomesticExemptionsValidationService
    {
        private readonly DomesticExemptionValidator domesticExemptionValidator;
        private readonly ILogger<DomesticExemptionsValidationService> logger;

        public DomesticExemptionsValidationService(IConfiguration configuration,
            ILogger<DomesticExemptionsValidationService> logger)
        {
            var maxDateOfBirth = DateTime.Parse(configuration["MaxDateOfBirth"], CultureInfo.InvariantCulture);
            var minDateOfBirth = DateTime.Parse(configuration["MinDateOfBirth"], CultureInfo.InvariantCulture);
            this.logger = logger;
            domesticExemptionValidator = new DomesticExemptionValidator(minDateOfBirth, maxDateOfBirth, logger);
        }

        public async Task<(List<DomesticExemptionDto> validDomesticExemptions, List<string> invalidDomesticExemptions)>
            ValidateExemptionsAsync(List<DomesticExemptionDto> records,
                string defaultExemptionReason)
        {
            logger.LogTraceAndDebug($"{nameof(ValidateExemptionsAsync)} was invoked");

            List<DomesticExemptionDto> validDomesticExemptions = new List<DomesticExemptionDto>();
            List<string> invalidDomesticExemptions = new List<string>();

            foreach (var record in records)
            {
                if (await domesticExemptionValidator.IsValidDomesticExemptionAsync(record, defaultExemptionReason))
                {
                    record.Reason = defaultExemptionReason;
                    validDomesticExemptions.Add(record);
                    continue;
                }

                invalidDomesticExemptions.Add(record.ToString());
            }

            logger.LogTraceAndDebug($"{nameof(ValidateExemptionsAsync)} finished");

            return (validDomesticExemptions, invalidDomesticExemptions);
        }
    }
}
