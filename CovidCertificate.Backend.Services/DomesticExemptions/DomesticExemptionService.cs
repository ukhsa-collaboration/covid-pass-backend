using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CovidCertificate.Backend.Utils.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace CovidCertificate.Backend.Services.DomesticExemptions
{
    public class DomesticExemptionService : IDomesticExemptionService
    {
        private readonly ILogger<DomesticExemptionService> logger;
        private readonly IClinicalTrialExemptionService clinicalTrialExemptionService;
        private readonly IMedicalExemptionService medicalExemptionService;
        private readonly IMiscExemptionService miscExemptionService;

        public DomesticExemptionService(ILogger<DomesticExemptionService> logger,
                                        IClinicalTrialExemptionService clinicalTrialExemptionService,
                                        IMedicalExemptionService medicalExemptionService,
                                        IMiscExemptionService miscExemptionService)
        {
            this.logger = logger;
            this.clinicalTrialExemptionService = clinicalTrialExemptionService;
            this.medicalExemptionService = medicalExemptionService;
            this.miscExemptionService = miscExemptionService;
        }        

        public async Task<bool> IsUserExemptAsync(CovidPassportUser user, string idToken)
        {
            logger.LogTraceAndDebug($"{nameof(IsUserExemptAsync)} was invoked.");

            var exemptions = await GetAllExemptionsAsync(user, idToken);

            logger.LogTraceAndDebug($"{nameof(IsUserExemptAsync)} has finished.");

            return exemptions.Any();
        }

        public async Task<IEnumerable<DomesticExemption>> GetAllExemptionsAsync(CovidPassportUser user, string idToken)
        {
            logger.LogTraceAndDebug($"{nameof(GetAllExemptionsAsync)} was invoked.");

            var exemptionsTasks = new List<Task<IEnumerable<DomesticExemption>>>
            {
                clinicalTrialExemptionService.GetClinicalTrialExemptionsAsync(user.NhsNumber, user.DateOfBirth),
                medicalExemptionService.GetValidMedicalExemptionsAsDomesticExemptionsAsync(user, idToken),
                miscExemptionService.GetExemptionsAsync(user.NhsNumber, user.DateOfBirth)
            };

            var results = await Task.WhenAll(exemptionsTasks);
            var exemptions = results.SelectMany(task => task);

            logger.LogTraceAndDebug($"{nameof(GetAllExemptionsAsync)} has finished.");

            return exemptions;
        }
    }
}
