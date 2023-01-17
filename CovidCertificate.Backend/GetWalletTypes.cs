using System;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace CovidCertificate.Backend
{
    public class GetWalletTypes
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<GetWalletTypes> logger;

        public GetWalletTypes( IConfiguration configuration,
            ILogger<GetWalletTypes> logger)
        {
           this.configuration = configuration;
           this.logger = logger;
        }

        [FunctionName("GetWalletTypes")]
        [OpenApiOperation(operationId: "getWalletTypes", tags: new[] { "Wallet" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetWalletTypes")] HttpRequest req)
        {
            try
            {
                logger.LogInformation("GetWalletTypes was invoked");
                string AllowedCertTypes;
                var device = req.Headers["Device"];

                if (device.Equals("Apple"))
                {
                    AllowedCertTypes = configuration["AllowedAppleCertTypes"];
                }
                else if (device.Equals("Google"))
                {
                    AllowedCertTypes = configuration["AllowedGoogleCertTypes"];
                }
                else
                {
                    throw new Exception("Unknown device type");
                }

                return new OkObjectResult(await Task.FromResult(AllowedCertTypes));
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
        }
    }
}
