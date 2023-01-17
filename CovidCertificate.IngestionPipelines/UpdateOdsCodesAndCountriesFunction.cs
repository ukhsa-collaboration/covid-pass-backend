using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.IngestionPipelines
{
    public class UpdateOdsCodesAndCountriesFunction
    {
        private const string UpdateOdsCodesAndCountriesFunctionName = "UpdateOdsCodesAndCountries";

        private readonly IUpdateOrganisationsService updateOrganisationsService;

        public UpdateOdsCodesAndCountriesFunction(IUpdateOrganisationsService updateOrganisationsService)
        {
            this.updateOrganisationsService = updateOrganisationsService;
        }

        [FunctionName(UpdateOdsCodesAndCountriesFunctionName)]
        public async Task Run([TimerTrigger("%UpdateOdsCodesAndCountriesFunctionCRON%")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"{UpdateOdsCodesAndCountriesFunctionName} Timer triggered function executed at: '{DateTime.UtcNow}'.");

            try
            {
                await updateOrganisationsService.UpdateOrganisationsFromOdsAsync();

                log.LogInformation($"{UpdateOdsCodesAndCountriesFunctionName} finished at: '{DateTime.UtcNow}'.");
            }
            catch (Exception e)
            {
                log.LogError($"Cannot update organizations in Cosmos collection. Ex message: '{e.Message}'.", e);

                throw;
            }
        }
    }
}
