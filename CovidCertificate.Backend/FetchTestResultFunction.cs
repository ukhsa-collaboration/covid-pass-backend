using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.ManagementInformation;
using CovidCertificate.Backend.Models.StaticValues;
using CovidCertificate.Backend.Utils.Extensions;
using System.Net;
using System.Collections.Generic;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;

namespace CovidCertificate.Backend
{
    public class FetchTestResultFunction
    {
        private const string Route = "GetTestResults";
        private readonly IDiagnosticTestResultsService testResultsService;
        private readonly IManagementInformationReportingService miReportingService;
        private readonly ILogger<FetchTestResultFunction> logger;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;

        public FetchTestResultFunction(
            IDiagnosticTestResultsService testResultsService,
            ILogger<FetchTestResultFunction> logger,
            IEndpointAuthorizationService endpointAuthorizationService,
            IManagementInformationReportingService miReportingService)
        {
            this.testResultsService = testResultsService;
            this.miReportingService = miReportingService;
            this.endpointAuthorizationService = endpointAuthorizationService;
            this.logger = logger;
        }

        [FunctionName(Route)]
        [OpenApiOperation(operationId: Route, tags: new[] { "Test Results" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(IEnumerable<TestResultNhs>), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetTestResults")] HttpRequest req)
        {
            var odsCountry = StringUtils.UnknownCountryString;

            try
            {
                logger.LogInformation("GetTestResults was invoked");

                var validationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req);
                odsCountry = validationResult.UserProperties?.Country;
                logger.LogTraceAndDebug($"validationResult: IsValid {validationResult?.IsValid}, Response is {validationResult?.Response}");

                if (endpointAuthorizationService.ResponseIsInvalid(validationResult))
                {
                    logger.LogInformation($"Validation result is invalid or forbidden");
                    logger.LogInformation("GetTestResults has finished");
                    return validationResult.Response;
                }

                var covidUser = new CovidPassportUser(validationResult);
                logger.LogInformation($"covidUser hash: {covidUser?.ToNhsNumberAndDobHashKey()}");

                var results = await testResultsService.GetDiagnosticTestResultsAsync(endpointAuthorizationService.GetIdToken(req), NhsdApiKey.Attended);
                logger.LogInformation("GetTestResults has finished");

                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Success);

                return new OkObjectResult(results);
            }
            catch (Exception e) when (e is BadRequestException || e is ArgumentNullException || e is ArgumentException)
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
