using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.BlobService;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.DataModels.PdfGeneration;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.RequestDtos;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;
using HandlebarsDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.PdfGeneration
{
    public class PdfContentGenerator : IPdfContentGenerator
    {
        private ILogger<PdfContentGenerator> logger;
        private readonly IQrImageGenerator qrImageGenerator;
        private readonly IConfiguration configuration;
        private readonly IMemoryCacheService memoryCache;
        private readonly TimeZoneInfo timeZoneInfo;
        private readonly IBlobService blobService;

        public PdfContentGenerator(ILogger<PdfContentGenerator> logger,
            IQrImageGenerator qrImageGenerator,
            IConfiguration configuration,
            IGetTimeZones timeZones,
            IBlobService blobService,
            IMemoryCacheService memoryCache)
        {
            this.logger = logger;
            this.qrImageGenerator = qrImageGenerator;
            this.configuration = configuration;
            this.timeZoneInfo = timeZones.GetTimeZoneInfo();
            this.blobService = blobService;
            this.memoryCache = memoryCache;
        }

        public async Task<PdfContent> GenerateInternationalAsync(CovidPassportUser covidPassportUser, Certificate vaccinationCertificate, Certificate recoveryCertificate, string languageCode, PDFType type, int doseNumber)
        {
            var pdfPagesBody = await GeneratePdfBodyContentAsync(
                covidPassportUser: covidPassportUser,
                vaccinationCertificate: vaccinationCertificate,
                recoveryCertificate: recoveryCertificate,
                languageCode: languageCode, 
                type: type,
                doseNumber: doseNumber
            );

            return new PdfContent()
            {
                Body = pdfPagesBody,
                LanguageCode = languageCode
            };
        }

        private async Task<string> GeneratePdfBodyContentAsync(CovidPassportUser covidPassportUser, Certificate vaccinationCertificate, Certificate recoveryCertificate, string languageCode, PDFType type, int doseNumber)
        {
            logger.LogTraceAndDebug("GenerateHtml was invoked");

            int numberOfVaccinationPages = vaccinationCertificate != null && vaccinationCertificate.EligibilityResults != null ? vaccinationCertificate.EligibilityResults.Count() : 0;
            int numberOfRecoveryPages = recoveryCertificate != null && recoveryCertificate.EligibilityResults != null ? recoveryCertificate.EligibilityResults.Count() : 0;
            int totalNumberOfPages = numberOfVaccinationPages + numberOfRecoveryPages;

            var includeVaccinePages = type == PDFType.VaccineAndRecovery || type == PDFType.Vaccine;
            var includeRecoveryPages = type == PDFType.VaccineAndRecovery || type == PDFType.Recovery;
            
            (IEnumerable<string> vaccinePages, int nextPageNumber) = includeVaccinePages ? await GenerateVaccinePagesAsync(
                user: covidPassportUser, 
                certificate: vaccinationCertificate, 
                languageCode: languageCode,
                doseNumber: doseNumber, 
                totalPages: totalNumberOfPages
            ) : new Tuple<IEnumerable<string>, int>(new List<string>(), 1);
            
            var recoveryPages = includeRecoveryPages ? await GenerateRecoveryPagesAsync(
                covidPassportUser: covidPassportUser,
                certificate: recoveryCertificate,
                languageCode: languageCode,
                nextPageNumber: nextPageNumber,
                totalPages: totalNumberOfPages
            ) : Enumerable.Empty<string>();

            string resultString = await CreateHtmlStringAsync(
                vaccinePages: vaccinePages,
                recoveryPages: recoveryPages,
                languageCode: languageCode
            );

            return resultString;
        }

        private async Task<string> CreateHtmlStringAsync(IEnumerable<string> vaccinePages, IEnumerable<string> recoveryPages, string languageCode)
        {
            string htmlStyling = await GetHtmlStylingAsync(languageCode);

            string htmlDocumentStart = "<!DOCTYPE html><html><body><replaceHeader><replaceFooter>";
            string htmlDocumentEnd = "</body></html>";
            string htmlPagesContainerStart = "<table><tbody><tr><td>";
            string htmlPagesContainerEnd = "</td></tr></tbody></table>";

            StringBuilder vaccineAndRecoveryPages = new StringBuilder();

            foreach (string vaccinePage in vaccinePages.ToList())
            {
                string completePage = $"<div class=\"page\">{vaccinePage}</div>";
                vaccineAndRecoveryPages.Append(completePage);
            }

            foreach (string recoveryPage in recoveryPages.ToList())
            {
                string completePage = $"<div class=\"page\">{recoveryPage}</div>";
                vaccineAndRecoveryPages.Append(completePage);
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder
                .Append(htmlDocumentStart)
                .Append(htmlPagesContainerStart)
                .Append(vaccineAndRecoveryPages.ToString())
                .Append(htmlPagesContainerEnd)
                .Append(htmlDocumentEnd)
                .Append(htmlStyling);

            return stringBuilder.ToString();
        }

        private async Task<string> GetHtmlStylingAsync(string languageCode)
        {
            var stylingTemplateName = $"{languageCode}-international-styling.hbs";
            return await GetHtmlAsync(stylingTemplateName);
        }

        private async Task<Tuple<IEnumerable<string>, int>> GenerateVaccinePagesAsync(CovidPassportUser user, Certificate certificate,
            string languageCode, int doseNumber, int totalPages)
        {
            if (certificate == default)
            {
                return new Tuple<IEnumerable<string>, int>(new List<string>(), 1);
            }

            var vaccineTemplateName = $"{languageCode}-international-vaccine.hbs";

            var vaccines = certificate.GetAllVaccinationsFromEligibleResults()
                .Select((vaccine, index) => new { vaccine, index })
                .OrderBy(x => x.vaccine.DateTimeOfTest)
                .ToArray();

            // Sorting of QR codes should match sorting of vaccines
            var qrTokens = certificate.QrCodeTokens
                .Select((qrToken, index) => new { qrToken, index })
                .OrderBy(x => vaccines[x.index].index)
                .Select(x => x.qrToken);

            var pages = new List<string>();
            var vaccineHeaderOverflow = vaccines.Any(x => x.vaccine.IsBooster && x.vaccine.DisplayName.Length > 30);
            int currentPageNumber = 1;

            if (doseNumber == -1)
            {
                for (int i = vaccines.Count() - 1; i >= 0; i--)
                {
                    var vaccine = vaccines.ElementAt(i).vaccine;
                    var qrCode = qrTokens.ElementAt(i);

                    var vaccineQrCodeTuple = (vaccine, qrCode);

                    var validityEndDate = certificate.ValidityEndDate;
                    var pdfVaccineDto = GenerateHandlebarsVaccinesDto(user, languageCode, vaccineQrCodeTuple, validityEndDate, vaccineHeaderOverflow, currentPageNumber.ToString(), totalPages.ToString());
                    var vaccineHtml = await GetHtmlAndCompileWithDataAsync(vaccineTemplateName, pdfVaccineDto);
                    pages.Add(vaccineHtml);

                    currentPageNumber = currentPageNumber + 1;
                }
            }
            else
            {
                var vaccine = vaccines.ElementAt(doseNumber - 1).vaccine;
                var qrCode = qrTokens.ElementAt(doseNumber - 1);

                var vaccineQrCodeTuple = (vaccine, qrCode);

                var validityEndDate = certificate.ValidityEndDate;
                var pdfVaccineDto = GenerateHandlebarsVaccinesDto(user, languageCode, vaccineQrCodeTuple, validityEndDate, vaccineHeaderOverflow, currentPageNumber.ToString(), totalPages.ToString());
                var vaccineHtml = await GetHtmlAndCompileWithDataAsync(vaccineTemplateName, pdfVaccineDto);
                pages.Add(vaccineHtml);
            }

            return new Tuple<IEnumerable<string>, int>(pages, currentPageNumber);
        }

        private async Task<IEnumerable<string>> GenerateRecoveryPagesAsync(CovidPassportUser covidPassportUser, Certificate certificate, string languageCode, int nextPageNumber, int totalPages)
        {
            if (certificate == default)
            {
                return new List<string>();
            }

            var recoveryTemplateName = $"{languageCode}-international-recovery.hbs";

            var testResult = certificate.GetLatestDiagnosticResultFromEligibleResultsOrDefault();

            var pages = new List<string>();
            var pdfRecoveryDto = GenerateHandlebarsRecoveryDto(covidPassportUser, certificate, testResult, languageCode, nextPageNumber.ToString(), totalPages.ToString());
            var recoveryHtml = await GetHtmlAndCompileWithDataAsync(recoveryTemplateName, pdfRecoveryDto);
            pages.Add(recoveryHtml);

            return pages;
        }

        private HandlebarsVaccinationsDto GenerateHandlebarsVaccinesDto(CovidPassportUser user, string languageCode, (Vaccine vaccine, string qrCode) vaccineQrCodeTuple, DateTime validityEndDate, bool vaccineHeaderOverflow, string currentPage, string totalPages)
        {
            if (DateUtils.GetAgeInYears(user.DateOfBirth) < 16)
            {
                vaccineQrCodeTuple.vaccine.Site = "-";
            }
            return new HandlebarsVaccinationsDto()
            {
                Name = user.Name,
                DateOfBirth = StringUtils.GetTranslatedAndFormattedDate(user.DateOfBirth, languageCode),
                Vaccination = GenerateHandlebarsVaccineDto(languageCode, validityEndDate, vaccineQrCodeTuple.vaccine, vaccineQrCodeTuple.qrCode),
                VaccinationHeaderOverflows = vaccineHeaderOverflow,
                PageNumber = currentPage,
                TotalNumberOfPages = totalPages
            };

            HandlebarsVaccinationDto GenerateHandlebarsVaccineDto(string languageCode, DateTime validityEndDate, Vaccine vaccine, string qrCode)
            {
                var qrCodeString = qrImageGenerator.GenerateQrCodeString(qrCode);
                return new HandlebarsVaccinationDto()
                {
                    DisplayName = vaccine.DisplayName == null ? StringUtils.UseDashForEmptyOrBlankValues(vaccine.Product.Item2) : StringUtils.UseDashForEmptyOrBlankValues(vaccine.DisplayName),
                    QRCodeToken = qrCodeString,
                    ExpiryDate = StringUtils.UtcTimeZoneConverterWithTranslationOption(validityEndDate, timeZoneInfo, languageCode),
                    DoseNumber = vaccine.DoseNumber.ToString(),
                    TotalSeriesOfDoses = vaccine.TotalSeriesOfDoses.ToString(),
                    Date = StringUtils.UseDashForEmptyOrBlankValues(StringUtils.UtcTimeZoneConverterWithTranslationOption(vaccine.VaccinationDate, timeZoneInfo, languageCode)),
                    Product = StringUtils.UseDashForEmptyOrBlankValues(vaccine.Product.Item2),
                    Manufacturer = StringUtils.UseDashForEmptyOrBlankValues(vaccine.VaccineManufacturer.Item2),
                    Type = StringUtils.UseDashForEmptyOrBlankValues(vaccine.VaccineType.Item2),
                    BatchNumber = StringUtils.UseDashForEmptyOrBlankValues(vaccine.VaccineBatchNumber),
                    DiseaseTargeted = vaccine.DiseaseTargeted.Item2,
                    CountryOfVaccination = StringUtils.UseDashForEmptyOrBlankValues(vaccine.CountryOfVaccination),
                    Authority = configuration["VaccinationAuthority"],
                    Site = StringUtils.UseDashForEmptyOrBlankValues(vaccine.Site),
                    IsBooster = vaccine.IsBooster
                };
            }
        }

        private HandlebarsRecoveryDto GenerateHandlebarsRecoveryDto(CovidPassportUser user, Certificate recoveryCertificate, TestResultNhs testResult, string languageCode, string currentPage, string totalPages)
        {
            var qrCode = qrImageGenerator.GenerateQrCodeString(recoveryCertificate?.QrCodeTokens[0]);
            var diseaseTargeted = StringUtils.UseDashForEmptyOrBlankValues(testResult.DiseaseTargeted.Item2);

            return new HandlebarsRecoveryDto()
            {
                Name = user.Name,
                DateOfBirth = StringUtils.GetTranslatedAndFormattedDate(user.DateOfBirth, languageCode), // TODO needs translating
                QrCode = qrCode,
                DateOfFirstPositiveTestResult = StringUtils.UseDashForEmptyOrBlankValues(StringUtils.UtcTimeZoneConverterWithTranslationOption(testResult.DateTimeOfTest, timeZoneInfo, languageCode)),
                CertificateType = StringUtils.UseDashForEmptyOrBlankValues(testResult.ValidityType),
                DiseaseTargeted = diseaseTargeted,
                CountryOfTest = StringUtils.UseDashForEmptyOrBlankValues(testResult.CountryOfAuthority),
                CertificateIssuer = configuration["VaccinationAuthority"],
                CertificateValidFrom = StringUtils.UseDashForEmptyOrBlankValues(StringUtils.UtcTimeZoneConverterWithTranslationOption(DateTime.UtcNow, timeZoneInfo, languageCode)),
                CertificateValidUntil = StringUtils.UseDashForEmptyOrBlankValues(StringUtils.UtcTimeZoneConverterWithTranslationOption(recoveryCertificate.ValidityEndDate, timeZoneInfo, languageCode)),
                PageNumber = currentPage,
                TotalNumberOfPages = totalPages
            };
        }

        private async Task<string> GetHtmlAndCompileWithDataAsync(string templateName, dynamic dto)
        {
            var template = await GetHtmlAsync(templateName);
            var compiledTemplate = Handlebars.Compile(template);
            var htmlPage = compiledTemplate(dto);

            return htmlPage;
        }

        private async Task<string> GetHtmlAsync(string templateName)
        {
            var cacheKey = $"handlebar-template:{templateName}";

            var template = await memoryCache.GetOrCreateCacheAsync(cacheKey,
                async () => await blobService.GetStringFromBlobAsync("email-views", templateName),
                DateTimeOffset.UtcNow.AddHours(1));

            return template;
        }
    }
}
