using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using Microsoft.OpenApi.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using System.Collections.Generic;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Interfaces.ManagementInformation;
using CovidCertificate.Backend.Models.Exceptions;

namespace CovidCertificate.Backend
{
    public class GetMedicalExemptionsFunction
    {
        private const string Route = "GetMedicalExemptions";

        private readonly IMedicalExemptionService medicalExemptionService;
        private readonly ILogger<GetMedicalExemptionsFunction> logger;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;
        private readonly IManagementInformationReportingService managementInformationReportingService;

        public GetMedicalExemptionsFunction(IMedicalExemptionService medicalExemptionService,
            ILogger<GetMedicalExemptionsFunction> logger,
            IEndpointAuthorizationService endpointAuthorizationService,
            IManagementInformationReportingService managementInformationReportingService)
        {
            this.medicalExemptionService = medicalExemptionService;
            this.logger = logger;
            this.endpointAuthorizationService = endpointAuthorizationService;
            this.managementInformationReportingService = managementInformationReportingService;
        }

        [FunctionName(Route)]
        [OpenApiOperation(operationId: Route, tags: new[] { "Medical Exemption" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(IEnumerable<MedicalExemption>), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "text/plain", bodyType: typeof(string), Description = "The forbidden response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = Route)] HttpRequest req)
        {
            try
            {
                logger.LogInformation("GetMedicalExemptions was invoked");
                var validationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req);
                logger.LogTraceAndDebug($"validationResult: IsValid {validationResult?.IsValid}, Response is {validationResult?.Response}");
                if (endpointAuthorizationService.ResponseIsInvalid(validationResult))
                {
                    logger.LogInformation("GetMedicalExemptions has finished");
                    return validationResult.Response;
                }

                var covidUser = new CovidPassportUser(validationResult);
                logger.LogInformation($"covidUser hash: {covidUser?.ToNhsNumberAndDobHashKey()}");

                var exemptions = await medicalExemptionService.GetMedicalExemptionsAsync(covidUser, endpointAuthorizationService.GetIdToken(req));

                foreach (var exemption in exemptions)
                {
                    managementInformationReportingService.AddReportLogMedicalExemptionInformation(Route, exemption.ExemptionReasonCode.ToString(),
                        exemption.ExemptionReason);
                }

                logger.LogInformation("GetMedicalExemptions has finished");

                return new OkObjectResult(exemptions);
            }
            catch (Exception e) when (e is BadRequestException || e is ArgumentException)
            {
                logger.LogError(e, e.Message);
                return new BadRequestObjectResult("There seems to be a problem: Bad request");
            }
            catch (UnauthorizedException e)
            {
                logger.LogWarning(e, e.Message);
                return new UnauthorizedObjectResult("There seems to be a problem: Unauthorized");
            }
            catch (ForbiddenException e)
            {
                logger.LogWarning(e, e.Message);
                return new ObjectResult("There seems to be a problem: Forbidden") { StatusCode = StatusCodes.Status403Forbidden };
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
