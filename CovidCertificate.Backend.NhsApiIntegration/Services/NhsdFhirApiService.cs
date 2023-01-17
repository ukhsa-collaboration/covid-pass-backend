using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading.Tasks;
using System.Web;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.NhsApiIntegration.Interfaces;
using CovidCertificate.Backend.Utils;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Polly.Wrap;
using Microsoft.Extensions.Configuration;
using CovidCertificate.Backend.Interfaces;
using System.Linq;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Utils.Extensions;
using Hl7.Fhir.ElementModel;
using CovidCertificate.Backend.Models.Deserializers;

namespace CovidCertificate.Backend.NhsApiIntegration.Services
{
    public class NhsdFhirApiService : INhsdFhirApiService
    {
        private readonly ILogger<NhsdFhirApiService> logger;
        private readonly HttpClient httpClient;
        private readonly NhsTestResultsHistoryApiSettings settings;
        private readonly INhsTestResultsHistoryApiAccessTokenService nhsTestResultsHistoryApiAccessTokenService;
        private readonly IProofingLevelValidatorService proofingLevelValidatorService;
        private AsyncPolicyWrap<HttpResponseMessage> retryPolicy;
        private readonly IConfiguration configuration;

        public NhsdFhirApiService(
            ILogger<NhsdFhirApiService> logger,
            IHttpClientFactory httpClientFactory,
            NhsTestResultsHistoryApiSettings settings,
            INhsTestResultsHistoryApiAccessTokenService nhsTestResultsHistoryApiAccessTokenService,
            IProofingLevelValidatorService proofingLevelValidatorService,
            IConfiguration configuration)
        {
            this.logger = logger;
            this.httpClient = httpClientFactory.CreateClient();
            this.settings = settings;
            this.nhsTestResultsHistoryApiAccessTokenService = nhsTestResultsHistoryApiAccessTokenService;
            this.proofingLevelValidatorService = proofingLevelValidatorService;
            SetupRetryPolicy();
            this.configuration = configuration;
        }

        public async Task<Bundle> GetUnattendedVaccinesBundleAsync(CovidPassportUser user, string apiKey)
        {
            logger.LogInformation("Trying to get access token for Immunisation History API.");

            var accessToken = await PrepareAndGetAccessTokenAsync(apiKey);

            var response = await SendVaccinationHistoryAPIRequestAsync(accessToken, user.NhsNumber);
            var responseMessage = await response.Content.ReadAsStringAsync() ?? "";

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Success in obtaining data from Immunisation History API. Deserializing response.");
                return TryDeserializeVaccinationHistoryResponseMessage(responseMessage);
            }

            throw new VaccinationApiException($"Error in NHS Immunization history API, response message: {responseMessage}");
        }

        public async Task<Bundle> GetDiagnosticTestResultsAsync(string identityToken, string apiKey)
            => await GetTestResultsAsync(new[] { settings.AntigenTestSNOMEDCode, settings.VirusTestSNOMEDCode }, identityToken, apiKey);

        public async Task<Bundle> GetUnattendedDiagnosticTestResultsAsync(CovidPassportUser covidUser, string apiKey)
          => await GetUnattendedTestResultsAsync(covidUser, new[] { settings.AntigenTestSNOMEDCode, settings.VirusTestSNOMEDCode }, apiKey);

        public async Task<Bundle> GetAttendedVaccinesBundleAsync(string identityToken, string apiKey)
        {
            var accessToken = await PrepareAndGetAccessTokenAsync(apiKey, identityToken);

            var response = await SendVaccinationHistoryAPIRequestAsync(accessToken, GetNhsNumberFromUserToken(identityToken));
            var responseMessage = await response.Content.ReadAsStringAsync() ?? "";

            if (response.IsSuccessStatusCode)
            {
                logger.LogTraceAndDebug("Success in obtaining data from Vaccination History API. Deserializing response.");
                return TryDeserializeVaccinationHistoryResponseMessage(responseMessage);
            }

            if (response.StatusCode.Equals(HttpStatusCode.Unauthorized))
            {
                throw new TokenExpiredException($"Expired ID token, response message: {responseMessage}");
            }
            throw new VaccinationApiException($"Error in NHS Immunization history API, response message: {responseMessage}");
        }

