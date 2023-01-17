using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Net;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.DASigningService.Interfaces;
using CovidCertificate.Backend.DASigningService.Requests;
using CovidCertificate.Backend.DASigningService.Models;
using CovidCertificate.Backend.DASigningService.Requests.Interfaces;
using CovidCertificate.Backend.DASigningService.Services.Commands;
using Hl7.Fhir.Model;
using CovidCertificate.Backend.Models.Deserializers;
using BarcodeResults = CovidCertificate.Backend.DASigningService.Responses.BarcodeResults;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Utils;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using CovidCertificate.Backend.DASigningService.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;

namespace CovidCertificate.Backend.DASigningService
{
    public class DevolvedAdministrationBarcodeGeneratorFunction
    {
        private const string VaccinationApiName = "Create2DVaccinationBarcode";
        private const string RecoveryApiName = "Create2DRecoveryBarcode";
        private const string DomesticApiName = "Create2DDomesticBarcode";
        private const string TestResultsApiName = "Create2DTestResultsBarcode";
        public const string RegionSubscriptionNameHeader = "Region-Subscription-Name";

        private readonly IBarcodeGenerator barcodeGenerator;
        private readonly IRegionConfigService regionConfigService;
        private readonly ILogService logService;
        private readonly IConfigurationRefresher configurationRefresher;
        private readonly IConfiguration configuration;
        private readonly IFeatureManager featureManager;
        private readonly IClientCertificateValidator clientCertificateValidator;
        private readonly ILogger<DevolvedAdministrationBarcodeGeneratorFunction> logger;

        public DevolvedAdministrationBarcodeGeneratorFunction(
            IBarcodeGenerator barcodeGenerator,
            IRegionConfigService regionConfigService,
            ILogService logService,
            ILogger<DevolvedAdministrationBarcodeGeneratorFunction> logger,
            IConfigurationRefresher configurationRefresher,
            IFeatureManager featureManager,
            IClientCertificateValidator clientCertificateValidator,
            IConfiguration configuration)
        {
            this.barcodeGenerator = barcodeGenerator;
            this.regionConfigService = regionConfigService;
            this.logService = logService;
            this.configurationRefresher = configurationRefresher;
            this.configuration = configuration;
            this.featureManager = featureManager;
            this.clientCertificateValidator = clientCertificateValidator;
            this.logger = logger;
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
                    clientCertificateValidator.ValidateCertificate(req, errorHandler, regionConfig);
                }

                if (errorHandler.HasErrors())
                {
                    return new BadRequestObjectResult(new BarcodeResults {Errors = errorHandler.Errors});
                }

                (bool isValid, BadRequestObjectResult badRequestResult) = ValidateRequest(request);

                if (!isValid)
                {
                    return await GetBadRequestResultAsync(regionConfig, apiName, badRequestResult);
                }

                BarcodeResults barcodeResult = await GetBarcodeResultsAsync(type, regionConfig, rawRequestBody, request);

                logger.LogDebug(apiName + " finished");
                return await GetOKResultAsync(regionConfig, apiName, barcodeResult);
            }
            catch (FormatException ex)
            {
                logger.LogError(ex, "Cannot parse FHIR payload.");

                var result = new BadRequestObjectResult(new Error
                {
                    Code = ((ushort)ErrorCode.FHIR_INVALID).ToString(), Message = ex.Message,
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
                logger.LogError(ex, ex.Message);
                errorHandler.AddError(ErrorCode.UNEXPECTED_SYSTEM_ERROR);

                return await GetErrorResultAsync(regionConfig, apiName, errorHandler);
            }
        }

        private ICreate2DBarcodeRequest CreateRequest(CertificateType type, HttpRequest req, string rawRequestBody)
        {
            ICreate2DBarcodeRequest request;
            if (type == CertificateType.DomesticMandatory)
            {
                var domesticRequest = new Create2DDomesticBarcodeRequest(configuration);

                domesticRequest.RegionSubscriptionNameHeader = req.Headers[RegionSubscriptionNameHeader];
                domesticRequest.Body = rawRequestBody;
                domesticRequest.Policy = req.Query["policy"];
                domesticRequest.PolicyMask = req.Query["policyMask"];
                domesticRequest.ValidFrom = req.Query["validFrom"];
                domesticRequest.ValidTo = req.Query["validTo"];

                request = domesticRequest;
            } else
            {
                var internationalRequest = new Create2DBarcodeRequest(configuration);

                internationalRequest.Type = type;
                internationalRequest.RegionSubscriptionNameHeader = req.Headers[RegionSubscriptionNameHeader];
                internationalRequest.Body = rawRequestBody;
                internationalRequest.ValidFrom = req.Query["validFrom"];
                internationalRequest.ValidTo = req.Query["validTo"];

                request = internationalRequest;
            }
            return request;
        }

        private async Task<BarcodeResults> GetBarcodeResultsAsync(CertificateType type, RegionConfig regionConfig, string rawRequestBody, ICreate2DBarcodeRequest request)
        {
            var validFrom = DateUtils.UnixTimeSecondsToDateTime(long.Parse(request.ValidFrom));
            var validTo = DateUtils.UnixTimeSecondsToDateTime(long.Parse(request.ValidTo));

            if (type == CertificateType.DomesticMandatory)
            {
                var patient = FHIRDeserializer.Deserialize<Patient>(rawRequestBody);
                var domesticRequest = (Create2DDomesticBarcodeRequest)request;

                var command = new GenerateDomesticBarcodeCommand
                {
                    Patient = patient,
                    Policies = domesticRequest.Policy.Split(","),
                    PolicyMask = int.Parse(domesticRequest.PolicyMask),
                    ValidFrom = validFrom,
                    ValidTo = validTo,
                    RegionConfig = regionConfig
                };

                return await barcodeGenerator.GenerateDomesticBarcodeAsync(command);
            }
            else
            {
                var bundle = FHIRDeserializer.Deserialize<Bundle>(rawRequestBody);

                var command = new GenerateInternationalBarcodeCommand
                {
                    Bundle = bundle,
                    RegionConfig = regionConfig,
                    CertificateType = type,
                    ValidFrom = validFrom,
                    ValidTo = validTo
                };

                return await barcodeGenerator.GenerateInternationalBarcodesAsync(command);
            }
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
