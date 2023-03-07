using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.DASigningService.Interfaces;
using CovidCertificate.Backend.DASigningService.Models;
using CovidCertificate.Backend.DASigningService.Models.Exceptions;
using CovidCertificate.Backend.DASigningService.Requests;
using CovidCertificate.Backend.DASigningService.Requests.Interfaces;
using CovidCertificate.Backend.DASigningService.Services.Commands;
using CovidCertificate.Backend.Interfaces.DateTimeProvider;
using CovidCertificate.Backend.Models.Deserializers;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Constants;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi.Models;
using BarcodeResults = CovidCertificate.Backend.DASigningService.Responses.BarcodeResults;

namespace CovidCertificate.Backend.DASigningService
{
    public class DevolvedAdministrationBarcodeGeneratorFunction
    {
        private const string VaccinationApiName = "Create2DVaccinationBarcode";
        private const string RecoveryApiName = "Create2DRecoveryBarcode";
        private const string DomesticApiName = "Create2DDomesticBarcode";
        private const string TestResultsApiName = "Create2DTestResultsBarcode";

        private readonly IBarcodeGenerator barcodeGenerator;
        private readonly IRegionConfigService regionConfigService;
        private readonly ILogService logService;
        private readonly IConfigurationRefresher configurationRefresher;
        private readonly IConfiguration configuration;
        private readonly IFeatureManager featureManager;
        private readonly IThumbprintValidator thumbprintValidator;
        private readonly ILogger<DevolvedAdministrationBarcodeGeneratorFunction> logger;
        private readonly IDateTimeProviderService dateTimeProviderService;

        public DevolvedAdministrationBarcodeGeneratorFunction(
            IBarcodeGenerator barcodeGenerator,
            IRegionConfigService regionConfigService,
            ILogService logService,
            ILogger<DevolvedAdministrationBarcodeGeneratorFunction> logger,
            IConfigurationRefresher configurationRefresher,
            IFeatureManager featureManager,
            IThumbprintValidator thumbprintValidator,
            IConfiguration configuration,
            IDateTimeProviderService dateTimeProviderService)
        {
            this.barcodeGenerator = barcodeGenerator;
            this.regionConfigService = regionConfigService;
            this.logService = logService;
            this.configurationRefresher = configurationRefresher;
            this.configuration = configuration;
            this.featureManager = featureManager;
            this.thumbprintValidator = thumbprintValidator;
            this.logger = logger;
            this.dateTimeProviderService = dateTimeProviderService;
        }

