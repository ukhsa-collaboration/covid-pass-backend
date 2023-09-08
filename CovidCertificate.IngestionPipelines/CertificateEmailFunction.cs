using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.TwoFactor;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.RequestDtos;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Models.Validators;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Notify.Client;

namespace CovidCertificate.Backend.IngestionPipelines
{
    public class CertificateEmailFunction
    {
        private const int GovUkNotifyMaximumBytes = 2097152; // 2MB

        private readonly NotificationTemplates notificationTemplates;
        private readonly IEmailService emailService;
        private readonly ILogger<CertificateEmailFunction> logger;

        public CertificateEmailFunction(NotificationTemplates notificationTemplates,
            IEmailService emailService,
            ILogger<CertificateEmailFunction> logger)
        {
            this.notificationTemplates = notificationTemplates;
            this.emailService = emailService;
            this.logger = logger;
        }

        [FunctionName("CertificateEmailFunction")]
        public async Task Run([ServiceBusTrigger("%InputOutputPdfQueueName%", Connection = "ServiceBusConnectionString")] string myQueueItem)
        {
            try
            {
                logger.LogInformation("CertificateEmailFunction was invoked");
                var pdfRequest = JsonConvert.DeserializeObject<EmailPdfRequestDto>(myQueueItem);

                var validator = new EmailPdfRequestDtoValidator();
                var pdfValidated = await validator.ValidateAsync(pdfRequest);
                logger.LogTraceAndDebug($"pdfValidated: IsValid is {pdfValidated?.IsValid}");

                if (!pdfValidated.IsValid)
                {
                    logger.LogError("Invalid pdf request format: " + pdfValidated.Errors.ToString());
                    throw new ValidationException(pdfValidated.Errors.ToString());
                }

                if (pdfRequest == default)
                {
                    logger.LogInformation("pdfRequest == default");
                    logger.LogError("Invalid pdf request format: " + myQueueItem);
                    throw new Exception("Invalid pdf request format: " + myQueueItem);
                }

                var pdfFileBytes = pdfRequest.PdfData.Length;
                if (pdfFileBytes < GovUkNotifyMaximumBytes)
                {
                    logger.LogInformation(
                        "PDF file bytes are LESS than 2MB - email standard GOV.UK Notify template will be used");

                    var emailContent = new SendPdfCertificateRequestDto
                    {
                        EmailAddress = pdfRequest.Email,
                        Name = pdfRequest.Name,
                        DocumentContent = NotificationClient.PrepareUpload(
                            documentContents: pdfRequest.PdfData,
                            isCsv: false,
                            confirmEmailBeforeDownload: true,
                            retentionPeriod: "12 weeks"
                        )
                    };

                    var notificationTemplate = GetNotificationTemplateString(pdfRequest, false);

                    await emailService.SendEmailAsync<SendPdfCertificateRequestDto>(emailContent, notificationTemplate);
                    logger.LogInformation("CertificateEmailFunction has finished");

                    return;
                }

                logger.LogInformation(
                    "PDF file bytes are MORE than 2MB - email failure GOV.UK Notify template will be used");

                var notificationTemplateSizeExceeded = GetNotificationTemplateString(new EmailPdfRequestDto(), true);

                await emailService.SendPdfSizeFailureEmailAsync(pdfRequest.Email, notificationTemplateSizeExceeded);
                logger.LogInformation("CertificateEmailFunction has finished");
            }
            catch (ValidationException validationException)
            {
                logger.LogError($"Payload not valid: {validationException.Message}");
            }
            catch (Exception e)
            {
                logger.LogError($"Error when trying to email certificate, ex message: {e.Message}", e);
            }

        }

        private string GetNotificationTemplateString(EmailPdfRequestDto pdfRequest, bool pdfExceededNotifyLimit)
        {
            if (pdfExceededNotifyLimit)
                return notificationTemplates?.EmailPdfFailureSizeLimitExceeded?.EmailTemplateId.ToString();

            if (pdfRequest.CertificateScenario == CertificateScenario.International)
                return notificationTemplates?.InternationalPdf?.EmailTemplateId.ToString();

            return notificationTemplates?.DomesticPdf?.EmailTemplateId.ToString();
        }
    }
}
