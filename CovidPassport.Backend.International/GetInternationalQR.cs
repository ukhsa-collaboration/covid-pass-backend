using System;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using CovidCertificate.Backend.Interfaces.ManagementInformation;
using CovidCertificate.Backend.Models.StaticValues;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Models.ResponseDtos;

namespace CovidCertificate.Backend.International
{
    public class GetInternationalQR
    {
        private const string Route = "GetInternationalQR";

        private readonly ILogger<GetInternationalQR> logger;
        private readonly IEndpointAuthorizationService endpointAuthorizationService;
        private readonly ICovidCertificateService covidCertificateService;
        private readonly ICovidResultsService covidResultsService;
        private readonly IManagementInformationReportingService miReportingService;
        private readonly IInternationalCertificateWrapper internationalCertificateWrapper;

        public GetInternationalQR(
            ILogger<GetInternationalQR> logger,
            ICovidCertificateService covidCertificateService,
            ICovidResultsService covidResultsService,
            IManagementInformationReportingService miReportingService,
            IEndpointAuthorizationService endpointAuthorizationService,
            IInternationalCertificateWrapper internationalCertificateWrapper)
        {
            this.logger = logger;
            this.covidCertificateService = covidCertificateService;
            this.covidResultsService = covidResultsService;
            this.endpointAuthorizationService = endpointAuthorizationService;
            this.miReportingService = miReportingService;
            this.internationalCertificateWrapper = internationalCertificateWrapper;
        }

        [FunctionName(Route)]
        [OpenApiOperation(operationId: Route, tags: new[] { "QR" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(QRcodeResponse), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NoContent, contentType: "text/plain", bodyType: typeof(string), Description = "The no content response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string), Description = "The unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = Route)] HttpRequest req)
        {
            var odsCountry = StringUtils.UnknownCountryString;
            
            try
            {
                logger.LogInformation($"{nameof(GetInternationalQR)} was invoked");
                var validationResult = await endpointAuthorizationService.AuthoriseEndpointAsync(req, "NhsLogin");
                logger.LogTraceAndDebug($"validationResult: IsValid {validationResult?.IsValid}, Response is {validationResult?.Response}");

                if (endpointAuthorizationService.ResponseIsInvalid(validationResult))
                {
                    logger.LogInformation($"{nameof(GetInternationalQR)} has finished with invalid authorisation.");
                    return validationResult?.Response;
                }

                var covidUser = new CovidPassportUser(validationResult);
                logger.LogInformation($"covidUser hash: {covidUser?.ToNhsNumberAndDobHashKey()}");

                var idToken = endpointAuthorizationService.GetIdToken(req, true);
                var medicalResults = await covidResultsService.GetMedicalResultsAsync(covidUser, idToken, CertificateScenario.International, NhsdApiKey.Attended);

                var vaccinationCertificateTask = covidCertificateService.GetInternationalCertificateAsync(covidUser, idToken, CertificateType.Vaccination, medicalResults);
                var recoveryCertificateTask = covidCertificateService.GetInternationalCertificateAsync(covidUser, idToken, CertificateType.Recovery, medicalResults);

                await Task.WhenAll(vaccinationCertificateTask, recoveryCertificateTask);

                var vaccinationCertificateContainer = await vaccinationCertificateTask;
                var recoveryCertificateContainer = await recoveryCertificateTask;

                var vaccinationCertificate = vaccinationCertificateContainer?.GetSingleCertificateOrNull();
                var recoveryCertificate = recoveryCertificateContainer?.GetSingleCertificateOrNull();

                var vaccinationCertificateValid = IsCertificateValid(vaccinationCertificate, CertificateType.Vaccination);
                var recoveryCertificateValid = IsCertificateValid(recoveryCertificate, CertificateType.Recovery);

                odsCountry = validationResult?.UserProperties?.Country;

                if (!vaccinationCertificateValid &&
                    !recoveryCertificateValid)
                {
                    miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.SuccessNoCert);

                    return new StatusCodeResult(204);
                }

                var qrResponses = new InternationalQrResponse(default, default);

                if (vaccinationCertificateValid)
                {
                    qrResponses.VaccinationQrResponse = internationalCertificateWrapper.WrapVaccines(vaccinationCertificateContainer);
                }

                if (recoveryCertificateValid)
                {
                    qrResponses.RecoveryQrResponse = internationalCertificateWrapper.WrapRecovery(recoveryCertificateContainer);
                }

                logger.LogInformation($"{nameof(GetInternationalQR)} has finished");

                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Success);

                return new OkObjectResult(qrResponses);
            }
            catch (BadRequestException e)
            {
                logger.LogWarning(e, e.Message);

                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureBadRequest);

                return new BadRequestObjectResult("There seems to be a problem: bad request.");
            }
            catch (UnauthorizedException e)
            {
                logger.LogWarning(e, e.Message);

                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.FailureUnauth);

                return new UnauthorizedObjectResult(e.Message);
            }
            catch (ArgumentException e)
            {
                logger.LogError(e, e.Message);

                miReportingService.AddReportLogInformation(Route, odsCountry, MIReportingStatus.Failure);

                return new StatusCodeResult(400);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private bool IsCertificateValid(Certificate certificate, CertificateType certificateType)
        {
            if (certificate == default)
            {
                logger.LogTraceAndDebug($"No {certificateType} data found.");
                return false;
            }

            return true;
        }
    }
}

