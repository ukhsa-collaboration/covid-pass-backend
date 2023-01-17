using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.RequestDtos;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Models.Validators;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.IngestionPipelines
{
    public class FetchPdfEmailHtml
    {
        private readonly IHtmlGeneratorService htmlGeneratorService;
        private readonly IQueueService queueService;
        private readonly HtmlGeneratorSettings generatorSettings;
        private readonly ILogger<FetchPdfEmailHtml> logger;
        private readonly string outputQueueName;
        public FetchPdfEmailHtml(IHtmlGeneratorService htmlGeneratorService,
            IQueueService queueService,
            HtmlGeneratorSettings generatorSettings,
            IConfiguration configuration,
            ILogger<FetchPdfEmailHtml> logger)
        {
            this.htmlGeneratorService = htmlGeneratorService;
            this.queueService = queueService;
            this.generatorSettings = generatorSettings;
            this.logger = logger;
            outputQueueName = configuration["GeneratePDFCertificateQueue"];
        }

        [FunctionName("FetchPdfEmailHtml")]
        public async Task Run(
            [ServiceBusTrigger("%InputGeneratePdfEmailHtmlQueue%",
            Connection = "ServiceBusConnectionString")]string myQueueItem)
        {
            logger.LogInformation("FetchPdfEmailHtml was invoked");

            var pdfRequest = JsonConvert.DeserializeObject<AddPdfCertificateRequestDto>(myQueueItem);
            var validator = new AddPdfRequestDtoValidator();
            var pdfValidated = await validator.ValidateAsync(pdfRequest);
            logger.LogTraceAndDebug($"pdfValidated: IsValid is {pdfValidated?.IsValid}");

            if (!pdfValidated.IsValid)
            {
                logger.LogError("Invalid pdf request format: " + pdfValidated.Errors.ToString());
                throw new Exception(pdfValidated.Errors.ToString());
            }

            var htmlContent = await htmlGeneratorService.GenerateHtmlAsync(pdfRequest.GetHtmlDto(), generatorSettings.TemplateFolder);
            
            if (string.IsNullOrEmpty(htmlContent)) {
                logger.LogError("No Html could be generated");
                throw new Exception("No Html Content");
            }

            var emailMessage = new PdfGenerationRequestDomesticDto
            {
                Email = pdfRequest.Email,
                EmailContent = htmlContent,
                Name = pdfRequest.Name,
                LanguageCode = pdfRequest.TemplateName
            };

            await queueService.SendMessageAsync(outputQueueName, emailMessage);
            logger.LogInformation("FetchPdfEmailHtml has finished");
        }
    }
}
