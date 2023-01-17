using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Deserializers;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.NhsApiIntegration.Interfaces;
using CovidCertificate.Backend.NhsApiIntegration.Models;
using CovidCertificate.Backend.Utils;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly.Wrap;

namespace CovidCertificate.Backend.NhsApiIntegration.Services
{
    public class MedicalExemptionApiService : IMedicalExemptionApiService
    {
        private readonly ILogger<MedicalExemptionApiService> logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly MedicalExemptionApiSettings medicalExemptionApiSettings;
        private readonly INhsTestResultsHistoryApiAccessTokenService accessTokenService;
        private readonly IMedicalExemptionDataParser medicalExemptionParser;
        private AsyncPolicyWrap<HttpResponseMessage> retryPolicy;
        private readonly IConfiguration configuration;
        private readonly IUnattendedSecurityService unattendedSecurityService;
        private readonly NhsTestResultsHistoryApiSettings nhsTestResultsHistoryApiSettings;

        public MedicalExemptionApiService(ILogger<MedicalExemptionApiService> logger,
                                          IHttpClientFactory httpClientFactory,
                                          MedicalExemptionApiSettings medicalExemptionApiSettings,
                                          RetryPolicySettings retryPolicySettings,
                                          INhsTestResultsHistoryApiAccessTokenService accessTokenService,
                                          IMedicalExemptionDataParser medicalExemptionParser,                                         
                                          IConfiguration configuration,
                                          NhsTestResultsHistoryApiSettings nhsTestResultsHistoryApiSettings,
                                          IUnattendedSecurityService unattendedSecurityService)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.accessTokenService = accessTokenService;
            this.medicalExemptionParser = medicalExemptionParser;
            this.medicalExemptionApiSettings = medicalExemptionApiSettings;
            this.configuration = configuration;
            this.unattendedSecurityService = unattendedSecurityService;
            SetupRetryPolicy(retryPolicySettings);
            this.nhsTestResultsHistoryApiSettings = nhsTestResultsHistoryApiSettings;
        }

        private void SetupRetryPolicy(RetryPolicySettings retryPolicySettings)
        {
            int retryCount = retryPolicySettings.RetryCount;
            int retrySleepDuration = retryPolicySettings.RetrySleepDurationInMilliseconds;
            int timeout = retryPolicySettings.TimeoutInMilliseconds;
            List<HttpStatusCode> statusCodes = new List<HttpStatusCode> { HttpStatusCode.TooManyRequests, HttpStatusCode.GatewayTimeout };
            retryPolicy = HttpRetryPolicyUtils.CreateRetryPolicyWrapCustomResponseCodes(retryCount, retrySleepDuration, timeout, "retrieving medical exemption from NHS Medical Exemption API", logger, statusCodes);
        }

        public async Task<IEnumerable<MedicalExemption>> GetMedicalExemptionDataAttendedAsync(string identityToken)
        {
            logger.LogInformation("Trying to get access token for attended Medical Exemption API.");

            var accessTokenPrivateKey = configuration["NhsTestResultsHistoryApiAccessTokenPrivateKey"];
            var appKid = nhsTestResultsHistoryApiSettings.NhsTestResultsHistoryApiAccessTokenAppKid;
            var appKey = nhsTestResultsHistoryApiSettings.NhsTestResultsHistoryApiAccessTokenAppKey;
            var accessTokenKey = new NHSDAccessTokenConfigs(accessTokenPrivateKey, appKid, appKey);

            var accessToken = await accessTokenService.GetAccessTokenAsync(accessTokenKey, identityToken);
            var nhsNumber = GetNhsNumberFromUserToken(identityToken);
            return await GetMedicalExemptionsAsync(accessToken, nhsNumber, identityToken);
        }

        public async Task<IEnumerable<MedicalExemption>> GetMedicalExemptionDataUnattendedAsync(string nhsNumber)
        {
            logger.LogInformation("Trying to get access token for unattended Medical Exemption API.");
            
            unattendedSecurityService.Authorize();

            var accessTokenConfigs = new NHSDAccessTokenConfigs(
                configuration["IsolationExemptionNHSDApiAccessTokenPrivateKey"], 
                configuration["IsolationExemptionNHSDApiAccessTokenAppKid"], 
                configuration["IsolationExemptionNHSDApiAccessTokenAppKey"]);

            var accessToken = await accessTokenService.GetAccessTokenAsync(accessTokenConfigs);
            return await GetMedicalExemptionsAsync(accessToken, nhsNumber, null);
        }

        private async Task<IEnumerable<MedicalExemption>> GetMedicalExemptionsAsync(string accessToken, string nhsNumber, string identityToken)
        {
            var correlationId = Guid.NewGuid().ToString();

            logger.LogInformation("Sending request to Medical Exemption API.");
            var response = await retryPolicy.ExecuteAsync(() => httpClientFactory.CreateClient().SendAsync(CreateRequest(nhsNumber, accessToken, correlationId, identityToken)));

            var responseMessage = await response?.Content?.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Success in obtaining data from Medical Exemption API. Deserializing response.");
                var bundle = FHIRDeserializer.Deserialize<Bundle>(responseMessage);

                var parsed_medicalExemptionData = medicalExemptionParser.Parse(bundle);

                return parsed_medicalExemptionData;
            }

            MedicalExemptionApiErrorHandler(response, correlationId, responseMessage);

            return null;
        }

        private HttpRequestMessage CreateRequest(string nhsNumber, string accessToken, string correlationId, string identityToken)
        {
            logger.LogInformation("Preparing request to Medical Exemption API.");
            var baseUrl = medicalExemptionApiSettings.BaseUrl;
            var system = "https://fhir.nhs.uk/Id/nhs-number";

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["patient.identifier"] = $"{system}|{nhsNumber}";
            query["questionnaire"] = @"https://fhir.nhs.uk/Questionnaire/COVIDVaccinationMedicalExemption";
            var queryString = query.ToString();
            var endpoint = medicalExemptionApiSettings.Endpoint;

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl}{endpoint}?{queryString}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            request.Headers.Add("X-Correlation-ID", correlationId);
            if (identityToken != null)
            {
                request.Headers.Add(JwtTokenUtils.IdTokenHeaderName, identityToken);
            }
            return request;
        }

        private void MedicalExemptionApiErrorHandler(HttpResponseMessage response, string correlationId, string responseMessage)
        {
            var message = $"Error during accessing Medical Exemption API. ResponseStatusCode: {response.StatusCode}, Error message: '{responseMessage}', headers: {response.Headers}, correlationId: {correlationId}.";
            logger.LogCritical(message);
            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    throw new NotFoundException(message);
                case HttpStatusCode.BadRequest:
                    throw new BadRequestException(message);
                case HttpStatusCode.Forbidden:
                    throw new ForbiddenException(message);
                case HttpStatusCode.Unauthorized:
                    throw new UnauthorizedException(message);
                default:
                    throw new Exception(message);
            }
        }

        private string GetNhsNumberFromUserToken(string nhsdUserIdentityToken)
        {
            var nhsNumberFromToken = JwtTokenUtils.GetClaim(nhsdUserIdentityToken, JwtTokenUtils.NhsNumberClaimName);

            return nhsNumberFromToken;
        }
    }
}
