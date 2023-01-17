using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi.Models;

namespace CovidCertificate.Backend
{
    public class GetDomesticEndpointStatus
    {
        private readonly ILogger<GetDomesticEndpointStatus> logger;
        private readonly IFeatureManager featureManager;

        public GetDomesticEndpointStatus( IFeatureManager featureManager,
            ILogger<GetDomesticEndpointStatus> logger)
        {
           this.featureManager = featureManager;
           this.logger = logger;
        }

        [FunctionName("GetDomesticEndpointStatus")]
        [OpenApiOperation(operationId: "getDomesticEndpointStatus", tags: new[] { "DomesticEndpointStatus" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetDomesticEndpointStatus")] HttpRequest req)
        {
            logger.LogInformation("GetDomesticEndpointStatus was invoked");
            var domesticEnabled = await featureManager.IsEnabledAsync(FeatureFlags.EnableDomestic);
            return new OkObjectResult(domesticEnabled);
        }
    }
}
