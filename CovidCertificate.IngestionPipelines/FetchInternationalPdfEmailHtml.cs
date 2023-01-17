using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.RequestDtos;
using CovidCertificate.Backend.Models.Validators;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.IngestionPipelines
{
    public class FetchInternationalPdfEmailHtml
    {
        private readonly IQueueService queueService;
        private readonly ILogger<FetchPdfEmailHtml> logger;
        private readonly string outputQueueName;
        private readonly IPdfContentGenerator pdfContentGenerator;

        public FetchInternationalPdfEmailHtml(
            IQueueService queueService,
            IConfiguration configuration,
            ILogger<FetchPdfEmailHtml> logger,
            IPdfContentGenerator pdfContentGenerator)
        {
            this.queueService = queueService;
            this.logger = logger;
            this.pdfContentGenerator = pdfContentGenerator;
            outputQueueName = configuration["GeneratePDFCertificateQueueInternational"];
        }

        [FunctionName("FetchInternationalPdfEmailHtml")]
        public async Task Run(
                [ServiceBusTrigger("%InputGeneratePdfEmailHtmlQueue_INT%",
                Connection = "ServiceBusConnectionString")] string myQueueItem)
        {
            logger.LogInformation("FetchPdfEmailHtml was invoked");

            var serviceBusDto = JsonConvert.DeserializeObject<InternationalEmailServiceBusRequestDto>(myQueueItem);
            var validator = new InternationalEmailServiceBusRequestDtoValidator();
            var emailValidated = await validator.ValidateAsync(serviceBusDto);
            logger.LogTraceAndDebug($"pdfValidated: IsValid is {emailValidated?.IsValid}");

            if (!emailValidated.IsValid)
            {
                logger.LogError("Invalid email request format: " + emailValidated.Errors.ToString());
                throw new Exception(emailValidated.Errors.ToString());
            }

            if (serviceBusDto.RecoveryCertificate != null && serviceBusDto.RecoveryData != null)
                serviceBusDto.RecoveryCertificate.EligibilityResults = new List<IGenericResult>(serviceBusDto.RecoveryData);
            if (serviceBusDto.VaccinationCertificate != null && serviceBusDto.VaccinationData != null)
                serviceBusDto.VaccinationCertificate.EligibilityResults = new List<IGenericResult>(serviceBusDto.VaccinationData);

            var pdfContent = await pdfContentGenerator.GenerateInternationalAsync(
                covidPassportUser: serviceBusDto.CovidPassportUser,
                vaccinationCertificate: serviceBusDto.VaccinationCertificate,
                recoveryCertificate: serviceBusDto.RecoveryCertificate,
                languageCode: serviceBusDto.LanguageCode,
                type: serviceBusDto.Type,
                doseNumber: serviceBusDto.DoseNumber
            );

            if (pdfContent == default)
            {
                logger.LogError("No Html could be generated");
                throw new Exception("No Html Content");
            }

            var emailMessage = new PdfGenerationRequestInternationalDto
            {
                Email = serviceBusDto.EmailToSendTo,
                PdfContent = pdfContent,
                Name = serviceBusDto.CovidPassportUser.Name,
                LanguageCode = serviceBusDto.LanguageCode
            };

            await queueService.SendMessageAsync(outputQueueName, emailMessage);

            logger.LogInformation("FetchPdfEmailHtml has finished");
        }
    }
}
