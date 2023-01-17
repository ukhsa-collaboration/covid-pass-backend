using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.RequestDtos;
using CovidCertificate.Backend.Utils.Extensions;
using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Interfaces.BlobService;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Services.PdfGeneration
{
    public class HtmlGeneratorService : IHtmlGeneratorService
    {
        private readonly IQrImageGenerator qrImageGenerator;
        private readonly ILogger<HtmlGeneratorService> logger;
        private readonly IBlobService blobService;
        private readonly IMemoryCacheService memoryCache;

        public HtmlGeneratorService(
            IQrImageGenerator qrImageGenerator,
            ILogger<HtmlGeneratorService> logger,
            IBlobService blobService,
            IMemoryCacheService memoryCache)
        {
            this.qrImageGenerator = qrImageGenerator ?? throw new ArgumentNullException(nameof(qrImageGenerator));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
            this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public async Task<string> GenerateHtmlAsync(GetHtmlRequestDto dto, string templateFolder)
        {
            logger.LogTraceAndDebug("GenerateHtml was invoked");

            var templateFetchingTask = GetHtmlTemplateAsync(templateFolder, dto.TemplateName);
            var qrCodeImage = qrImageGenerator.GenerateQrCodeString(dto.QrCodeToken);
            var template = await templateFetchingTask;
            logger.LogTraceAndDebug($"template is {template}");

            var data = new
            {
                qrCode = qrCodeImage,
                expiryDate = StringUtils.GetTranslatedAndFormattedDateTime(dto.Expiry, dto.LanguageCode),
                name = dto.Name,
                dateOfBirth = StringUtils.GetTranslatedAndFormattedDate(dto.DateOfBirth, dto.LanguageCode),
                certificateType = CertificateTypeToString(dto.CertificateType),
                uniqueCertificateIdentifier = dto.UniqueCertificateIdentifier
            };

            if (template == null)
                throw new NullReferenceException("Couldn't get template");

            var result = template(data);

            logger.LogTraceAndDebug("GenerateHtml has finished");
            return result;
        }

        public async Task<string> GetPassHtmlAsync(dynamic model, PassData type, string langCode = "en")
        {
            var templateFetchingTask = GetHtmlTemplateAsync("pass-views", langCode + "-" + type.ToString());
            var template = await templateFetchingTask;
            var result = template(model);
            return result;
        }
        
        public async Task<string> GetPassTermsAsync(string langCode="en")
        {
            var terms = await blobService.GetStringFromBlobAsync("pass-views", langCode + "-terms");
            return terms;
        }
        
        public async Task<Dictionary<string, string>> GetPassLabelsAsync(string langCode = "en")
        {
            var json = await blobService.GetStringFromBlobAsync("pass-labels", langCode+".json");
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            return values;
        }

        private void CheckIfVariablesAreNull(CovidPassportUser covidUser,
                                              string templateFolder)
        {
            if (covidUser == null)
            {
                throw new ArgumentNullException(nameof(covidUser));
            }

            if (templateFolder == null)
            {
                throw new ArgumentNullException(nameof(templateFolder));
            }
        }

        /// <summary>
        /// Fetches the email template from blob storage and then caches it in memory
        /// </summary>
        /// <param name="templateName"></param>
        /// <returns></returns>
        private async Task<HandlebarsTemplate<object, object>> GetHtmlTemplateAsync(string templateFolder, string templateName)
        {
            logger.LogTraceAndDebug("GetEmailTemplate was invoked");

            var cacheKey = $"email-template:{templateName}";

            var compiledTemplate = await memoryCache.GetOrCreateCacheAsync(cacheKey,
                async () => await CreateHtmlTemplateCacheAsync(templateFolder, templateName),
                DateTimeOffset.UtcNow.AddHours(1));

            logger.LogTraceAndDebug("GetEmailTemplate has finished");

            return compiledTemplate;
        }

        private async Task<HandlebarsTemplate<object, object>> CreateHtmlTemplateCacheAsync(string templateFolder, string templateName)
        {
            var returnText = await blobService.GetStringFromBlobAsync(templateFolder, templateName);
            var compiledTemplate = Handlebars.Compile(returnText);

            return compiledTemplate;
        }

        private string CertificateTypeToString(CertificateType type)
        {
            return type switch
            {
                CertificateType.Vaccination => "Vaccinated",
                CertificateType.Diagnostic => "Negative tested",
                CertificateType.Exemption => "Exempt",
                CertificateType.DomesticMandatory => "Domestic Mandatory",
                CertificateType.DomesticVoluntary => "Domestic Voluntary",
                CertificateType.None => "Basis of issuance",
                _ => "Basis of issuance",
            };
        }               
    }
}
