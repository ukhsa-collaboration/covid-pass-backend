using System;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.ResponseDtos;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi.Models;
using CovidCertificate.Backend.Interfaces.ManagementInformation;
using CovidCertificate.Backend.Models.StaticValues;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;

namespace CovidCertificate.Backend
{
    public class GetCertificateFunction
    {
        private const string Route = "GetCertificate";

        private readonly ICovidCertificateService covidCertificateService;
        private readonly IFeatureManager featureManager;
        private readonly IManagementInformationReportingService miReportingService;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;
        private readonly ILogger<GetCertificateFunction> logger;
        private readonly IDomesticCertificateWrapper domesticCertificateWrapper;
        private readonly ICovidResultsService covidResultsService;

        public GetCertificateFunction(ICovidCertificateService covidCertificateService,
            IFeatureManager featureManager,
            IManagementInformationReportingService miReportingService,
            IEndpointAuthorizationService endpointAuthorizationService,
            ILogger<GetCertificateFunction> logger,
            IDomesticCertificateWrapper domesticCertificateWrapper,
            ICovidResultsService covidResultsService)
        {
            this.covidCertificateService = covidCertificateService;
            this.endpointAuthorizationService = endpointAuthorizationService;
            this.featureManager = featureManager;
            this.miReportingService = miReportingService;
            this.logger = logger;
            this.domesticCertificateWrapper = domesticCertificateWrapper;
            this.covidResultsService = covidResultsService;
        }

        [FunctionName(Route)]
        [OpenApiOperation(operationId: Route, tags: new[] { "Certificate" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(DomesticCertificateResponse), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = Route)] HttpRequest req)
        {
            var odsCountry = StringUtils.UnknownCountryString;

            try
            {
                logger.LogInformation("GetCertificate (endpoint) was invoked");
                await CheckDomesticEnabledAsync();
                var validationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req);
                odsCountry = validationResult.UserProperties?.Country;
                logger.LogTraceAndDebug($"validationResult: IsValid {validationResult?.IsValid}, Response is {validationResult?.Response}");
                if (endpointAuthorizationService.ResponseIsInvalid(validationResult))
                {
                    logger.LogInformation("GetCertificate (endpoint) has finished");
                    miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureUnauth);

                    return validationResult.Response;
                }

                var covidUser = new CovidPassportUser(validationResult);
                logger.LogInformation($"covidUser hash: {covidUser?.ToNhsNumberAndDobHashKey()}");

                var idToken = endpointAuthorizationService.GetIdToken(req, true);
                var medicalResults = await covidResultsService.GetMedicalResultsAsync(covidUser, idToken, CertificateScenario.Domestic, NhsdApiKey.Attended);

                var certificateContainer = await covidCertificateService.GetDomesticCertificateAsync(covidUser, idToken, medicalResults);
                var certificate = certificateContainer.GetSingleCertificateOrNull();

                var expiredCertificateExists = await covidCertificateService.ExpiredCertificateExistsAsync(covidUser, certificate);

                var certificateResponse = await domesticCertificateWrapper.WrapAsync(certificateContainer, expiredCertificateExists);

                
                logger.LogInformation("GetCertificate (endpoint) has finished");
                var stringResponse = certificate != null ? MIReportingStatus.SuccessCert : MIReportingStatus.SuccessNoCert;

                var ageInYears = DateUtils.GetAgeInYears(covidUser.DateOfBirth);
                miReportingService.AddReportLogInformation(Route, odsCountry, stringResponse, ageInYears);

                return new OkObjectResult(certificateResponse);
            }
            catch (Exception e) when (e is BadRequestException || e is ArgumentException)
            {
                logger.LogError(e, e.Message);
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureBadRequest);

                return new BadRequestObjectResult("There seems to be a problem: bad request");
            }
            catch (UnauthorizedException e)
            {
                logger.LogWarning(e, e.Message);
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureUnauth);

                return new UnauthorizedObjectResult("There seems to be a problem: unauthorized");
            }
            catch (DisabledException e)
            {
                logger.LogWarning(e, e.Message);
                return new UnauthorizedObjectResult("This endpoint has been disabled");
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Failure);

                var result = new ObjectResult(e.Message);
                result.StatusCode = 500;

                return result;
            }
        }

        private async Task<MandatoryToggle> GetMandatoryToggleAsync()
        {
            if (!await featureManager.IsEnabledAsync(FeatureFlags.MandatoryCerts))
            {
                return MandatoryToggle.MandatoryVoluntaryOff;
            }

            return await featureManager.IsEnabledAsync(FeatureFlags.VoluntaryDomestic) ? MandatoryToggle.MandatoryAndVoluntary
                                                                                       : MandatoryToggle.MandatoryOnly;
        }

        private async Task CheckDomesticEnabledAsync()
        {
            if(!await featureManager.IsEnabledAsync(FeatureFlags.EnableDomestic))
            {
                throw new DisabledException("This endpoint has been disabled");
            }
        }
    }
}