        private void SetupRetryPolicy()
        {
            List<HttpStatusCode> statusCodes = new List<HttpStatusCode> { HttpStatusCode.InternalServerError, HttpStatusCode.TooManyRequests };
            this.retryPolicy = HttpRetryPolicyUtils.CreateRetryPolicyWrapCustomResponseCodes(settings.RetryCount,
                settings.RetrySleepDurationInMilliseconds,
                settings.TimeoutInMilliseconds,
                "retrieving Test results from NHS Test results API",
                logger, statusCodes);
        }

        private async Task<string> PrepareAndGetAccessTokenAsync(string apiKey, string idToken = null)
        {
            logger.LogInformation("Trying to get access token for Vaccination History API.");

            var accessTokenConfig = GetAccessTokenConfig(apiKey);
            return await nhsTestResultsHistoryApiAccessTokenService.GetAccessTokenAsync(accessTokenConfig, idToken);
        }

        private async Task<HttpResponseMessage> SendVaccinationHistoryAPIRequestAsync(string accessToken, string NhsNumber)
        {
            logger.LogInformation("Preparing request to Immunisation History API.");
            var baseUrl = settings.NhsTestResultsHistoryApiBaseUrl;
            var system = "https://fhir.nhs.uk/Id/nhs-number";

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["patient.identifier"] = $"{system}|{NhsNumber}";
            query["procedure-code:below"] = "90640007";
            query["_include"] = "Immunization:patient";
            var queryString = query.ToString();
            var endpoint = "/immunisation-history/FHIR/R4/Immunization";
            var correlationId = Guid.NewGuid().ToString();

            logger.LogInformation("Sending request to Immunisation History API.");
            var response = await retryPolicy.ExecuteAsync(() => httpClient.SendAsync(CreateRequest(null, baseUrl, endpoint, queryString, accessToken, correlationId)));
            var responseMessage = await response.Content.ReadAsStringAsync() ?? "";

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            var headers = response.Headers.ToString();
            var message = $"Error during accessing Vaccination History API. Error message: '{responseMessage}', headers: {headers}, url: {endpoint}?{queryString}, correlationId: {correlationId}.";
            logger.LogCritical(message);
            return response;
        }

        private Bundle TryDeserializeTestResultsResponseMessage(string responseMessage)
        {
            try
            {
                var bundle = FHIRDeserializer.Deserialize<Bundle>(responseMessage);
                RemoveSensitiveDataFromTheTestsBundle(bundle);

                return bundle;
            }
            catch (StructuralTypeException e)
            {
                throw new DiagnosticTestMappingException($"{nameof(TryDeserializeTestResultsResponseMessage)}: Bundle Mapping Exception {e.Message}");
            }
        }

        private Bundle TryDeserializeVaccinationHistoryResponseMessage(string responseMessage)
        {
            try
            {
                return FHIRDeserializer.Deserialize<Bundle>(responseMessage);
            }
            catch (StructuralTypeException e)
            {
                throw new VaccineMappingException($"{nameof(TryDeserializeVaccinationHistoryResponseMessage)}: Bundle Mapping Exception {e.Message}");
            }
        }

        private async Task<Bundle> GetUnattendedTestResultsAsync(CovidPassportUser covidUser, string[] testSNOMEDcodes, string apiKey)
        {
            var accessToken = await PrepareAndGetAccessTokenAsync(apiKey);
            (var request, var correlationId) = CreateRequest(covidUser, testSNOMEDcodes, accessToken);
            var response = await SendRequestAsync(request);

            var responseMessage = response.Content != null ? await response.Content.ReadAsStringAsync() : "";
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Success in obtaining data from Test Results History API. Deserializing response.");
                var bundle = FHIRDeserializer.Deserialize<Bundle>(responseMessage);
                RemoveSensitiveDataFromTheTestsBundle(bundle);

                return bundle;
            }

