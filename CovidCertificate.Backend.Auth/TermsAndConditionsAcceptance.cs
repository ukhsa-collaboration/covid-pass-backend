using System;
using System.Net;
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

namespace CovidCertificate.Backend.Auth
{
    public class TermsAndConditionsAcceptance
    {
        private readonly IUserPreferenceService userPreferences;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;
        private readonly ILogger<TermsAndConditionsAcceptance> logger;

        public TermsAndConditionsAcceptance(IUserPreferenceService userPreferences,
            ILogger<TermsAndConditionsAcceptance> logger, 
            IEndpointAuthorizationService endpointAuthorizationService)
        {
            this.userPreferences = userPreferences;
            this.logger = logger;
            this.endpointAuthorizationService = endpointAuthorizationService;
        }

        [FunctionName("UpdateTCAcceptance")]
        [OpenApiOperation(operationId: "updateTCAcceptance", tags: new[] { "TC" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "UpdateTCAcceptance")] HttpRequest req,
            ILogger log)
        {
            try
            {
                logger.LogInformation("UpdateTCAcceptance was invoked");
                var validationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req);
                logger.LogTraceAndDebug($"validationResult: IsValid {validationResult?.IsValid}, Response is {validationResult?.Response}");
                if (endpointAuthorizationService.ResponseIsInvalid(validationResult))
                {
                    logger.LogInformation("UpdateTCAcceptance has finished");
                    return validationResult.Response;
                }

                var covidUser = new CovidPassportUser(validationResult);
                logger.LogInformation($"covidUser hash: {covidUser?.ToNhsNumberAndDobHashKey()}");

                await userPreferences.UpdateTermsAndConditionsAsync(covidUser.ToNhsNumberAndDobHashKey());

                return new OkResult();
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return new BadRequestResult();
            }
        }
    }
}
