using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.DomesticExemptions
{
    public class ClinicalTrialExemptionService : IClinicalTrialExemptionService
    {
        private readonly IDomesticExemptionRecordsService domesticExemptionRecordsService;
        private readonly ILogger<ClinicalTrialExemptionService> logger;

        public ClinicalTrialExemptionService(IDomesticExemptionRecordsService domesticExemptionRecordsService,
                                             ILogger<ClinicalTrialExemptionService> logger)
        {
            this.domesticExemptionRecordsService = domesticExemptionRecordsService;
            this.logger = logger;
        }

        public async Task<bool> IsUserClinicalTrialExemptAsync(string nhsNumber, DateTime dateOfBirth)
        {
            logger.LogTraceAndDebug($"{nameof(IsUserClinicalTrialExemptAsync)} was invoked.");

            var exemptions = await GetClinicalTrialExemptionsAsync(nhsNumber, dateOfBirth);

            logger.LogTraceAndDebug($"{nameof(IsUserClinicalTrialExemptAsync)} has finished.");
            return exemptions.Any();
        }

        public async Task<IEnumerable<DomesticExemption>> GetClinicalTrialExemptionsAsync(string nhsNumber, DateTime dateOfBirth)
        {
            logger.LogTraceAndDebug($"{nameof(GetClinicalTrialExemptionsAsync)} was invoked.");

            var mongoClinicalExemptions = (await domesticExemptionRecordsService.GetDomesticExemptionsAsync(nhsNumber, dateOfBirth))?
                                     .Where(x => x.Reason.ToLower().Contains("clinical trial"));

            logger.LogTraceAndDebug($"{nameof(GetClinicalTrialExemptionsAsync)} has finished.");
            return mongoClinicalExemptions != null ? mongoClinicalExemptions.Select(exemption => new DomesticExemption(exemption)) 
                                                   : Enumerable.Empty<DomesticExemption>();
        }
    }
}
