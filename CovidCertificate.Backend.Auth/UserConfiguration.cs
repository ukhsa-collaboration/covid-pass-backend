using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Models.DataModels;
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
using CovidCertificate.Backend.Models.Exceptions;

namespace CovidCertificate.Backend.Auth
{
    public class UserConfiguration
    {
        private readonly IUserConfigurationService userConfigurationService;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;
        private readonly ILogger<UserConfiguration> logger;

        public UserConfiguration
            (ILogger<UserConfiguration> logger,
            IUserConfigurationService userConfigurationService,
            IEndpointAuthorizationService endpointAuthorizationService)
        {
            this.userConfigurationService = userConfigurationService;
            this.logger = logger;
            this.endpointAuthorizationService = endpointAuthorizationService;
        }

        [FunctionName("UserConfiguration")]
        [OpenApiOperation(operationId: "userConfiguration", tags: new[] { "Configuration" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(Dictionary<string, object>), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> GetUserConfiguration(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest request)
        {
            try
            {
                logger.LogInformation($"[GET] UserConfiguration was invoked");
                var validationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(request);
                logger.LogTraceAndDebug($"validationResult: IsValid {validationResult?.IsValid}, Response is {validationResult?.Response}");
                if (endpointAuthorizationService.ResponseIsInvalid(validationResult))
                {
                    return validationResult.Response;
                }

                var covidUser = new CovidPassportUser(validationResult);
                logger.LogInformation($"covidUser hash: {covidUser?.ToNhsNumberAndDobHashKey()}");

                var preferencesHash = JwtTokenUtils.CalculateHashFromIdToken(endpointAuthorizationService.GetIdToken(request));
                var userConfiguration = await userConfigurationService.GetUserConfigurationAsync(covidUser, preferencesHash);

                var userInfoReturnData = new Dictionary<string, Object>()
                {
                    { "birthdate", covidUser.DateOfBirth },
                    { "email", covidUser.EmailAddress },
                    { "family_name", covidUser.FamilyName },
                    { "given_name", covidUser.GivenName },
                    { "identity_proofing_level", covidUser.IdentityProofingLevel.ToString() },
                };

                var response = new Dictionary<string, Object>()
                {
                    { "userInfo", userInfoReturnData },
                    { "userConfiguration", userConfiguration }
                };

                return new OkObjectResult(response);
            }
            catch (Exception e) when (e is BadRequestException || e is ArgumentException)
            {
                logger.LogError(e, e.Message);
                return new BadRequestObjectResult("There seems to be a problem: bad request");
            }
            catch (UnauthorizedException e)
            {
                logger.LogWarning(e, e.Message);
                return new UnauthorizedObjectResult("There seems to be a problem: unauthorized");
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                var result = new ObjectResult(e.Message);
                result.StatusCode = 500;
                return result;
            }
            finally
            {
                logger.LogInformation($"[GET] UserConfiguration has finished");
            }
        }
    }
}
