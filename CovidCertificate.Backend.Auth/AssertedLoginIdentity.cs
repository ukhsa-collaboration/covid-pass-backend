using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
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
    public class AssertedLoginIdentity
    {
        private readonly IAssertedLoginIdentityService assertedLoginIdentityService;
        private readonly ILogger<AssertedLoginIdentity> logger;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;

        public AssertedLoginIdentity(
            IAssertedLoginIdentityService assertedLoginIdentityService,
            ILogger<AssertedLoginIdentity> logger,
            IEndpointAuthorizationService endpointAuthorizationService)
        {
            this.assertedLoginIdentityService = assertedLoginIdentityService;
            this.logger = logger;
            this.endpointAuthorizationService = endpointAuthorizationService;
        }

        [FunctionName("FetchAssertedLoginIdentity")]
        [OpenApiOperation(operationId: "fetchAssertedLoginIdentity", tags: new[] { "Login" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "FetchAssertedLoginIdentity")] HttpRequest req)
        {
            try
            {
                logger.LogInformation("FetchAssertedLoginIdentity (endpoint) was invoked");
                var validationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req);
                logger.LogTraceAndDebug($"validationResult: IsValid {validationResult?.IsValid}, Response is {validationResult?.Response}");
                if (endpointAuthorizationService.ResponseIsInvalid(validationResult))
                {
                    logger.LogInformation("FetchAssertedLoginIdentity has finished");
                    return validationResult.Response;
                }

                //get jti
                var idToken = endpointAuthorizationService.GetIdToken(req, true);
                var jwtToken = new JwtSecurityToken(idToken);
                var assertedLoginIdentity = assertedLoginIdentityService.GenerateAssertedLoginIdentity(jwtToken);
                logger.LogInformation("FetchAssertedLoginIdentity has finished");
                return new OkObjectResult(assertedLoginIdentity);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return new BadRequestResult();
            }
        }
    }
}
