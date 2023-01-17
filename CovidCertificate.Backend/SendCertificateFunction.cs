using System;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Models.RequestDtos;
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
using Microsoft.FeatureManagement;
using CovidCertificate.Backend.Interfaces.ManagementInformation;
using CovidCertificate.Backend.Models.StaticValues;
using CovidCertificate.Backend.Utils;
using Microsoft.OpenApi.Models;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend
{
    public class SendCertificateFunction
    {
        private const string FunctionName = "SendCertificate";
        private static readonly EmailAddressValidator emailValidator = new EmailAddressValidator();

        private readonly IQueueService queueService;
        private readonly ICovidCertificateService covidCertificateCreator;
        private readonly string outputQueueName;
        private readonly TimeZoneInfo timeZoneInfo;
        private readonly IEmailLimiter emailLimiter;
        private readonly IFeatureManager featureManager;
        private readonly IManagementInformationReportingService miReportingService;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;
        private readonly IPostEndpointValidationService postEndpointValidationService;
        private readonly ILogger<SendCertificateFunction> logger;
        private readonly ICovidResultsService covidResultsService;

        public SendCertificateFunction(IQueueService queueService,
                                     ICovidCertificateService covidCertificateCreator,
                                     IConfiguration configuration, IGetTimeZones timeZones,
                                     IEmailLimiter emailLimiter,
                                     IFeatureManager featureManager, 
                                     IManagementInformationReportingService miReportingService,
                                     IEndpointAuthorizationService endpointAuthorizationService,
                                     IPostEndpointValidationService postEndpointValidationService,
                                     ILogger<SendCertificateFunction> logger,
                                     ICovidResultsService covidResultsService)
        {
            this.queueService = queueService;
            this.covidCertificateCreator = covidCertificateCreator;
            outputQueueName = configuration["GeneratePdfEmailHtmlQueue"];
            timeZoneInfo = timeZones.GetTimeZoneInfo();
            this.emailLimiter = emailLimiter;
            this.featureManager = featureManager;
            this.endpointAuthorizationService = endpointAuthorizationService;
            this.logger = logger;
            this.postEndpointValidationService = postEndpointValidationService;
            this.miReportingService = miReportingService;
            this.covidResultsService = covidResultsService;
        }

        [FunctionName(FunctionName)]
        [OpenApiOperation(operationId: FunctionName, tags: new[] { "Certificate" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(IActionResult), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NoContent, contentType: "text/plain", bodyType: typeof(string), Description = "The no content response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.TooManyRequests, contentType: "text/plain", bodyType: typeof(string), Description = "The too many requests response")]
        public async Task<IActionResult> Post(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = FunctionName)] HttpRequest req)
        {
            logger.LogInformation($"{FunctionName} was invoked");
            try
            {
                await CheckDomesticEnabledAsync();
            }
            catch (DisabledException e)
            {
                logger.LogWarning(e, e.Message);
                return new UnauthorizedObjectResult(e.Message);
            }
            var returnResult = await postEndpointValidationService.ValidatePostAsync<SendCertificateDto, SendCertificateDtoValidator>(req);
            if (returnResult.GetType() != typeof(OkObjectResult))
            {
                logger.LogInformation($"Invalid SendCertificateDto");
                logger.LogInformation($"{FunctionName} has finished");
                return returnResult;
            }
            var resultObject = (OkObjectResult)returnResult;
            var dto = (SendCertificateDto)resultObject.Value;

            string email = dto.Email;

            var validationResult = emailValidator.Validate(email);
            logger.LogTraceAndDebug($"email validationResult: IsValid is {validationResult?.IsValid}");
            if (!validationResult.IsValid)
            {
                logger.LogInformation($"Validation result is invalid");
                logger.LogInformation("SendCertificate (endpoint post) has finished");

                return new BadRequestObjectResult(validationResult.Errors);
            }

            var authorisationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req);
            var odsCountry = authorisationResult.UserProperties?.Country ?? StringUtils.UnknownCountryString;
            logger.LogTraceAndDebug($"authorisationResult: IsValid is {authorisationResult?.IsValid}, Response is {authorisationResult?.Response}");
            if (endpointAuthorizationService.ResponseIsInvalid(authorisationResult))
            {
                logger.LogInformation("SendCertificate has finished");
                return authorisationResult.Response;
            }
            var covidUser = new CovidPassportUser(authorisationResult);
            logger.LogInformation($"covidUser hash: {covidUser?.ToNhsNumberAndDobHashKey()}");

            var emailAttempts = await emailLimiter.GetUserEmailAttempts(covidUser);

            if (!emailLimiter.UserWithinEmailLimit(emailAttempts, CertificateScenario.Domestic))
            {
                miReportingService.AddReportLogInformation(FunctionName, odsCountry, MIReportingStatus.FailureTooManyRequests);
                return new StatusCodeResult(StatusCodes.Status429TooManyRequests);
            }

            var idToken = endpointAuthorizationService.GetIdToken(req);
            var medicalResults = await covidResultsService.GetMedicalResultsAsync(covidUser, idToken, CertificateScenario.Domestic, NhsdApiKey.Attended);

            var certificateModel = await covidCertificateCreator.GetDomesticCertificateAsync(covidUser, idToken, medicalResults);
            Certificate certificate = certificateModel.GetSingleCertificateOrNull();
            if (certificate == default)
            {
                logger.LogTraceAndDebug("certificate is null");
                logger.LogInformation("SendCertificate has finished");
                return new NoContentResult();
            }
            var templateLanguage = req.GetContentLanguage();
            logger.LogTraceAndDebug($"language is {templateLanguage}");

            var mandatoryCertsOn = await featureManager.IsEnabledAsync(FeatureFlags.MandatoryCerts);
            var voluntaryDomesticOn = await featureManager.IsEnabledAsync(FeatureFlags.VoluntaryDomestic);
            var mandatoryToggle = mandatoryCertsOn && voluntaryDomesticOn ? "-two-pass" : "-one-pass";
            var templateName = (certificate.CertificateType == CertificateType.DomesticVoluntary)
                    ? $"{templateLanguage}{mandatoryToggle}-wales-only"
                    : templateLanguage + mandatoryToggle;

            logger.LogTraceAndDebug($"Template Name is {templateName }");


            var pdfDto = new AddPdfCertificateRequestDto
            {
                Name = covidUser.Name,
                DateOfBirth = covidUser.DateOfBirth,
                Email = email,
                TemplateName = templateName,
                Expiry = TimeZoneInfo.ConvertTimeFromUtc(TimeFormatConvert.ToUniversal(certificate.ValidityEndDate), timeZoneInfo),
                EligibilityPeriod = certificate.eligibilityEndDate,
                QrCodeToken = certificate.QrCodeTokens[0],
                CertificateType = certificate.CertificateType,
                UniqueCertificateIdentifier = certificate.UniqueCertificateIdentifier,
                LanguageCode = templateLanguage
            };

            var certificateSent = await covidCertificateCreator.SendCertificateAsync(pdfDto, outputQueueName);

            if (!certificateSent)
            {
                logger.LogTraceAndDebug("SendCertificate has finished");
                miReportingService.AddReportLogInformation(FunctionName, odsCountry, MIReportingStatus.Failure);

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            await emailLimiter.UpdateUserDailyEmailAttempts(emailAttempts, CertificateScenario.Domestic);
            logger.LogTraceAndDebug("SendCertificate has finished");
            miReportingService.AddReportLogInformation(FunctionName, odsCountry, MIReportingStatus.Success);

            return new OkResult();
        }

        /// <summary>
        /// Sends the certificate to the message queue
        /// </summary>
        /// <param name="covidUser">The user whos certificate this represents</param>
        /// <param name="emailToSendTo">The email we're sending it too</param>
        /// <param name="templateName"></param>
        /// <param name="expiry"></param>
        /// <param name="qrCodeToken"></param>
        /// <returns></returns>
        private async Task<IActionResult> SendCertificateAsync(CovidPassportUser covidUser,
                                                          string emailToSendTo,
                                                          string templateName,
                                                          DateTime expiry,
                                                          string qrCodeToken,
                                                          CertificateType certificateType,
                                                          DateTime eligibilityEndDate,
                                                          string uvci,
                                                          UserDailyEmailAttempts emailAttempts,
                                                          string languageCode)
        {
            logger.LogTraceAndDebug("SendCertificate was invoked");

            var utc = TimeFormatConvert.ToUniversal(expiry);

            var dto = new AddPdfCertificateRequestDto
            {
                Name = covidUser.Name,
                DateOfBirth = covidUser.DateOfBirth,
                Email = emailToSendTo,
                TemplateName = templateName,
                Expiry = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZoneInfo),
                EligibilityPeriod = eligibilityEndDate,
                QrCodeToken = qrCodeToken,
                CertificateType = certificateType,
                UniqueCertificateIdentifier = uvci,
                LanguageCode = languageCode
            };

            await emailLimiter.UpdateUserDailyEmailAttempts(emailAttempts, CertificateScenario.Domestic);
            var odsCountry = covidUser.Country;
            var result = await queueService.SendMessageAsync(outputQueueName, dto);
            logger.LogTraceAndDebug($"result is {result}");

            if (!result)
            {
                logger.LogTraceAndDebug("SendCertificate has finished");
                miReportingService.AddReportLogInformation(FunctionName, odsCountry, MIReportingStatus.Failure);

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            logger.LogTraceAndDebug("SendCertificate has finished");

            var ageInYears = DateUtils.GetAgeInYears(covidUser.DateOfBirth);
            miReportingService.AddReportLogInformation(FunctionName, odsCountry, MIReportingStatus.Success, ageInYears);

            return new OkResult(); // AcceptedResult();
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
