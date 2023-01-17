using System;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.ResponseDtos;
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
    public class UserPreferences
    {
        private readonly IUserPreferenceService userPreferences;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;
        private readonly ILogger<UserPreferences> logger;

        public UserPreferences(IUserPreferenceService userPreferences,
            ILogger<UserPreferences> logger, 
            IEndpointAuthorizationService endpointAuthorizationService)
        {
            this.userPreferences = userPreferences;
            this.logger = logger;
            this.endpointAuthorizationService = endpointAuthorizationService;
        }

        [FunctionName("GetUserPreferences")]
        [OpenApiOperation(operationId: "getUserPreferences", tags: new[] { "Preferences" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(UserPreferenceResponse), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NoContent, contentType: "text/plain", bodyType: typeof(string), Description = "The no content response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetUserPreferences")] HttpRequest req,
            ILogger log)
        {
            try
            {
                logger.LogInformation("GetUserPreferences was invoked");
                var validationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req);
                logger.LogTraceAndDebug($"validationResult: IsValid {validationResult?.IsValid}, Response is {validationResult?.Response}");
                if (endpointAuthorizationService.ResponseIsInvalid(validationResult))
                {
                    logger.LogInformation("GetUserPreferences has finished");
                    return validationResult.Response;
                }

                var covidUser = new CovidPassportUser(validationResult);
                logger.LogInformation($"covidUser hash: {covidUser?.ToNhsNumberAndDobHashKey()}");
                
                var nhsNumberDobHash = JwtTokenUtils.CalculateHashFromIdToken(endpointAuthorizationService.GetIdToken(req));
                var userPreferenceDto = await userPreferences.GetPreferencesAsync(nhsNumberDobHash);

                return new OkObjectResult(userPreferenceDto);
            }
            catch(NoResultsException e)
            {
                logger.LogWarning(e, e.Message);
                return new NoContentResult();
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return new BadRequestResult();
            }
        }

    }
}
