using System;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.PKINationaBackend;
using CovidCertificate.Backend.Models.PKINationalBackend.DomesticPolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace CovidCertificate.Backend.PKINationalBackend
{
    public class DomesticPolicyFunction
    {
        private readonly INationalBackendService nationalBackendService;
        private readonly ILogger<DomesticPolicyFunction> logger;

        public DomesticPolicyFunction(INationalBackendService nationalBackendService, ILogger<DomesticPolicyFunction> logger)
        {
            this.nationalBackendService = nationalBackendService;
            this.logger = logger;
        }

        [OpenApiOperation(operationId: "getPolicy", tags: new[] { "Domestic Policy" })]
        [OpenApiParameter(name: "lastUpdated", In = ParameterLocation.Query, Required = true, Type = typeof(string), Summary = "User supplies last time they updated their policy.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(DomesticPolicyInformation), Description = "The OK response.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Description = "User has the most recent policy.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request error response.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response.")]
        [FunctionName("GetPolicy")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "policy/GB-ENG")] HttpRequest req)
        {
            try
            {
                logger.LogInformation("GetPolicy was invoked.");

                if (!req.Query.TryGetValue("LastUpdated", out var reqLastUpdated))
                {
                    return new BadRequestObjectResult("Please supply LastUpdated as a parameter.");
                }
                var lastUpdated = DateTime.Parse(reqLastUpdated);

                var policy = await nationalBackendService.GetDomesticPolicyInformationAsync(lastUpdated);

                logger.LogInformation("GetPolicy has finished.");

                return new OkObjectResult(policy);
            }
            catch (FormatException)
            {
                return new BadRequestObjectResult("LastUpdated in incorrect format");
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
