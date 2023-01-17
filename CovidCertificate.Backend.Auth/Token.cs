using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.ResponseDtos;
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
    public class Token
    {
        private readonly ILogger<Token> logger;
        private readonly INhsLoginService nhsLoginService;

        public Token(INhsLoginService nhsLoginService, ILogger<Token> logger)
        {
            this.logger = logger;
            this.nhsLoginService = nhsLoginService;
        }

        [FunctionName("Token")]
        [OpenApiOperation(operationId: "token", tags: new[] { "Login" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(NhsLoginTokenResponse), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "Token")] HttpRequest request)
        {
            try
            {
                logger.LogInformation("Token was invoked");

                if (!request.Query.TryGetValue("code", out var queryCode))
                {
                    throw new ArgumentNullException(nameof(queryCode), "No authorization code was specified.");
                }

                if (!request.Query.TryGetValue("redirectUri", out var queryRedirectUri))
                {
                    throw new ArgumentNullException(nameof(queryCode), "No redirect uri was specified.");
                }

                var nhsLoginToken = await nhsLoginService.GetAccessTokenAsync(queryCode, queryRedirectUri);

                var nhsLoginTokenResponse = new NhsLoginTokenResponse(nhsLoginToken);

                logger.LogInformation("Token has finished");
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
}
