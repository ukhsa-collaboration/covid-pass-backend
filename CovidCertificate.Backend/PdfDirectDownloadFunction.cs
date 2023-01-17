using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Interfaces.PdfLimiters;
using Microsoft.FeatureManagement;
using CovidCertificate.Backend.Interfaces.ManagementInformation;
using CovidCertificate.Backend.Models.StaticValues;
using Microsoft.OpenApi.Models;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;

namespace CovidCertificate.Backend
{
    public class PdfDirectDownloadFunction
    {
        private const string FunctionName = "PdfDirectDownloadFunction";

        private readonly ICovidCertificateService covidCertificateService;
        private readonly IHtmlGeneratorService htmlGeneratorService;
        private readonly IPdfGeneratorService pdfGeneratorService;
        private readonly IDomesticPdfLimiter domesticPdfLimiter;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;
        private readonly ILogger<PdfDirectDownloadFunction> logger;
        private readonly IManagementInformationReportingService miReportingService;
        private readonly IFeatureManager featureManager;
        private readonly ICovidResultsService covidResultsService;

        public PdfDirectDownloadFunction(ICovidCertificateService covidCertificateService,
                                         IHtmlGeneratorService htmlGeneratorService,
                                         IFeatureManager featureManager,
                                         IPdfGeneratorService pdfGeneratorService,
                                         ILogger<PdfDirectDownloadFunction> logger,
                                         IDomesticPdfLimiter domesticPdfLimiter, 
                                         IManagementInformationReportingService miReportingService,
                                         IEndpointAuthorizationService endpointAuthorizationService,
                                         ICovidResultsService covidResultsService)
        {
            this.covidCertificateService = covidCertificateService;
            this.htmlGeneratorService = htmlGeneratorService;
            this.pdfGeneratorService = pdfGeneratorService;
            this.domesticPdfLimiter = domesticPdfLimiter;
            this.logger = logger;
            this.endpointAuthorizationService = endpointAuthorizationService;
            this.miReportingService = miReportingService;
            this.featureManager = featureManager;
            this.covidResultsService = covidResultsService;
        }

        [FunctionName(FunctionName)]
        [OpenApiOperation(operationId: FunctionName, tags: new[] { "PDF" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "PdfDirectDownload")] HttpRequest req)
        {
            try
            {
                logger.LogInformation("PdfDirectDownloadFunction was invoked");
                await CheckDomesticEnabledAsync();
                var authorisationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req);
                var odsCountry = authorisationResult.UserProperties?.Country ?? StringUtils.UnknownCountryString;
                logger.LogTraceAndDebug($"authorisationResult: IsValid is {authorisationResult?.IsValid}, Response is {authorisationResult?.Response}");

                if (endpointAuthorizationService.ResponseIsInvalid(authorisationResult))
                {
                    logger.LogInformation("PdfDirectDownloadFunction has finished");
                    return authorisationResult.Response;
                }

                var covidUser = new CovidPassportUser(authorisationResult);
                logger.LogInformation($"covidUser hash: {covidUser?.ToNhsNumberAndDobHashKey()}");

                (bool isUserAllowedToDownloadPdf, int retryAfterSeconds) =
                    await domesticPdfLimiter.GetUserAllowanceAndRetryTimeForDomesticPdfAsync(covidUser);

                if (!isUserAllowedToDownloadPdf)
                {
                    logger.LogWarning($"User has reached domestic PDFs limit. Next time Pdf can be requested after: '{retryAfterSeconds}' seconds.");

                    miReportingService.AddReportLogInformation(FunctionName, odsCountry, MIReportingStatus.FailureTooManyRequests);

                    req.HttpContext.Response.Headers.Add("Retry-After", retryAfterSeconds.ToString());

                    return new StatusCodeResult((int)HttpStatusCode.TooManyRequests);
                }

                var templateLanguage = req.GetContentLanguage();
                logger.LogTraceAndDebug($"language is {templateLanguage}");

                var idToken = endpointAuthorizationService.GetIdToken(req);
                var medicalResults = await covidResultsService.GetMedicalResultsAsync(covidUser, idToken, Models.Enums.CertificateScenario.Domestic, NhsdApiKey.Attended);

                var certificateContainer = await covidCertificateService.GetDomesticCertificateAsync(covidUser, idToken, medicalResults);
                var certificate = certificateContainer.GetSingleCertificateOrNull();

                if (certificate == default)
                {
                    logger.LogTraceAndDebug("certificate == default");
                    logger.LogInformation("PdfDirectDownloadFunction has finished");
                    return new NoContentResult();
                }
                
                logger.LogTraceAndDebug($"certificate:{certificate}");

                var domesticDownloadRequestObject = await pdfGeneratorService.GetDirectDownloadPdfRequestObjectAsync(covidUser, certificate, templateLanguage, htmlGeneratorService);
                var pdfContent = await pdfGeneratorService.GeneratePdfDocumentStreamAsync(domesticDownloadRequestObject);

                if (!pdfContent.CanRead)
                {
                    logger.LogCritical("Could not read pdf stream");
                    logger.LogInformation("PdfDirectDownloadFunction has finished");
                    miReportingService.AddReportLogInformation(FunctionName, odsCountry, MIReportingStatus.Failure);

                    return new StatusCodeResult(500);
                }
                
                logger.LogInformation("PdfDirectDownloadFunction has finished");

                // Update daily attempts, just before sending PDF to user
                await domesticPdfLimiter.AddUserDailyDomesticPdfAttemptAsync(covidUser);

                var ageInYears = DateUtils.GetAgeInYears(covidUser.DateOfBirth);
                miReportingService.AddReportLogInformation(FunctionName, odsCountry, MIReportingStatus.Success, ageInYears);

                return new FileStreamResult(pdfContent, "application/pdf");
            }
            catch (DisabledException e)
            {
                logger.LogWarning(e, e.Message);
                return new UnauthorizedObjectResult(e.Message);
            }
            catch (Exception e)
            {
                logger.LogCritical(e, e.Message);
                throw;
            }
            
        }
        private async Task CheckDomesticEnabledAsync()
        {
            if (!await featureManager.IsEnabledAsync(FeatureFlags.EnableDomestic))
            {
                throw new DisabledException("This endpoint has been disabled");
            }
        }
    }
}
