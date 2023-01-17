using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Models.HttpObjectResults;
using CovidCertificate.Backend.Models.Validators;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.IsolationExemptions
{
    public class IsolationExemptionStatus
    {
        IIsolationExemptionStatusService isolationExemptionStatusService;
        private readonly IPostEndpointValidationService postEndpointValidationService;
        private readonly ILogger<IsolationExemptionStatus> logger;

        public IsolationExemptionStatus(ILogger<IsolationExemptionStatus> logger,
            IIsolationExemptionStatusService isolationExemptionStatusService, IPostEndpointValidationService postEndpointValidationService)
        {
            this.isolationExemptionStatusService = isolationExemptionStatusService;
            this.logger = logger;
            this.postEndpointValidationService = postEndpointValidationService;
        }

        [FunctionName("IsolationExemptionStatus")]
        [OpenApiOperation(operationId: "isolationExemptionStatus", tags: new[] { "Isolation Exemption" })]
        [OpenApiSecurity("Ocp-Apim-Subscription-Key", SecuritySchemeType.ApiKey, Name = "Ocp-Apim-Subscription-Key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("id-token", SecuritySchemeType.ApiKey, Name = "id-token", In = OpenApiSecurityLocationType.Header)]
        [OpenApiSecurity("authorization", SecuritySchemeType.ApiKey, Name = "authorization", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The bad request response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The internal server error response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "status")] HttpRequest request)
        {
            try
            {
                logger.LogInformation("IsolationExemptionStatus was invoked.");
            
                var validationResult = await postEndpointValidationService.ValidatePostWithFhirPayloadAsync<Patient, FhirPatientBundleValidator.FhirPatientValidator>(request);
                logger.LogTraceAndDebug($"IsolationExemptionStatus result: {validationResult}.");

                if (validationResult.GetType() != typeof(OkObjectResult))
                {
                    logger.LogInformation("Invalid IsolationExemptionStatus result. IsolationExemptionStatus has finished");

                    if (validationResult.GetType() == typeof(BadRequestObjectResult))
                    {
                        logger.LogInformation("IsolationExemptionStatus: Validation of requst body failed.");
                        return new BadRequestObjectResult(JsonConvert.SerializeObject(new
                        {
                            errorReason = "INVALID_REQUEST"
                        }));
                    }

                    return validationResult;
                }

                var effectiveDatetime = GetEffectiveDateTimeFromRequest(request);

                var user = GetUserFromValidationResult(validationResult);

                var isolationExemptionStatus =
                    await isolationExemptionStatusService.GetIsolationExemptionStatusAsync(user, effectiveDatetime, NhsdApiKey.IsolationExemption);

                logger.LogInformation("IsolationExemptionStatus has finished.");

                return new OkObjectResult(JsonConvert.SerializeObject(new
                {
                    exemptionStatus = isolationExemptionStatus.ToString()
                }));
            }
            catch (BirthdayValidationException)
            {
                logger.LogInformation("IsolationExemptionStatus: Birthdate validation failed.");
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new
                {
                    errorReason = "BIRTHDATE_VALIDATION_FAILED"
                }));
            }
            catch (APILookupException e)
            {
                logger.LogError(e, e.Message);
                return new InternalServerErrorObjectResult(JsonConvert.SerializeObject(new
                {
                    errorReason = "API_LOOKUP_FAILED"
                }));
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return new InternalServerErrorObjectResult(JsonConvert.SerializeObject(new
                {
                    errorReason = "INTERNAL_SERVER_ERROR"
                }));
            }
        }

        private CovidPassportUser GetUserFromValidationResult(IActionResult validationResult)
        {
            var resultObject = validationResult as OkObjectResult;
            var patientObject = resultObject.Value as Patient;
            var dateOfBirth = Convert.ToDateTime(patientObject.BirthDate);
            var nhsNumber = patientObject.Identifier?.FirstOrDefault()?.Value;
            var user = new CovidPassportUser(patientObject.Name?.FirstOrDefault()?.Given?.FirstOrDefault(), dateOfBirth, "", "", nhsNumber: nhsNumber);
            logger.LogInformation($"user: {user?.ToNhsNumberAndDobHashKey()}");
            return user;
        }

        private DateTime GetEffectiveDateTimeFromRequest(HttpRequest request)
        {

            DateTime effectiveDatetime;
            if (request.Query.ContainsKey("effectiveDate"))
            {
                if (!DateTime.TryParseExact(request.Query["effectiveDate"], DateUtils.EffectiveDateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out effectiveDatetime))
                {
                    logger.LogInformation("IsolationExemptionStatus: Effective date in incorrect format.");
                    effectiveDatetime = DateTime.MinValue;
                }
            }
            else
            {
                effectiveDatetime = DateTime.UtcNow;
                logger.LogInformation("Effective date not provided. Using DateTime.Now instead.");
            }
            return effectiveDatetime;
        }
    }
}
