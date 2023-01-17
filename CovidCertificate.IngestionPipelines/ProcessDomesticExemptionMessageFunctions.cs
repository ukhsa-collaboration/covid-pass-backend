using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Utils.Extensions;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.IngestionPipelines
{
    public class ProcessDomesticExemptionMessageFunctions
    {
        private ILogger<ProcessDomesticExemptionMessageFunctions> logger;
        private readonly IDomesticExemptionRecordsService domesticExemptionRecordsService;

        public ProcessDomesticExemptionMessageFunctions(ILogger<ProcessDomesticExemptionMessageFunctions> logger, IDomesticExemptionRecordsService domesticExemptionRecordsService)
        {
            this.logger = logger;
            this.domesticExemptionRecordsService = domesticExemptionRecordsService;
        }

        [FunctionName("DomesticExemptionInsertionMessageFunction")]
        public async Task RunDomesticExemptionInsert(
             [ServiceBusTrigger("%DomesticExemptionSaveISBQN%", Connection = "ServiceBusConnectionString")] string myQueueItem)
        {
            try
            {
                logger.LogInformation("DomesticExemptionInsertionMessageFunction was invoked");

                var domesticExemption = JsonConvert.DeserializeObject<DomesticExemptionRecord>(myQueueItem);

                logger.LogTraceAndDebug($"domesticExemption: Id is {domesticExemption?.Id}, nhsDobHash is {domesticExemption?.NhsDobHash}, reason is {domesticExemption.Reason}");
                await domesticExemptionRecordsService.SaveDomesticExemptionAsync(domesticExemption, isMedicalExemption:false);
                logger.LogInformation("DomesticExemptionInsertionMessageFunction has finished");
            }
            catch (JsonSerializationException e)
            {
                logger.LogError(e, $"Failed to deserialize queue item: {myQueueItem}");
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                throw;
            }
        }

        [FunctionName("DomesticExemptionRemoveMessageFunction")]
        public async Task RunDomesticExemptionRemove(
             [ServiceBusTrigger("%DomesticExemptionRemoveISBQN%", Connection = "ServiceBusConnectionString")] string myQueueItem)
        {
            try
            {
                logger.LogInformation("DomesticExemptionRemoveMessageFunction was invoked");

                var domesticExemption = JsonConvert.DeserializeObject<DomesticExemptionRecord>(myQueueItem);

                if (domesticExemption == null)
                {
                    throw new ArgumentException(
                        "Couldn't populate domestic exemption, please check format supplied: " + myQueueItem);
                }

                logger.LogTraceAndDebug($"domesticExemption: Id is {domesticExemption?.Id}, nhsDobHash is {domesticExemption?.NhsDobHash}, reason is {domesticExemption.Reason}");

                await domesticExemptionRecordsService.RemoveDomesticExemptionsForUserAsync(domesticExemption.NhsDobHash, isMedicalExemption:false);

                logger.LogInformation("DomesticExemptionRemoveMessageFunction has finished");
            }
            catch (JsonSerializationException e)
            {
                logger.LogError(e, $"Failed to deserialize queue item: {myQueueItem}");
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
