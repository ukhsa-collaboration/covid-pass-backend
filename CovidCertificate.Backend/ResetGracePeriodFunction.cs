using System;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models;
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

namespace CovidCertificate.Backend
{
    public class ResetGracePeriodFunction
    {
        private readonly IGracePeriodService gracePeriodService;
        private readonly ILogger<ResetGracePeriodFunction> logger;

        public ResetGracePeriodFunction
            (ILogger<ResetGracePeriodFunction> logger,
            IGracePeriodService gracePeriodService)
        {
            this.gracePeriodService = gracePeriodService;
            this.logger = logger;
        }

        [FunctionName("ResetGracePeriod")]
        [OpenApiOperation(operationId: "resetGracePeriod", tags: new[] { "Grace Period" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ResetGracePeriodModel), Example = typeof(ResetGracePeriodModelExample))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(GracePeriodResponse), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> ResetGracePeriod(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest request)
        {
            try
            {
                logger.LogInformation($"{nameof(ResetGracePeriod)} was invoked");

                var nhsNumberDobHash = request.Query["nhsNumberDobHash"];
                logger.LogTraceAndDebug($"nhsNumberDobHash: {nhsNumberDobHash}");

                var gracePeriod = await gracePeriodService.ResetGracePeriodAsync(nhsNumberDobHash);

                logger.LogInformation($"{nameof(ResetGracePeriod)} has finished");

                return new OkObjectResult(gracePeriod);
            }
            catch (BadRequestException e)
            {
                logger.LogError(e, e.Message);
                return new BadRequestObjectResult(e.Message);
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
    public class ResetGracePeriodModelExample : OpenApiExample<ResetGracePeriodModel>
    {
        public override IOpenApiExample<ResetGracePeriodModel> Build(NamingStrategy namingStrategy = null)
        {
            this.Examples.Add(
                OpenApiExampleResolver.Resolve(
                    "sample1",
                    new ResetGracePeriodModel("nhs-number", "DD-MM-YYYY"),
                    namingStrategy
                ));

            return this;
        }
    }
}
