using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using CovidCertificate.Backend.Interfaces.ManagementInformation;
using CovidCertificate.Backend.Models.StaticValues;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.ResponseDtos;
using Microsoft.FeatureManagement;

namespace CovidCertificate.Backend
{
    public class FetchVaccinationResultFunction
    {
        private const string Route = "GetVaccinationResults";

        private readonly IVaccineService vaccinesService;
        private readonly TimeZoneInfo timeZoneInfo;
        private readonly IManagementInformationReportingService miReportingService;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;
        private readonly ILogger<FetchVaccinationResultFunction> logger;
        private readonly IFeatureManager featureManager;

        public FetchVaccinationResultFunction(
            IVaccineService vaccinesService,
            IFeatureManager featureManager,
            ILogger<FetchVaccinationResultFunction> logger, 
            IGetTimeZones timeZones,
            IManagementInformationReportingService miReportingService,
            IEndpointAuthorizationService endpointAuthorizationService)
        {
            this.vaccinesService = vaccinesService;
            this.featureManager = featureManager;
            this.endpointAuthorizationService = endpointAuthorizationService;
            this.logger = logger;
            this.miReportingService = miReportingService;
            timeZoneInfo = timeZones.GetTimeZoneInfo();
        }

        [FunctionName(Route)]
        [OpenApiOperation(operationId: "getVaccinationResults", tags: new[] { "Vaccination Results" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(IEnumerable<Vaccine>), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = Route)] HttpRequest request)
        {
            var odsCountry = StringUtils.UnknownCountryString;

            try
            {
                logger.LogInformation("GetVaccinationResults was invoked");
                var validationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(request, "NhsLogin");
                odsCountry = validationResult.UserProperties?.Country;
                logger.LogTraceAndDebug($"validationResult: IsValid {validationResult?.IsValid}, Response is {validationResult?.Response}");

                if (endpointAuthorizationService.ResponseIsInvalid(validationResult))
                {
                    logger.LogInformation($"validationResults is invalid or forbidden");
                    logger.LogInformation("GetVaccinationResults has finished");
                    return validationResult?.Response;
                }

                var covidUser = new CovidPassportUser(validationResult);
                logger.LogInformation($"covidUser hash: {covidUser?.ToNhsNumberAndDobHashKey()}");

                var vaccineresults = await vaccinesService.GetAttendedVaccinesAsync(endpointAuthorizationService.GetIdToken(request, true), covidUser, NhsdApiKey.Attended);

                if (vaccineresults is null)
                {
                    logger.LogTraceAndDebug($"vaccineresults is null");
                    logger.LogInformation("GetVaccinationResults has finished");
                    return new BadRequestObjectResult("Invalid Authorisation Level");
                }

                logger.LogInformation("GetVaccinationResults has finished");

                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Success);

                return new OkObjectResult(vaccineresults.Select(x => new VaccineResponse(x, timeZoneInfo)));
            }
            catch (Exception e) when (e is BadRequestException || e is ArgumentNullException)
            {
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureBadRequest);
                logger.LogWarning(e, e.Message);

                return new BadRequestObjectResult("There seems to be a problem: bad request");
            }
            catch (UnauthorizedException e)
            {
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureUnauth);
                logger.LogWarning(e, e.Message);

                return new UnauthorizedObjectResult("There seems to be a problem: unauthorized");
            }
            catch (ForbiddenException e)
            {
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureForbidden);
                logger.LogWarning(e, e.Message);

                return new ObjectResult("There seems to be a problem: forbidden") { StatusCode = StatusCodes.Status403Forbidden };
            }
            catch (HttpRequestException e)
            {
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureInternal);
                logger.LogCritical(e, e.Message);

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }        
            catch (Exception e)
            {
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Failure);
                logger.LogError(e, e.Message);

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
