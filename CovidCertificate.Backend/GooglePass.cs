using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Threading.Tasks;
using CovidCertificate.Backend.Utils.Extensions;
using Newtonsoft.Json;
using CovidCertificate.Backend.Models.Enums;
using System;
using System.IO;
using CovidCertificate.Backend.Interfaces.ManagementInformation;
using CovidCertificate.Backend.Models.StaticValues;
using CovidCertificate.Backend.Utils;
using System.Net;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using Microsoft.FeatureManagement;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;

namespace CovidCertificate.Backend
{
    public class GooglePass
    {
        private const string Route = "WalletPassGoogle";

        private readonly IGooglePassJwt JWT;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;
        private readonly ILogger<GooglePass> logger;
        private readonly IManagementInformationReportingService miReportingService;
        private readonly IFeatureManager featureManager;

        public GooglePass(
            IGooglePassJwt googlePassJwt, 
            ILogger<GooglePass> logger,
            IManagementInformationReportingService miReportingService,
            IEndpointAuthorizationService endpointAuthorizationService,
            IFeatureManager featureManager)
        {
            this.JWT = googlePassJwt;
            this.miReportingService = miReportingService;
            this.endpointAuthorizationService = endpointAuthorizationService;
            this.logger = logger;
            this.featureManager = featureManager;
        }

        [FunctionName(Route)]
        [OpenApiOperation(operationId: Route, tags: new[] { "Wallet" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/string", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NoContent, contentType: "text/plain", bodyType: typeof(string), Description = "The bad data response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = Route)] HttpRequest req, ILogger log)
        {
            var odsCountry = StringUtils.UnknownCountryString;

            try
            {
                logger.LogInformation("WalletPassGoogle was invoked");
                var authorisationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req);
                odsCountry = authorisationResult.UserProperties?.Country; 
                logger.LogTraceAndDebug($"validationResult: IsValid {authorisationResult?.IsValid}, Response is {authorisationResult?.Response}");
                if (endpointAuthorizationService.ResponseIsInvalid(authorisationResult))
                {
                    logger.LogInformation("WalletPassGoogle has finished");
                    return authorisationResult.Response;
                }
                var covidUser = new CovidPassportUser(authorisationResult);
                var result = getQRType(req);
                int doseNumber = -1;
                if (result == QRType.International)
                {
                    doseNumber = getDoseNumber(req);
                }
                if(result == QRType.Domestic)
                {
                    await CheckDomesticEnabledAsync();
                }
                var country = req.Headers["Language"];

                if (!LanguageUtils.ValidCountryCode(country))
                {
                    throw new Exception("Invalid Language Selection- must be a 2 digit ISO language code");
                }
                string jwt;
                jwt = await JWT.GenerateJwtAsync(covidUser, result, country,doseNumber, NhsdApiKey.Attended, endpointAuthorizationService.GetIdToken(req));
                var jsonJwt = JsonConvert.SerializeObject(jwt);

                var ageInYears = DateUtils.GetAgeInYears(covidUser.DateOfBirth);

                logger.LogInformation("WalletPassGoogle has finished");
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Success, ageInYears);
                
                return new OkObjectResult(jsonJwt);
            }
            catch (InvalidDataException e)
            {
                logger.LogWarning(e, e.Message);
                var result = new ObjectResult(e.Message);
                result.StatusCode = 204;

                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureInvalid);
                logger.LogInformation("WalletPassGoogle has finished");

                return result;
            }
            catch(DisabledException e)
            {
                logger.LogWarning(e, e.Message);
                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FaliureDisabled);
                return new UnauthorizedObjectResult("This endpoint has been disabled");
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                var result = new ObjectResult(e.Message);
                result.StatusCode = 500;

                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Failure);
                logger.LogInformation("WalletPassGoogle has finished");

                return result;
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
