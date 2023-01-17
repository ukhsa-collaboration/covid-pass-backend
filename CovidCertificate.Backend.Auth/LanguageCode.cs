using System;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Utils;
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
    public class LanguageCode
    {
        private readonly IUserPreferenceService userPreferences;
        private readonly ILogger<LanguageCode> logger;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;

        public LanguageCode(IUserPreferenceService userPreferences,
            ILogger<LanguageCode> logger, 
            IEndpointAuthorizationService endpointAuthorizationService)
        {
            this.userPreferences = userPreferences;
            this.logger = logger;
            this.endpointAuthorizationService = endpointAuthorizationService;
        }

        [FunctionName("UpdateLanguageCode")]
        [OpenApiOperation(operationId: "updateLanguageCode", tags: new[] { "Language" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "UpdateLanguageCode")] HttpRequest req,
            ILogger log)
        {
            try
            {
                logger.LogInformation("UpdateLanguageCode was invoked");
                var validationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req);
                logger.LogTraceAndDebug($"validationResult: IsValid {validationResult?.IsValid}, Response is {validationResult?.Response}");
                if (endpointAuthorizationService.ResponseIsInvalid(validationResult))
                {
                    logger.LogInformation("UpdateLanguageCode has finished");
                    return validationResult.Response;
                }


                var languageCode = req.Headers["LanguageCode"];
                var nhsID = JwtTokenUtils.CalculateHashFromIdToken(endpointAuthorizationService.GetIdToken(req));
                await userPreferences.UpdateLanguageCodeAsync(nhsID, languageCode);
                return new OkResult();
            }
            catch(Exception e)
            {
                logger.LogWarning(e, e.Message);
                return new BadRequestResult();
            }
        }
    }
}
