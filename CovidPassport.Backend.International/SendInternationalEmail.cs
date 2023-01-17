using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Interfaces.ManagementInformation;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Models.RequestDtos;
using CovidCertificate.Backend.Models.StaticValues;
using CovidCertificate.Backend.Models.Validators;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using static CovidCertificate.Backend.Services.PdfGeneration.PdfHttpRequestHeadersUtil;

namespace CovidCertificate.Backend.International
{
    public class SendInternationalEmail
    {
        private const string Route = "SendInternationalEmail";
        private static readonly EmailAddressValidator emailValidator = new EmailAddressValidator();

        private readonly ICovidCertificateService covidCertificateService;
        private readonly IQueueService queueService;
        private readonly string outputQueueName;
        private readonly IEmailLimiter emailLimiter;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;
        private readonly IPostEndpointValidationService postEndpointValidationService;
        private readonly ILogger<SendInternationalEmail> logger;
        private readonly IManagementInformationReportingService miReportingService;
        private readonly ICovidResultsService covidResultsService;

        public SendInternationalEmail(
            ILogger<SendInternationalEmail> logger,
            ICovidCertificateService covidCertificateService,
            IQueueService queueService,
            IConfiguration configuration,
            IEmailLimiter emailLimiter,
            IManagementInformationReportingService miReportingService,
            IPostEndpointValidationService postEndpointValidationService,
            IEndpointAuthorizationService endpointAuthorizationService,
            ICovidResultsService covidResultsService)
        {
            this.covidCertificateService = covidCertificateService;
            this.queueService = queueService;
            outputQueueName = configuration["GeneratePdfEmailHtmlQueueInt"];
            this.emailLimiter = emailLimiter;
            this.miReportingService = miReportingService;
            this.logger = logger;
            this.endpointAuthorizationService = endpointAuthorizationService;
            this.postEndpointValidationService = postEndpointValidationService;
            this.covidResultsService = covidResultsService;
        }

        [FunctionName(Route)]
        [OpenApiOperation(operationId: Route, tags: new[] { "Email" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Post(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = Route)] HttpRequest req)
        {
            var odsCountry = StringUtils.UnknownCountryString;

            try
            {
                logger.LogInformation("SendInternationalEmailPost (endpoint post) was invoked");

                var returnResult = await postEndpointValidationService.ValidatePostAsync<SendInternationalEmailDto, SendInternationalEmailDtoValidator>(req);
                if (returnResult.GetType() != typeof(OkObjectResult))
                {
                    logger.LogInformation($"Invalid SendInternationalEmailDto");
                    logger.LogInformation("SendInternationalEmail has finished");
                    return returnResult;
                }
                var resultObject = (OkObjectResult)returnResult;
                var dto = (SendInternationalEmailDto)resultObject.Value;
                var email = dto.Email;

                var validationResult = emailValidator.Validate(email);
                logger.LogTraceAndDebug($"email validationResult: IsValid is {validationResult?.IsValid}");

                if (!validationResult.IsValid)
                {
                    logger.LogInformation($"Validation result is invalid");
                    logger.LogInformation("SendCertificate (endpoint get) has finished");
                    return new BadRequestObjectResult(validationResult.Errors);
                }

                var authorisationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req, "NhsLogin");
                odsCountry = authorisationResult.UserProperties?.Country;
                logger.LogTraceAndDebug($"authorisationResult: IsValid is {authorisationResult?.IsValid}, Response is {authorisationResult?.Response}");

                if (endpointAuthorizationService.ResponseIsInvalid(authorisationResult))
                {
                    logger.LogInformation("SendInternationalPDF (endpoint get) has finished");
                    return authorisationResult.Response;
                }

                var covidUser = new CovidPassportUser(authorisationResult);
               
                var emailAttempts = await emailLimiter.GetUserEmailAttempts(covidUser);

                if (!emailLimiter.UserWithinEmailLimit(emailAttempts, CertificateScenario.International))
                {
                    miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureTooManyRequests);
                    return new StatusCodeResult(StatusCodes.Status429TooManyRequests);
                }

                var idToken = endpointAuthorizationService.GetIdToken(req, true);

                var medicalResults = await covidResultsService.GetMedicalResultsAsync(covidUser, idToken, CertificateScenario.International, NhsdApiKey.Attended);
                var vaccinationCertificateTask = covidCertificateService.GetInternationalCertificateAsync(covidUser, idToken, CertificateType.Vaccination, medicalResults);
                var recoveryCertificateTask = covidCertificateService.GetInternationalCertificateAsync(covidUser, idToken, CertificateType.Recovery, medicalResults);

                await Task.WhenAll(vaccinationCertificateTask, recoveryCertificateTask);

                var vaccinationCertificate = (await vaccinationCertificateTask).GetSingleCertificateOrNull();
                var recoveryCertificate = (await recoveryCertificateTask).GetSingleCertificateOrNull();

                if (recoveryCertificate == default && vaccinationCertificate == default)
                {
                    logger.LogTraceAndDebug($"testrawVaccineData and rawVaccineData are empty ");
                    logger.LogInformation("GetRecoveryBasedInternationalQR has finished");
                    return new NoContentResult();
                }

                var contentLanguage = req.GetContentLanguage();
                var (pdfType, doseNumber) = GetPdfHeaderValues(req);
                logger.LogTraceAndDebug($"Template Name is {contentLanguage}");

                var tempSendInternationalEmail = await SendInternationalAsync(covidUser,
                                                                         vaccinationCertificate,
                                                                         recoveryCertificate,
                                                                         dto.Email,
                                                                         contentLanguage,
                                                                         emailAttempts, pdfType, doseNumber);

                logger.LogInformation("SendInternationalEmail (endpoint get) has finished");

                return tempSendInternationalEmail;
            }
            catch (UnauthorizedException e)
            {
                logger.LogWarning(e, e.Message);

                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureUnauth);

                return new UnauthorizedObjectResult(e.Message);
            }
            catch (BadRequestException e)
            {
                logger.LogWarning(e, e.Message);

                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureBadRequest);

                return new BadRequestObjectResult("There seems to be a problem: bad request");
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<IActionResult> SendInternationalAsync(
            CovidPassportUser covidUser,
            Certificate vaccinationCertificate,
            Certificate recoveryCertificate,
            string emailToSendTo,
            string templateName,
            UserDailyEmailAttempts emailAttempts, PDFType type, int doseNumber)
        {
            logger.LogInformation("SendInternational was invoked");

            var serviceBusDto = new InternationalEmailServiceBusRequestDto
            {
                EmailToSendTo = emailToSendTo,
                CovidPassportUser = covidUser,
                VaccinationCertificate = vaccinationCertificate,
                RecoveryCertificate = recoveryCertificate,
                LanguageCode = templateName,
                VaccinationData = vaccinationCertificate?.GetAllVaccinationsFromEligibleResults(),
                RecoveryData = new List<TestResultNhs> { recoveryCertificate?.GetLatestDiagnosticResultFromEligibleResultsOrDefault() },
                Type = type,
                DoseNumber = doseNumber
            };

            await emailLimiter.UpdateUserDailyEmailAttempts(emailAttempts, CertificateScenario.International);

            var result = await queueService.SendMessageAsync(outputQueueName, serviceBusDto);
            var odsCountry = covidUser.Country;
            logger.LogTraceAndDebug($"result is {result}");
            if (!result)
            {
                logger.LogInformation("SendInternational has finished");

                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Failure);

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("SendInternational has finished");

            miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Success);

            return new OkResult(); // AcceptedResult();
        }
    }
}
