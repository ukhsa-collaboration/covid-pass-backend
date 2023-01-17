using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Interfaces.PdfLimiters;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using CovidCertificate.Backend.Interfaces.ManagementInformation;
using CovidCertificate.Backend.Models.StaticValues;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using static CovidCertificate.Backend.Services.PdfGeneration.PdfHttpRequestHeadersUtil;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;

namespace CovidCertificate.Backend.International
{
    public class GetInternationalPDF
    {
        private const string Route = "GetInternationalPDF";

        private readonly IPdfGeneratorService pdfGeneratorService;
        private readonly ICovidCertificateService covidCertificateService;
        private readonly IPdfContentGenerator pdfContentGenerator;
        private readonly IInternationalPdfLimiter internationalPdfLimiter;
        private readonly IManagementInformationReportingService miReportingService;
        private readonly ILogger<GetInternationalPDF> logger;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;
        private readonly ICovidResultsService covidResultsService;

        public GetInternationalPDF(
            IPdfGeneratorService pdfGeneratorService,
            ILogger<GetInternationalPDF> logger,
            ICovidCertificateService covidCertificateService,
            IPdfContentGenerator pdfContentGenerator,
            IInternationalPdfLimiter internationalPdfLimiter,
            IManagementInformationReportingService miReportingService,
            IEndpointAuthorizationService endpointAuthorizationService,
            ICovidResultsService covidResultsService)
        {
            this.pdfGeneratorService = pdfGeneratorService;
            this.covidCertificateService = covidCertificateService;
            this.pdfContentGenerator = pdfContentGenerator;
            this.internationalPdfLimiter = internationalPdfLimiter;
            this.miReportingService = miReportingService;
            this.endpointAuthorizationService = endpointAuthorizationService;
            this.covidResultsService = covidResultsService;
            this.logger = logger;
        }

        [FunctionName(Route)]
        [OpenApiOperation(operationId: Route, tags: new[] { "PDF" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            var odsCountry = StringUtils.UnknownCountryString;
            
            try
            {
                logger.LogInformation("GetInternationalPDF was invoked");

                var authorisationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req, "NhsLogin");
                odsCountry = authorisationResult.UserProperties?.Country;
                logger.LogTraceAndDebug($"validationResult: IsValid {authorisationResult?.IsValid}, Response is {authorisationResult?.Response}");

                if (endpointAuthorizationService.ResponseIsInvalid(authorisationResult))
                {
                    logger.LogInformation("GetInternationalPDF has finished");
                    return authorisationResult.Response;
                }

                //Get User 
                var covidUser = new CovidPassportUser(authorisationResult);

                (bool isUserAllowedToDownloadPdf, int retryAfterSeconds) =
                    await internationalPdfLimiter.GetUserAllowanceAndRetryTimeForInternationalPdfAsync(covidUser);

                if (!isUserAllowedToDownloadPdf)
                {
                    miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureTooManyRequests);

                    logger.LogWarning($"User has reached international PDFs limit. Next time Pdf can be requested after: '{retryAfterSeconds}' seconds.");

                    req.HttpContext.Response.Headers.Add("Retry-After", retryAfterSeconds.ToString());

                    return new StatusCodeResult((int)HttpStatusCode.TooManyRequests);
                }

                //Get HTML Template
                var languageCode = req.GetContentLanguage();
                logger.LogTraceAndDebug($"Language Code is {languageCode}");

                var idToken = endpointAuthorizationService.GetIdToken(req, true);

                var medicalResults = await covidResultsService.GetMedicalResultsAsync(covidUser, idToken, CertificateScenario.International, NhsdApiKey.Attended);
                var vaccinationCertificateTask = covidCertificateService.GetInternationalCertificateAsync(covidUser, idToken, CertificateType.Vaccination, medicalResults);
                var recoveryCertificateTask = covidCertificateService.GetInternationalCertificateAsync(covidUser, idToken, CertificateType.Recovery, medicalResults);

                await Task.WhenAll(vaccinationCertificateTask, recoveryCertificateTask);

                Certificate vaccinationCertificate = (await vaccinationCertificateTask).GetSingleCertificateOrNull();
                Certificate recoveryCertificate = (await recoveryCertificateTask).GetSingleCertificateOrNull();

                if (recoveryCertificate == default && vaccinationCertificate == default)
                {
                    miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureNoContent);

                    logger.LogTraceAndDebug($"testrawVaccineData and rawVaccineData are empty ");
                    logger.LogInformation("GetInternationalPDF has finished");
                    return new StatusCodeResult(204);
                }

                var (pdfType, doseNumber) = GetPdfHeaderValues(req);

                var pdfContent = await pdfContentGenerator.GenerateInternationalAsync(
                    covidPassportUser: covidUser,
                    vaccinationCertificate: vaccinationCertificate,
                    recoveryCertificate: recoveryCertificate,
                    languageCode: languageCode,
                    type: pdfType,
                    doseNumber: doseNumber
                );

                if (!pdfContent.Body.Any())
                {
                    miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureNoPdfBody);

                    logger.LogCritical("Could not fetch body html");
                    throw new Exception("Body html couldn't be fetched");
                }

                var pdfContentStream = await pdfGeneratorService.GeneratePdfContentStreamAsync(pdfContent);

                if (!pdfContentStream.CanRead)
                {
                    miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Failure);

                    logger.LogCritical("Could not read pdf stream");
                    logger.LogInformation("GetInternationalPDF has finished");
                    return new StatusCodeResult(500);
                }

                // Update daily attempts, just before sending PDF to user
                await internationalPdfLimiter.AddUserDailyInternationalPdfAttemptAsync(covidUser);

                logger.LogInformation("GetInternationalPDF has finished");

                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.SuccessCert);
                
                return new FileStreamResult(pdfContentStream, "application/pdf");
            }
            catch (BadRequestException e)
            {
                logger.LogWarning(e, e.Message);

                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureBadRequest);

                return new BadRequestObjectResult("There seems to be a problem: bad request");
            }
            catch (UnauthorizedException e)
            {
                logger.LogWarning(e, e.Message);
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureUnauth);

                return new UnauthorizedObjectResult(e.Message); ;
            }
            catch (Exception e)
            {
                logger.LogCritical(e, e.Message);
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Failure);

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
