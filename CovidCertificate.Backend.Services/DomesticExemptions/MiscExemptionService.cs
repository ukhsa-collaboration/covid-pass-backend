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
    public class MiscExemptionService : IMiscExemptionService
    {
        private readonly IDomesticExemptionRecordsService domesticExemptionRecordsService;
        private readonly ILogger<MiscExemptionService> logger;

        public MiscExemptionService(IDomesticExemptionRecordsService domesticExemptionRecordsService,
                                    ILogger<MiscExemptionService> logger)
        {
            this.domesticExemptionRecordsService = domesticExemptionRecordsService;
            this.logger = logger;
        }

        public async Task<bool> IsUserExemptAsync(string nhsNumber, DateTime dateOfBirth)
        {
            logger.LogTraceAndDebug($"{nameof(IsUserExemptAsync)} was invoked.");

            var exemptions = await GetExemptionsAsync(nhsNumber, dateOfBirth);

            logger.LogTraceAndDebug($"{nameof(IsUserExemptAsync)} has finished.");
            return exemptions.Any();
        }

        public async Task<IEnumerable<DomesticExemption>> GetExemptionsAsync(string nhsNumber, DateTime dateOfBirth)
        {
            logger.LogTraceAndDebug($"{nameof(GetExemptionsAsync)} was invoked.");

            var mongoExemptions = (await domesticExemptionRecordsService.GetDomesticExemptionsAsync(nhsNumber, dateOfBirth))?
                                     .Where(x => !x.Reason.ToLower().Contains("clinical trial") &&
                                                 !x.IsMedicalExemption);

            logger.LogTraceAndDebug($"{nameof(GetExemptionsAsync)} has finished.");
            return mongoExemptions != null ? mongoExemptions.Select(exemption => new DomesticExemption(exemption)) 
                                          : Enumerable.Empty<DomesticExemption>();
        }
    }
}
