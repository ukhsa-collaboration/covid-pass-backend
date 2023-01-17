using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ValidPublicKeysFunction
    {
        private readonly INationalBackendService nationalBackendService;
        private readonly ILogger<ValidPublicKeysFunction> logger;

        public ValidPublicKeysFunction(INationalBackendService nationalBackendService,
                               ILogger<ValidPublicKeysFunction> logger)
        {
            this.nationalBackendService = nationalBackendService;
            this.logger = logger;
        }

        [FunctionName("GetValidPublicKeys")]
        [OpenApiOperation(operationId: "getValidPublicKeys", tags: new[] { "Public Keys" })]
        [OpenApiParameter(name: "keyid", In = ParameterLocation.Query, Required = true, Type = typeof(string), Summary = "Gets Valid Public Keys")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ValidPublicKeys/{keyid?}")] HttpRequest req, string keyid)
        {
            try
            {
                logger.LogInformation("GetValidPublicKeys was invoked.");
                var trustList = await nationalBackendService.GetTrustListCertificatesAsync(kid: keyid);
                logger.LogInformation("GetValidPublicKeys has finished.");
                return new OkObjectResult(trustList);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [FunctionName("GetValidPublicKeysByCountry")]
        [OpenApiOperation(operationId: "getValidPublicKeysByCountry", tags: new[] { "Public Keys" })]
        [OpenApiParameter(name: "country", In = ParameterLocation.Query, Required = true, Type = typeof(string), Summary = "Gets Public Keys By Country")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(DGCGTrustList), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> RunByCountry(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ValidPublicKeys/Country/{country}")] HttpRequest req, string country)
        {
            try
            {
                logger.LogInformation("GetValidPublicKeysByCountry was invoked.");
                var trustList = (await nationalBackendService.GetTrustListCertificatesAsync(country: country));
                logger.LogInformation("GetValidPublicKeysByCountry has finished.");
                return new OkObjectResult(trustList);
            }
            catch(Exception e)
            {
                logger.LogError(e, e.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [FunctionName("GetSubjectPublicKeyInfo")]
        [OpenApiOperation(operationId: "getSubjectPublicKeyInfo", tags: new[] { "Public Keys" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(List<TrustListSubjectPublicKeyInfoDto>), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> RunSubjectPublicKeyInfo(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route ="SubjectPublicKeyInfo")] HttpRequest req)
        {
            try
            {
                logger.LogInformation("GetSubjectPublicKeyInfo was invoked.");
                var subjectPublicKeyInfosDtos = (await nationalBackendService.GetSubjectPublicKeyInfoDtosAsync()).ToList();
                logger.LogInformation("GetSubjectPublicKeyInfo has finished.");
                return new OkObjectResult(subjectPublicKeyInfosDtos);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }        
    }
}
