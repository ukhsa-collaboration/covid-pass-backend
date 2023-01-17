using System;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.PKINationaBackend;
using CovidCertificate.Backend.Models.PKINationalBackend;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace CovidCertificate.Backend.PKINationalBackend
{
    public class EUValueSetsFunction
    {
        private readonly INationalBackendService nationalBackendService;
        private readonly ILogger<EUValueSetsFunction> logger;

        public EUValueSetsFunction(INationalBackendService nationalBackendService, ILogger<EUValueSetsFunction> logger)
        {
            this.nationalBackendService = nationalBackendService;
            this.logger = logger;
        }

        [OpenApiOperation(operationId: "getValueSets", tags: new[] { "EU Value Sets" })]
        [OpenApiParameter(name: "includeNonEUValues", In = ParameterLocation.Query, Required = false, Type = typeof(bool), Summary = "Include values used by the UK Verifier app that do not come from the EU Value Set API.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(EUValueSetResponse), Description = "The OK response.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent,  Description = "User has the most recent value set.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request error response.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response.")]
        [FunctionName("GetValueSets")]
        public async Task<IActionResult> RunValueSets(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ValueSets")] HttpRequest req)
        {
            try
            {
                logger.LogInformation("GetValueSets was invoked.");

                var includeNonEUValues = false;
                if (req.Query.TryGetValue("IncludeNonEUValues", out var reqIncludeNonEUValues) && !bool.TryParse(reqIncludeNonEUValues, out includeNonEUValues))
                {
                    return new BadRequestObjectResult("IncludeNonEUValues query parameter not a boolean.");
                }

                var valueSetResponse = await nationalBackendService.GetEUValueSetResponseAsync(includeNonEUValues);

                logger.LogInformation("GetValueSets has finished.");
                return new OkObjectResult(valueSetResponse);
            }
            catch (FormatException)
            {
                return new BadRequestObjectResult("LocalValueSetDate in incorrect format.");
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
