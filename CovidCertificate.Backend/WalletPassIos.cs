using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.ManagementInformation;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.StaticValues;
using CovidCertificate.Backend.Utils;
using Microsoft.OpenApi.Models;
using System.Net;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Models.Helpers;
using Microsoft.FeatureManagement;

namespace CovidCertificate.Backend
{
    public class WalletPassIos
    {
        private const string Route = "walletPassIos";

        private readonly IGenerateApplePass generatePass;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;
        private readonly ILogger<WalletPassIos> logger;
        private readonly IManagementInformationReportingService miReportingService;
        private readonly IFeatureManager featureManager;

        public WalletPassIos(
            IGenerateApplePass generatePass,
            IManagementInformationReportingService miReportingService,
            ILogger<WalletPassIos> logger,
            IEndpointAuthorizationService endpointAuthorizationService,
            IFeatureManager featureManager)
        {
            this.generatePass = generatePass;
            this.endpointAuthorizationService = endpointAuthorizationService;
            this.logger = logger;
            this.featureManager = featureManager;
            this.miReportingService = miReportingService;
        }

        [FunctionName(Route)]
        [OpenApiOperation(operationId: Route, tags: new[] { "Wallet" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(IActionResult), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NoContent, contentType: "text/plain", bodyType: typeof(string), Description = "The no content response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = Route)] HttpRequest req)
        {
            var odsCountry = StringUtils.UnknownCountryString;
            
            try
            {
                logger.LogInformation("walletPassIos was invoked");
                var authorisationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req);
                odsCountry = authorisationResult.UserProperties?.Country; 
                logger.LogTraceAndDebug($"authorisationResult: IsValid is {authorisationResult?.IsValid}, Response is {authorisationResult?.Response}");
                if (endpointAuthorizationService.ResponseIsInvalid(authorisationResult))
                {
                    logger.LogInformation("walletPassIos has finished");
                    return authorisationResult.Response;
                }
               
                var covidUser = new CovidPassportUser(authorisationResult);
                logger.LogInformation($"covidUser hash: {covidUser?.ToNhsNumberAndDobHashKey()}");
                var result = getQRType(req);
                int doseNumber = -1;
                
                if (result == QRType.International)
                {
                    doseNumber = getDoseNumber(req);
                }
                if (result == QRType.Domestic)
                {
                    await CheckDomesticEnabledAsync();
                }
                var country = req.Headers["Language"];

                if (!LanguageUtils.ValidCountryCode(country))
                {
                    throw new Exception("Invalid Language Selection- must be a 2 digit ISO language code");
                }
                
                Stream pkpass;
                
                try
                {
                    pkpass = await generatePass.GeneratePassAsync(covidUser, result, country, endpointAuthorizationService.GetIdToken(req), doseNumber);
                }
                catch (InvalidDataException e)
                {
                    logger.LogWarning(e, e.Message);
                    logger.LogInformation("walletPassIos has finished");
                    return new NoContentResult();
                }

                req.HttpContext.Response.Headers.Add("content-disposition", "attachment; filename=CovidStatusPass.pkpass");

                logger.LogInformation("walletPassIos has finished");

                var ageInYears = DateUtils.GetAgeInYears(covidUser.DateOfBirth);
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Success, ageInYears);

                return new FileStreamResult(pkpass, "application/vnd.apple.pkpass");
            }
            catch (NoResultsException e)
            {
                logger.LogWarning(e, e.Message);
                logger.LogInformation("walletPassIos has finished");
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureNoContent);

                return new StatusCodeResult(204);//Confirm 204 for no data to add to pass
            }
            catch (DisabledException e)
            {
                logger.LogWarning(e, e.Message);
                return new UnauthorizedObjectResult("This endpoint has been disabled");
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                logger.LogInformation("walletPassIos has finished");
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Failure);

                return new StatusCodeResult(500);
            }
        }

        private QRType getQRType(HttpRequest req)
        {
            var selected = req.Headers["QRType"];
            bool parsed = int.TryParse(selected, out var result);
            logger.LogTraceAndDebug($"QRType: {result}");
            if (!parsed)
            {
                throw new Exception("No Pass Type supplied");
            }
            if (Enum.IsDefined(typeof(QRType), result))
            {
                return (QRType)result;
            }
            throw new Exception("Invalid QRType");
        }

        private int getDoseNumber(HttpRequest req)
        {
            var selected = req.Headers["DoseNumber"];
            bool parsed = int.TryParse(selected, out var result);
            logger.LogTraceAndDebug($"Dose Number: {result}");
            if (!parsed)
            {
                throw new Exception("No Dose Number supplied");
            }
            return result;
        }

        private async Task CheckDomesticEnabledAsync()
        {
            if (!await featureManager.IsEnabledAsync(FeatureFlags.EnableDomestic))
            {
                throw new DisabledException("This endpoint has been disabled");
            }
        }
    }
}
