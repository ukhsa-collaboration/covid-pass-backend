using CovidCertificate.Backend.Models.DataModels;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.DomesticExemptions;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;

namespace CovidCertificate.Backend.Services.DomesticExemptions
{
    public class DomesticExemptionsParsingService : IDomesticExemptionsParsingService
    {
        private readonly ILogger<DomesticExemptionsParsingService> logger;
        private readonly ICsvToDomesticExemptionsParsingService csvToDomesticExemptionsParsingService;
        private readonly IDomesticExemptionsValidationService domesticExemptionValidatingService;

        public DomesticExemptionsParsingService(ILogger<DomesticExemptionsParsingService> logger,
            ICsvToDomesticExemptionsParsingService csvToDomesticExemptionsParsingService,
            IDomesticExemptionsValidationService domesticExemptionValidatingService)
        {
            this.logger = logger;
            this.csvToDomesticExemptionsParsingService = csvToDomesticExemptionsParsingService;
            this.domesticExemptionValidatingService = domesticExemptionValidatingService;
        }

        public async Task<(List<DomesticExemptionRecord> parsedExemptions, List<string> failedExemptions)>
            ParseAndValidateDomesticExemptionsAsync(string request,
                string defaultReason = "")
        {
            logger.LogTraceAndDebug($"{nameof(ParseAndValidateDomesticExemptionsAsync)} was invoked");

            var fileContent = DomesticExemptionUtils.ReadExemptionFile(request);

            var (parsedExemptions, failedExemptions) =
                csvToDomesticExemptionsParsingService.ParseCsvToDomesticExemptions(fileContent);

            var (correctExemptions, invalidExemptions) =
                await domesticExemptionValidatingService.ValidateExemptionsAsync(parsedExemptions, defaultReason);

            failedExemptions.AddRange(invalidExemptions);

            var parsedSuccessfulExemptions = correctExemptions
                .Select(x => x.ToDomesticExemption())
                .ToList();

            logger.LogTraceAndDebug($"{nameof(ParseAndValidateDomesticExemptionsAsync)} has finished");
            return (parsedSuccessfulExemptions, failedExemptions);
        }
    }
}