            var message = $"Error during accessing Unattended Test Results History API. ResponseStatusCode: {response.StatusCode}, Error message: '{responseMessage}', headers: {response.Headers}, correlationId: {correlationId}.";
            logger.LogCritical(message);

            throw new TestResultApiException(message);
        }

        private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
        {
            logger.LogInformation("Sending request to Test Results History API.");
            return await retryPolicy.ExecuteAsync(() => httpClient.SendAsync(request));
        }

        private (HttpRequestMessage request, string correlationId) CreateRequest(CovidPassportUser covidUser, string[] testSNOMEDcodes, string accessToken)
        {
            logger.LogInformation("Preparing request to Test Results History API.");
            var baseUrl = settings.NhsTestResultsHistoryApiBaseUrl;
            var system = "https://fhir.nhs.uk/Id/nhs-number";

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["patient.identifier"] = $"{system}|{covidUser.NhsNumber}";
            query["code"] = string.Join(',', testSNOMEDcodes);
            var queryString = query.ToString();
            var endpoint = settings.NhsTestResultsHistoryApiEndpoint;
            var correlationId = Guid.NewGuid().ToString();
            var request = CreateRequest(null, baseUrl, endpoint, queryString, accessToken, correlationId);

            return (request, correlationId);
        }

        private async Task<Bundle> GetTestResultsAsync(string[] testSNOMEDCodes, string identityToken, string apiKey)
        {
            var identityProofingLevel = proofingLevelValidatorService.GetProofingLevel(identityToken);

            VerifyProofingLevel(identityProofingLevel);

            logger.LogInformation("Trying to get access token for Test Results History API.");

            var accessToken = await PrepareAndGetAccessTokenAsync(apiKey, identityToken);

            logger.LogInformation("Get NhsNumber from IdToken to make call to Test Results History API.");
            var nhsNumber = GetNhsNumberFromUserToken(identityToken);

            logger.LogInformation("Preparing request to Test Results History API.");
            var baseUrl = settings.NhsTestResultsHistoryApiBaseUrl;
            var system = "https://fhir.nhs.uk/Id/nhs-number";

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["patient.identifier"] = $"{system}|{nhsNumber}";
            query["code"] = string.Join(',', testSNOMEDCodes);
            var queryString = query.ToString();
            var endpoint = settings.NhsTestResultsHistoryApiEndpoint;
            var correlationId = Guid.NewGuid().ToString();

            logger.LogInformation("Sending request to Test Results History API.");
            var response = await retryPolicy.ExecuteAsync(() => httpClient.SendAsync(CreateRequest(identityToken, baseUrl, endpoint, queryString, accessToken, correlationId)));

            var responseMessage = response.Content != null ? await response.Content.ReadAsStringAsync() : "";

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Success in obtaining data from Test Results History API. Deserializing response.");
                return TryDeserializeTestResultsResponseMessage(responseMessage);
            }

            var message = $"Error during accessing Test Results History API. ResponseStatusCode: {response.StatusCode}, Error message: '{responseMessage}', headers: {response.Headers}, correlationId: {correlationId}.";
            logger.LogCritical(message);

            throw new APILookupException(message);
        }

        private void VerifyProofingLevel(IdentityProofingLevel identityProofingLevel)
        {
            if (settings.DisableP5 && IsP5ProofingLevel(identityProofingLevel))
            {
                logger.LogWarning("P5 proofing level detected but is disabled.");

                throw new SecurityException(
                    "Users with P5 proofing level are not allowed to access Test Results History API.");
            }

            if (settings.DisableP5Plus && IsP5PlusProofingLevel(identityProofingLevel))
            {
                logger.LogWarning("P5Plus proofing level detected but is disabled.");

                throw new SecurityException(
                    "Users with P5Plus proofing level are not allowed to access Test Results History API.");
            }

            if (settings.DisableP9 && IsP9ProofingLevel(identityProofingLevel))
            {
                logger.LogWarning("P9 proofing level detected but is disabled.");

                throw new SecurityException(
                    "Users with P9 proofing level are not allowed to access Test Results History API.");
            }

            if (!settings.AllowAllOtherThanP5AndP5PlusAndP9 && !IsP9OrP5orP5PlusProofingLevel(identityProofingLevel))
            {
                logger.LogWarning(
                    $"Proofing level '{identityProofingLevel}' detected, but profiles other than P5 P5Plus and P9 are not allowed.");

                throw new SecurityException(
                    $"Users with '{identityProofingLevel}' proofing level are not allowed to access Test Results History API.");
            }
        }

        private HttpRequestMessage CreateRequest(string nhsdUserIdentityToken, string baseUrl, string endpoint, string queryString,
            string accessToken, string correlatioId)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl}{endpoint}?{queryString}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Id token header
            if (nhsdUserIdentityToken != null)
            {
                request.Headers.Add(JwtTokenUtils.IdTokenHeaderName, nhsdUserIdentityToken);
            }
            request.Headers.Add("X-Correlation-ID", correlatioId);

            if (settings.UseTestResultsHistoryMock)
            {
                // This header is only needed for Mock on Azure
                request.Headers.Add("x-functions-key", settings.TestResultsHistoryMockApiKey);
            }

            return request;
        }

        private string GetNhsNumberFromUserToken(string nhsdUserIdentityToken)
        {
            var nhsNumberFromToken = JwtTokenUtils.GetClaim(nhsdUserIdentityToken, JwtTokenUtils.NhsNumberClaimName);

            return nhsNumberFromToken;
        }

        private void RemoveSensitiveDataFromTheTestsBundle(Bundle bundle)
        {
            foreach (var entry in bundle.Entry)
            {
                if (entry?.Resource is Observation observation)
                {
                    var nhsId = observation?.Subject?.Identifier;
                    if (nhsId != null)
                        nhsId.Value = "";
                }
            }
        }

        private bool IsP5ProofingLevel(IdentityProofingLevel identityProofingLevel)
            => identityProofingLevel == IdentityProofingLevel.P5;

        private bool IsP5PlusProofingLevel(IdentityProofingLevel identityProofingLevel)
            => identityProofingLevel == IdentityProofingLevel.P5Plus;

        private bool IsP9ProofingLevel(IdentityProofingLevel identityProofingLevel)
            => identityProofingLevel == IdentityProofingLevel.P9;

        private bool IsP9OrP5orP5PlusProofingLevel(IdentityProofingLevel identityProofingLevel)
            => IsP5ProofingLevel(identityProofingLevel) || IsP9ProofingLevel(identityProofingLevel) || IsP5PlusProofingLevel(identityProofingLevel);

        private NHSDAccessTokenConfigs GetAccessTokenConfig(string apiKey)
        {
            return apiKey switch
            {
                NhsdApiKey.Attended => new NHSDAccessTokenConfigs(
                        configuration["NhsTestResultsHistoryApiAccessTokenPrivateKey"],
                        settings.NhsTestResultsHistoryApiAccessTokenAppKid,
                        settings.NhsTestResultsHistoryApiAccessTokenAppKey
                    ),
                NhsdApiKey.Unattended => new NHSDAccessTokenConfigs(
                        configuration["UnattendedNHSDApiAccessTokenPrivateKey"],
                        configuration["UnattendedNHSDApiAccessTokenAppKid"],
                        configuration["UnattendedNHSDApiAccessTokenAppKey"]
                    ),
                NhsdApiKey.IsolationExemption => new NHSDAccessTokenConfigs(
                        configuration["IsolationExemptionNHSDApiAccessTokenPrivateKey"],
                        configuration["IsolationExemptionNHSDApiAccessTokenAppKid"],
                        configuration["IsolationExemptionNHSDApiAccessTokenAppKey"]
                    ),
                _ => throw new ArgumentException($"API key {apiKey}, is not known")
            };
        }
    }
}
