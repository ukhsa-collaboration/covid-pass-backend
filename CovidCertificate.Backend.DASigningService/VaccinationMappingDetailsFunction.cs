using System;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.DASigningService.Interfaces;
using CovidCertificate.Backend.DASigningService.Models.Exceptions;
using CovidCertificate.Backend.DASigningService.Responses;
using CovidCertificate.Backend.Interfaces.BlobService;
using CovidCertificate.Backend.Services.Mappers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace CovidCertificate.Backend.DASigningService
{
    public class VaccinationMappingDetailsFunction
    {
        private const string VaccinationMappingApiName = "VaccinationMappingDetails";

        private readonly ILogger<VaccinationMappingDetailsFunction> logger;
        private readonly IConfiguration configuration;
        private readonly IBlobService blobService;
        private readonly IThumbprintValidator thumbprintValidator;

        private string BlobContainerName => this.configuration.GetValue<string>(VaccinationMapper.BlobContainerNameConfigKey);
        private string BlobFileName => this.configuration.GetValue<string>(VaccinationMapper.BlobFileNameConfigKey);

        public VaccinationMappingDetailsFunction(
            ILogger<VaccinationMappingDetailsFunction> log,
            IConfiguration configuration,
            IBlobService blobService,
            IThumbprintValidator thumbprintValidator)
        {
            logger = log;
            this.configuration = configuration;
            this.blobService = blobService;
            this.thumbprintValidator = thumbprintValidator;
        }

        [FunctionName(VaccinationMappingApiName)]
        [OpenApiOperation(operationId: VaccinationMappingApiName, tags: new[] { "Get vaccinations mapping details." })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetVaccinationMappingDetails(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mapping")] HttpRequest req)
        {
            var errorHandler = new ErrorHandler();

            try
            {
                thumbprintValidator.ValidateThumbprint(req, errorHandler);

                if (errorHandler.HasErrors())
                {
                    logger.LogDebug("Thumbprint Validated.");
                    return new BadRequestObjectResult(new { errorHandler.Errors });
                }

                logger.LogInformation("No errors during the validation, fetching the vaccination mapping details file.");
                var blobResult = await blobService.GetStringFromBlobAsync(BlobContainerName, BlobFileName);

                logger.LogDebug(VaccinationMappingApiName + " finished");
                return new OkObjectResult(blobResult);
            }
            catch (ThumbprintNotAllowedException ex)
            {
                logger.LogError(ex, "Thumbprint does not belong to region's allowed thumbprints.");
                return new UnauthorizedObjectResult(GetErrorResult(errorHandler));

            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                errorHandler.AddError(ErrorCode.UNEXPECTED_SYSTEM_ERROR);
                var result = new ObjectResult(GetErrorResult(errorHandler))
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
                return result;
            }
        }

        private BarcodeResults GetErrorResult(ErrorHandler errorHandler)
        {
            return new BarcodeResults
            {
                Errors = errorHandler.Errors
            };
        }
    }
}

