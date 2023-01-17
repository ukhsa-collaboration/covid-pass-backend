using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.RequestDtos;
using CovidCertificate.Backend.Utils;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Models.Validators
{
    public class DomesticExemptionValidator
    {
        private readonly DateTime minDateOfBirth, maxDateOfBirth;
        private readonly ILogger logger;

        public DomesticExemptionValidator(DateTime minDateOfBirth,
            DateTime maxDateOfBirth, ILogger logger)
        {
            this.minDateOfBirth = minDateOfBirth;
            this.maxDateOfBirth = maxDateOfBirth;
            this.logger = logger;
        }

        public async Task<bool> IsValidDomesticExemptionAsync(DomesticExemptionDto record,
            string defaultExemptionReason)
        {
            if (record.Reason == defaultExemptionReason)
            {
                return false;
            }

            if (!DomesticExemptionUtils.ValidateDoB(record.DateOfBirth, minDateOfBirth, maxDateOfBirth))
            {
                logger.LogWarning($"Invalid date of birth: {record.DateOfBirth}.");

                return false;
            }

            var recordValidationResult = await record.ValidateObjectAsync();

            return recordValidationResult.IsValid;
        }
    }
}
