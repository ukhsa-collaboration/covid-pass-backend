using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CovidCertificate.Backend.Auth.Models;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.ResponseDtos;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;

namespace CovidCertificate.Backend.Auth
{
    public class RefreshToken
    {
        private readonly INhsLoginService nhsLoginService;
        private readonly ILogger<RefreshToken> logger;

        public RefreshToken(INhsLoginService nhsLoginService, ILogger<RefreshToken> logger)
        {
            this.logger = logger;
            this.nhsLoginService = nhsLoginService;
        }

        [FunctionName("RefreshToken")]
        [OpenApiOperation(operationId: "refreshToken", tags: new[] { "Login" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(TokenRequestModel), Example = typeof(TokenRequestModelExample))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(NhsLoginTokenResponse), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "Token/Refresh")] HttpRequest request)
        {
            try
            {
                logger.LogInformation("RefreshToken was invoked");

                if (!request.Headers.TryGetValue("refreshToken", out var refreshTokenHeader))
                {
                    throw new ArgumentNullException(nameof(refreshTokenHeader), "No refresh token was specified.");
                }

                var nhsLoginToken = await nhsLoginService.GetAccessTokenAsync(refreshTokenHeader);

                // Since we do not acquire a new refresh token from NHS login. We return the same refresh token in the response.
                var nhsLoginTokenResponse = new NhsLoginTokenResponse(nhsLoginToken.AccessToken, refreshTokenHeader, nhsLoginToken.ExpiresIn, nhsLoginToken.IdToken);

                logger.LogInformation("RefreshToken has finished");

                return new OkObjectResult(nhsLoginTokenResponse);
            }
            catch (Exception e) when (e is BadRequestException || e is ArgumentNullException)
            {
                logger.LogWarning(e, e.Message);
                return new BadRequestObjectResult("There seems to be a problem: bad request");
            }
            catch (UnauthorizedException e)
            {
                logger.LogWarning(e, e.Message);
                return new UnauthorizedObjectResult("There seems to be a problem: unauthorized");
            }
            catch (HttpRequestException e)
            {
                logger.LogCritical(e, e.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
    public class RefreshTokenRequestModelExample : OpenApiExample<RefreshTokenRequestModel>
    {
        public override IOpenApiExample<RefreshTokenRequestModel> Build(NamingStrategy namingStrategy = null)
        {
            this.Examples.Add(
                OpenApiExampleResolver.Resolve(
                    "sample1",
                    new RefreshTokenRequestModel("redirectUri"),
                    namingStrategy
                ));

            return this;
        }
    }
}
