using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.DomesticExemptions;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.IngestionPipelines
{
    public class DomesticExemptionFunctions
    {
        private readonly ILogger<DomesticExemptionFunctions> logger;
        private readonly IQueueService queueService;
        private readonly IDomesticExemptionsParsingService parsingService;
        private readonly DomesticExemptionSettings settings;

        public DomesticExemptionFunctions
            (ILogger<DomesticExemptionFunctions> logger,
                IDomesticExemptionsParsingService parsingService,
                DomesticExemptionSettings settings,
                IQueueService queueService)
        {
            this.logger = logger;
            this.parsingService = parsingService;
            this.settings = settings;
            this.queueService = queueService;
        }

        [FunctionName("SaveDomesticExemptionBulk")]
        public async Task<IActionResult> SaveDomesticExemptionBulk(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
        {
            try
            {
                logger.LogInformation($"{nameof(SaveDomesticExemptionBulk)} was invoked");

                var queueName = settings.SaveQueueName;
                logger.LogTraceAndDebug($"queueName: {queueName}");
                if (string.IsNullOrWhiteSpace(queueName))
                {
                    logger.LogError($"The specified service bus queue name was either empty or null. This may be caused by typo in appsettings.");
                    logger.LogInformation($"{nameof(SaveDomesticExemptionBulk)} has finished");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                var requestBodyString = await request.ReadAsStringAsync();

                var defaultReason = request.Query["DefaultReason"];

                if (string.IsNullOrEmpty(defaultReason))
                    throw new ArgumentNullException("Request must include default reason for exemption");

                var parsingServiceResult = await parsingService.ParseAndValidateDomesticExemptionsAsync(requestBodyString, defaultReason);

                var parsedDomesticExemptions = parsingServiceResult.parsedExemptions;

                if (parsedDomesticExemptions.Count == 0)
                    logger.LogWarning("No domestic exemptions ingested");

                logger.LogTraceAndDebug($"parsedDomesticExemptions: {parsedDomesticExemptions}");

                var successesCount = await SendMessagesToServiceBusAsync(queueName, parsedDomesticExemptions);

                var failedLines = string.Join("\n", parsingServiceResult.failedExemptions);

                logger.LogInformation($"{nameof(SaveDomesticExemptionBulk)} has finished");
                if (failedLines.Any())
                {
                    return new OkObjectResult($"Successfully sent {successesCount} users.\nFailed results:\n{failedLines}");
                }
                return new OkObjectResult($"Successfully sent {successesCount} users.");
            }
            catch (Exception e) when (e is ValidationException || e is ArgumentNullException || e is ArgumentException || e is NullReferenceException)
            {
                logger.LogError(e + e.Message);
                return new BadRequestObjectResult("There seems to be a problem: bad request");
            }
            catch (Exception e) when (e is CsvHelper.TypeConversion.TypeConverterException || e is CsvHelper.ReaderException)
            {
                logger.LogError(e + e.Message);
                return new OkObjectResult($"Successfully sent 0 users.\n" +
                                          $"Failed results:\n" +
                                          $"{await request.ReadAsStringAsync()}");
            }
            catch (Exception e)
            {
                logger.LogError(e + e.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [FunctionName("RemoveDomesticExemptionBulk")]
        public async Task<IActionResult> RemoveDomesticExemptionBulk(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
        {
            try
            {
                logger.LogInformation($"{nameof(RemoveDomesticExemptionBulk)} was invoked");

                var queueName = settings.RemoveQueueName;
                logger.LogTraceAndDebug($"queueName: {queueName}");
                if (string.IsNullOrWhiteSpace(queueName))
                {
                    logger.LogError($"The specified service bus queue name was either empty or null. This may be caused by typo in appsettings.");
                    logger.LogInformation($"{nameof(RemoveDomesticExemptionBulk)} has finished");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }

                var requestBodyString = await request.ReadAsStringAsync();

                var parsingServiceResult = await parsingService.ParseAndValidateDomesticExemptionsAsync(requestBodyString);

                var parsedDomesticExemptions = parsingServiceResult.parsedExemptions;

                if (parsedDomesticExemptions.Count == 0)
                    logger.LogWarning("No domestic exemptions to remove ingested");

                logger.LogTraceAndDebug($"parsedDomesticExemptions: {parsedDomesticExemptions}");

                var successesCount = await SendMessagesToServiceBusAsync(queueName, parsedDomesticExemptions);

                var failedLines = string.Join("\n", parsingServiceResult.failedExemptions);

                logger.LogInformation($"{nameof(RemoveDomesticExemptionBulk)} has finished.");
                if (failedLines.Any())
                {
                    return new OkObjectResult($"Successfully sent {successesCount} users.\nFailed results:\n{failedLines}");
                }
                return new OkObjectResult($"Successfully sent {successesCount} users.");
            }
            catch (Exception e) when (e is ValidationException || e is ArgumentNullException || e is ArgumentException || e is NullReferenceException)
            {
                logger.LogError(e + e.Message);
                return new BadRequestObjectResult("There seems to be a problem: bad request");
            }
            catch (Exception e) when (e is CsvHelper.TypeConversion.TypeConverterException || e is CsvHelper.ReaderException)
            {
                logger.LogError(e + e.Message);
                return new OkObjectResult($"Successfully sent 0 users.\n" +
                                          $"Failed results:\n" +
                                          $"{await request.ReadAsStringAsync()}");
            }
            catch (Exception e)
            {
                logger.LogError(e + e.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<int> SendMessagesToServiceBusAsync(string queueName, List<DomesticExemptionRecord> domesticExemptionsFromFile)
        {
            var successesCount = 0;
            var savedHashes = new List<string>();
            foreach (var domesticExemption in domesticExemptionsFromFile)
            {
                if (savedHashes.Contains(domesticExemption.NhsDobHash))
                {
                    logger.LogInformation($"Duplicate record found for hash: {domesticExemption.NhsDobHash}.");
                }
                else if (!await queueService.SendMessageAsync(queueName, domesticExemption))
                {
                    logger.LogError($"Failed to add the message with hash: {domesticExemption.NhsDobHash} to the service bus queue with the name: {queueName}. Check queue name, and if the queue has been created on the Azure Portal.");
                }
                else
                {
                    savedHashes.Add(domesticExemption.NhsDobHash);
                    successesCount++;
                    logger.LogTraceAndDebug($"Domestic exemption: Id is {domesticExemption?.Id}, UserHash is {domesticExemption?.NhsDobHash}, reason is {domesticExemption.Reason}");
                    logger.LogInformation("This domestic exemptions has been ingested");
                }
            }
            return successesCount;
        }
    }
}
