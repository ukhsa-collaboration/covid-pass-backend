using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.NhsApiIntegration.Interfaces;
using CovidCertificate.Backend.NhsApiIntegration.Responses;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Polly.Wrap;

namespace CovidCertificate.Backend.NhsApiIntegration.Services
{
    public class NhsTestResultsHistoryApiAccessTokenService : INhsTestResultsHistoryApiAccessTokenService
    {
        private readonly ILogger<NhsTestResultsHistoryApiAccessTokenService> logger;
        private readonly IConfiguration configuration;
        private readonly NhsTestResultsHistoryApiSettings settings;
        private readonly HttpClient httpClient;

        private AsyncPolicyWrap<HttpResponseMessage> getAccessTokenRetryPolicy;


        public NhsTestResultsHistoryApiAccessTokenService(
            ILogger<NhsTestResultsHistoryApiAccessTokenService> logger,
            IConfiguration configuration,
            NhsTestResultsHistoryApiSettings settings,
            IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.settings = settings;
            this.httpClient = httpClientFactory.CreateClient();
            SetupRetryPolicy();
        }

        private void SetupRetryPolicy()
        {
            List<HttpStatusCode> statusCodes = new List<HttpStatusCode> { HttpStatusCode.InternalServerError, HttpStatusCode.TooManyRequests };
            this.getAccessTokenRetryPolicy = HttpRetryPolicyUtils.CreateRetryPolicyWrapCustomResponseCodes(settings.AccessTokenRetryCount,
                settings.AccessTokenRetrySleepDurationInMilliseconds,
                settings.AccessTokenTimeoutInMilliseconds,
                " acquiring access token ",
                logger, statusCodes);
        }

        public async Task<string> GetAccessTokenAsync(NHSDAccessTokenConfigs accessTokenKey, string identityToken = default)
        {
            var accessTokenResult = await GetAccessTokenFromNhsAsync(identityToken, accessTokenKey);

            if (accessTokenResult.isSuccessStatusCode)
            {
                var deserializedAccessToken = JsonConvert.DeserializeObject<TokenResponse>(accessTokenResult.responseString);
                return deserializedAccessToken.AccessToken;
            }

            var message = $"Error during obtaining access token. Error message: '{accessTokenResult.responseString}'.";
            logger.LogCritical(message);

            throw new Exception(message);
        }

        private async Task<(bool isSuccessStatusCode, string responseString)> GetAccessTokenFromNhsAsync(string identityToken, NHSDAccessTokenConfigs accessTokenKey)
        {
            logger.LogTraceAndDebug($"{nameof(GetAccessTokenFromNhsAsync)} was invoked.");

            string nhsAccessTokenEndpoint = settings.NhsTestResultsHistoryApiAccessTokenBaseUrl + "/oauth2/token";

            var response = await getAccessTokenRetryPolicy.ExecuteAsync(() => SendRequest(identityToken, nhsAccessTokenEndpoint, accessTokenKey));

            var responseMessage = await response.Content.ReadAsStringAsync();

            logger.LogTraceAndDebug($"{nameof(GetAccessTokenFromNhsAsync)} finished.");

            return (response.IsSuccessStatusCode, responseMessage);
        }

        private Task<HttpResponseMessage> SendRequest(string identityToken, string nhsAccessTokenEndpoint, NHSDAccessTokenConfigs accessTokenKey)
        {
            return httpClient.SendAsync(CreateAccessTokenRequest(nhsAccessTokenEndpoint, identityToken, accessTokenKey));
        }

        private HttpRequestMessage CreateAccessTokenRequest(string nhsAccessTokenEndpoint, string identityToken, NHSDAccessTokenConfigs accessTokenKey)
        {
            string token = GenerateJwtToken(nhsAccessTokenEndpoint, accessTokenKey);

            var content = new Dictionary<string, string>()
            {
                { "grant_type", "client_credentials" },
                { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                { "client_assertion", token },
            };

            if (!string.IsNullOrEmpty(identityToken))
            {
                content.Add("subject_token", identityToken);
                content.Add("subject_token_type", "urn:ietf:params:oauth:token-type:id_token");
                content["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange";
            }

            var request = new HttpRequestMessage(HttpMethod.Post, nhsAccessTokenEndpoint)
            {
                Content = new FormUrlEncodedContent(content)
            };

            if (settings.UseTestResultsHistoryMock)
            {
                // This header is only needed for Mock on Azure
                request.Headers.Add("x-functions-key", settings.AuthMockApiKey);
            }

            return request;
        }

        private string GenerateJwtToken(string nhsAccessTokenEndpoint, NHSDAccessTokenConfigs accessTokenKey)
        {
            logger.LogInformation("Creating JWT to obtain access token for Test Results History API for application.");

            using RSA rsaPrivateKey = RSA.Create();
            rsaPrivateKey.ImportRSAPrivateKey(accessTokenKey.PrivateKey.FromBase64StringToByteArray(), out _);

            

            var jwtHeader = new JwtHeader(
                signingCredentials: new SigningCredentials(new RsaSecurityKey(rsaPrivateKey),
                    SecurityAlgorithms.RsaSha512)
                {
                    CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
                });
            jwtHeader.Add("kid", accessTokenKey.AppKid);

            var jwtPayload = new JwtPayload
            {
                {"iss", accessTokenKey.AppKey},
                {"sub", accessTokenKey.AppKey},
                {"aud", nhsAccessTokenEndpoint},
                {"jti", Guid.NewGuid().ToString()},
                {"exp", new DateTimeOffset(DateTime.Now.AddMinutes(4)).ToUnixTimeSeconds()},
            };

            var jwt = new JwtSecurityToken(jwtHeader, jwtPayload);
            var token = new JwtSecurityTokenHandler().WriteToken(jwt);

            return token;
        }
    }
}
