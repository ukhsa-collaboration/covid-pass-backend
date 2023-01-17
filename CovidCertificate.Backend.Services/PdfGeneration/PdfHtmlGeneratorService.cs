using System;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.RequestDtos;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.Settings;
using Microsoft.FeatureManagement;

namespace CovidCertificate.Backend.Services.PdfGeneration
{
    public class PdfHtmlGeneratorService : IPdfGeneratorService
    {
        private readonly IConfiguration configuration;
        private readonly IFeatureManager featureManager;
        private readonly HtmlGeneratorSettings generatorSettings;
        private readonly ILogger<PdfHtmlGeneratorService> logger;
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly TimeZoneInfo timeZoneInfo;

        public PdfHtmlGeneratorService(IConfiguration configuration,
            ILogger<PdfHtmlGeneratorService> logger,
            IFeatureManager featureManager,
            HtmlGeneratorSettings generatorSettings,
            IGetTimeZones timeZones)
        {
            this.configuration = configuration;
            this.featureManager = featureManager;
            this.logger = logger;
            this.generatorSettings = generatorSettings;
            timeZoneInfo = timeZones.GetTimeZoneInfo();
        }

        public async Task<Stream> GeneratePdfContentStreamAsync(PdfContent pdfContent)
        {
            logger.LogTraceAndDebug("GeneratePdfDocumentStream was invoked");

            var url = configuration["InternationalPdfGenerationEndpoint"];
            logger.LogTraceAndDebug($"url is {url}");

            var pdfContentSerialized = JsonConvert.SerializeObject(pdfContent);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(pdfContentSerialized)
            };

            var pdfResponse = await _httpClient.SendAsync(request);
            
            if (!pdfResponse.IsSuccessStatusCode)
            {
                logger.LogError($"There was an error during GeneratePdfDocumentStream with a status code of : {pdfResponse.StatusCode}");
                logger.LogTraceAndDebug("GeneratePdfDocumentStream has finished");
                return default;
            }

            logger.LogTraceAndDebug($"pdfResponse: StatusCode is {pdfResponse?.StatusCode}, Headers is {pdfResponse?.Headers}, ReasonPhrase is {pdfResponse?.ReasonPhrase}");
            logger.LogTraceAndDebug("GeneratePdfDocumentStream has finished");

            return await pdfResponse.Content.ReadAsStreamAsync();
        }

        public async Task<Stream> GeneratePdfDocumentStreamAsync(PdfContent pdfContent)
        {
            logger.LogTraceAndDebug("GeneratePdfDocumentStream was invoked");

            if (pdfContent == default || string.IsNullOrWhiteSpace(pdfContent.Body))
            {
                throw new ArgumentNullException("pdfContent");
            }

            var url = configuration["PdfGenerationEndpoint"];
            logger.LogTraceAndDebug($"url is {url}");

            var pdfContentSerialized = JsonConvert.SerializeObject(pdfContent);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(pdfContentSerialized)
            };

            var pdfResponse = await _httpClient.SendAsync(request);

            if (!pdfResponse.IsSuccessStatusCode)
            {
                logger.LogError($"There was an error during GeneratePdfDocumentStream with a status code of : {pdfResponse.StatusCode}");
                logger.LogTraceAndDebug("GeneratePdfDocumentStream has finished");
                return default;
            }

            logger.LogTraceAndDebug($"pdfResponse: StatusCode is {pdfResponse?.StatusCode}, Headers is {pdfResponse?.Headers}, ReasonPhrase is {pdfResponse?.ReasonPhrase}");
            logger.LogTraceAndDebug("GeneratePdfDocumentStream has finished");

            return await pdfResponse.Content.ReadAsStreamAsync();
        }

        public async Task<PdfContent> GetDirectDownloadPdfRequestObjectAsync(CovidPassportUser covidUser, Certificate certificate, string templateLanguage, IHtmlGeneratorService htmlGeneratorService)
        {
            var utc = TimeFormatConvert.ToUniversal(certificate.ValidityEndDate);

            var mandatoryCertsOn = await featureManager.IsEnabledAsync(FeatureFlags.MandatoryCerts);
            var voluntaryDomesticOn = await featureManager.IsEnabledAsync(FeatureFlags.VoluntaryDomestic);
            var mandatoryToggle = mandatoryCertsOn && voluntaryDomesticOn ? "-two-pass" : "-one-pass";
            var templateName = (certificate.CertificateType == CertificateType.DomesticVoluntary)
                ? $"{templateLanguage}{mandatoryToggle}-wales-only"
                : templateLanguage + mandatoryToggle;
            
            var dto = new AddPdfCertificateRequestDto
            {
                Name = covidUser.Name,
                DateOfBirth = covidUser.DateOfBirth,
                Email = covidUser.EmailAddress,
                TemplateName = templateName,
                Expiry = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZoneInfo),
                QrCodeToken = certificate.QrCodeTokens[0],
                CertificateType = certificate.CertificateType,
                UniqueCertificateIdentifier = certificate.UniqueCertificateIdentifier,
                LanguageCode = templateLanguage
            };

            var emailHtml = await htmlGeneratorService.GenerateHtmlAsync(dto.GetHtmlDto(), generatorSettings.TemplateFolder);
            
            if (emailHtml == default)
            {
                logger.LogCritical("Could not fetch email html");
                throw new Exception("HTML could not be generated");
            }

            return new PdfContent
            {
                Body = emailHtml,
                LanguageCode = templateLanguage
            };
        }
    }
}