        [FunctionName(RecoveryApiName)]
        [OpenApiOperation(operationId: RecoveryApiName, tags: new[] { "Create Barcode" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Create2DRecoveryBarcode([HttpTrigger(AuthorizationLevel.Function, "post", Route = "recovery/2dbarcode")] HttpRequest req)
        {
            return await Create2DBarcodeAsync(req, CertificateType.Recovery, RecoveryApiName);
        }

        [FunctionName(VaccinationApiName)]
        [OpenApiOperation(operationId: VaccinationApiName, tags: new[] { "Create Barcode" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Create2DVaccinationBarcode([HttpTrigger(AuthorizationLevel.Function, "post", Route = "vaccinations/2dbarcode")] HttpRequest req)
        {
            return await Create2DBarcodeAsync(req, CertificateType.Vaccination, VaccinationApiName);
        }

        [FunctionName(DomesticApiName)]
        [OpenApiOperation(operationId: DomesticApiName, tags: new[] { "Create Barcode" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Create2DDomesticBarcode([HttpTrigger(AuthorizationLevel.Function, "post", Route = "domestic/2dbarcode")] HttpRequest req)
        {
            if (!await featureManager.IsEnabledAsync(FeatureFlags.EnableDomestic))
            {
                return new BadRequestObjectResult(new Error { Code = ((ushort)ErrorCode.ENDPOINT_DISABLED).ToString() });
            }
            return await Create2DBarcodeAsync(req, CertificateType.DomesticMandatory, DomesticApiName);
        }


        [FunctionName(TestResultsApiName)]
        public async Task<IActionResult> Create2DTestResultsBarcode([HttpTrigger(AuthorizationLevel.Function, "post", Route = "testresults/2dbarcode")] HttpRequest req)
        {
            return await Create2DBarcodeAsync(req, CertificateType.TestResult, TestResultsApiName);
        }

        private async Task<IActionResult> Create2DBarcodeAsync(HttpRequest req, CertificateType type, string apiName)
        {
            logger.LogInformation(apiName + " was invoked");
            await configurationRefresher.TryRefreshAsync();

            ErrorHandler errorHandler = new ErrorHandler();

            using StreamReader streamReader = new StreamReader(req.Body);
            string rawRequestBody = await streamReader.ReadToEndAsync();

            var request = CreateRequest(type, req, rawRequestBody);

            var regionConfig =
                regionConfigService.GetRegionConfig(request.GetRegionSubscriptionNameHeader(), errorHandler);

            try
            {

                if ("TLSMA".Equals(req.Headers["Authentication-Method"]))
                {
                    thumbprintValidator.ValidateThumbprint(req, errorHandler);
                }

                if (errorHandler.HasErrors())
                {
                    logger.LogError("Errors found after Validating Thumbprint.");

                    return new BadRequestObjectResult(new BarcodeResults { Errors = errorHandler.Errors });
                }

                (bool isValid, BadRequestObjectResult badRequestResult) = ValidateRequest(request);

                if (!isValid)
                {
                    logger.LogWarning("Request found not valid.");

                    return await GetBadRequestResultAsync(regionConfig, apiName, badRequestResult);
                }

                BarcodeResults barcodeResult = await GetBarcodeResultsAsync(type, regionConfig, rawRequestBody, request);

                logger.LogDebug(apiName + " finished.");
                return await GetOKResultAsync(regionConfig, apiName, barcodeResult);
            }
            catch (FormatException ex)
            {
                logger.LogError($"Cannot parse FHIR payload. Error message: {ex.Message}.");

                var result = new BadRequestObjectResult(new Error
                {
                    Code = ((ushort)ErrorCode.FHIR_INVALID).ToString(),
                    Message = ex.Message,
                });

                return await GetBadRequestResultAsync(regionConfig, apiName, result);
            }
            catch (ThumbprintNotAllowedException ex)
            {
                logger.LogError(ex, ex.Message);
                var result = GetErrorResult(errorHandler);

                return new UnauthorizedObjectResult(result);
            }
            catch (Exception ex)
            {
                errorHandler.AddError(ErrorCode.UNEXPECTED_SYSTEM_ERROR);

                return await GetErrorResultAsync(regionConfig, apiName, errorHandler);
            }
        }

        private ICreate2DBarcodeRequest CreateRequest(CertificateType type, HttpRequest req, string rawRequestBody)
        {
            if (type == CertificateType.DomesticMandatory)
            {
                var domesticRequest = new Create2DDomesticBarcodeRequest(configuration, dateTimeProviderService)
                {
                    RegionSubscriptionNameHeader = req.Headers[HeaderConsts.RegionSubscriptionNameHeader],
                    Body = rawRequestBody,
                    Policy = req.Query["policy"],
                    PolicyMask = req.Query["policyMask"],
                    ValidFrom = req.Query["validFrom"],
                    ValidTo = req.Query["validTo"]
                };

                return domesticRequest;
            }

            var internationalRequest = new Create2DBarcodeRequest(configuration, dateTimeProviderService)
            {
                Type = type,
                RegionSubscriptionNameHeader = req.Headers[HeaderConsts.RegionSubscriptionNameHeader],
                Body = rawRequestBody,
                ValidFrom = req.Query["validFrom"],
                ValidTo = req.Query["validTo"]
            };

            return internationalRequest;
        }

        private async Task<BarcodeResults> GetBarcodeResultsAsync(CertificateType type, RegionConfig regionConfig, string rawRequestBody, ICreate2DBarcodeRequest request)
        {
            var validFrom = DateUtils.UnixTimeSecondsToDateTime(long.Parse(request.ValidFrom));
            var validTo = DateUtils.UnixTimeSecondsToDateTime(long.Parse(request.ValidTo));

            if (type == CertificateType.DomesticMandatory)
            {
                var patient = FHIRDeserializer.Deserialize<Patient>(rawRequestBody);
                var domesticRequest = (Create2DDomesticBarcodeRequest)request;

                var domesticBarcodeCommand = new GenerateDomesticBarcodeCommand
                {
                    Patient = patient,
                    Policies = domesticRequest.Policy.Split(","),
                    PolicyMask = int.Parse(domesticRequest.PolicyMask),
                    ValidFrom = validFrom,
                    ValidTo = validTo,
                    RegionConfig = regionConfig
                };

                return await barcodeGenerator.GenerateDomesticBarcodeAsync(domesticBarcodeCommand);
            }

            var bundle = FHIRDeserializer.Deserialize<Bundle>(rawRequestBody);

            var generateInternationalBarcodeCommand = new GenerateInternationalBarcodeCommand
            {
                Bundle = bundle,
                RegionConfig = regionConfig,
                CertificateType = type,
                ValidFrom = validFrom,
                ValidTo = validTo
            };

            return await barcodeGenerator.GenerateInternationalBarcodesAsync(generateInternationalBarcodeCommand);
        }

        private async Task<IActionResult> GetOKResultAsync(RegionConfig regionConfig, string apiName, BarcodeResults barcodeResult)
        {
            await logService.LogResultAsync(logger, barcodeResult.UVCI, apiName, HttpStatusCode.OK, regionConfig);

            return new OkObjectResult(barcodeResult);
        }

        private async Task<IActionResult> GetBadRequestResultAsync(RegionConfig regionConfig, string apiName, BadRequestObjectResult result)
        {
            await logService.LogResultAsync(logger, null, apiName, HttpStatusCode.BadRequest, regionConfig);

            return result;
        }

        private async Task<IActionResult> GetErrorResultAsync(RegionConfig regionConfig, string apiName, ErrorHandler errorHandler)
        {
            var result = new ObjectResult(GetErrorResult(errorHandler))
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            };

            await logService.LogResultAsync(logger, null, apiName, HttpStatusCode.InternalServerError, regionConfig);

            return result;
        }

        private BarcodeResults GetErrorResult(ErrorHandler errorHandler)
        {
            return new BarcodeResults
            {
                Errors = errorHandler.Errors
            };
        }

        private Tuple<bool, BadRequestObjectResult> ValidateRequest(ICreate2DBarcodeRequest request)
        {
            var validationResult = request.Validate();

            if (!validationResult.IsValid)
            {
                var badRequestResult = new BadRequestObjectResult(new BarcodeResults
                {
                    Errors = validationResult.Errors.Select(e => new Error
                    {
                        Code = e.ErrorCode,
                        Message = e.ErrorMessage
                    }).ToList()
                });

                return new Tuple<bool, BadRequestObjectResult>(false, badRequestResult);
            }

            return new Tuple<bool, BadRequestObjectResult>(true, null);
        }
    }
}
