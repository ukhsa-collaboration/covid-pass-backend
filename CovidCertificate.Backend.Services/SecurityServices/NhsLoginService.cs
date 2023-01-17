using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Services.SecurityServices
{
    public class NhsLoginService : INhsLoginService
    {
        private const string AuthorizationCodeGrantType = "authorization_code";
        private readonly string refreshTokenGrantType = "refresh_token";

        private HttpClient httpClient;
        private readonly INhsKeyRing nhsKeyRing;
        private readonly NhsLoginSettings nhsLoginSettings;
        private readonly ILogger<NhsLoginService> logger;
        private readonly IRedisCacheService redisCacheService;
        private readonly AsyncPolicyWrap<HttpResponseMessage> retryPolicyGetUserInfo;
        private readonly AsyncPolicyWrap<HttpResponseMessage> retryPolicyNhsLoginToken;

        private void SetupHttpClient()
        {
            httpClient.DefaultRequestHeaders.Add("cache-control", "no-cache");
        }

        public NhsLoginService(INhsKeyRing keyRing,
                               NhsLoginSettings nhsLoginSettings,
                               ILogger<NhsLoginService> logger,
                               IRedisCacheService redisCacheService,
                               RetryPolicySettings settings,
                               IHttpClientFactory httpClientFactory)
        {
            this.nhsKeyRing = keyRing;
            this.nhsLoginSettings = nhsLoginSettings;
            this.logger = logger;
            this.redisCacheService = redisCacheService;
            this.httpClient = httpClientFactory.CreateClient();
            List<HttpStatusCode> statusCodes = new List<HttpStatusCode> { HttpStatusCode.GatewayTimeout, HttpStatusCode.InternalServerError };
            List<HttpStatusCode> statusCodesForToken = new List<HttpStatusCode> { HttpStatusCode.GatewayTimeout, HttpStatusCode.TooManyRequests, HttpStatusCode.InternalServerError };
            this.retryPolicyGetUserInfo = HttpRetryPolicyUtils.CreateRetryPolicyWrapCustomResponseCodes(settings.RetryCount,
                settings.RetrySleepDurationInMilliseconds,
                settings.TimeoutInMilliseconds,
                "Retrieving UserInfo from NHS API",
                logger, statusCodes);
            this.retryPolicyNhsLoginToken = HttpRetryPolicyUtils.CreateRetryPolicyWrapCustomResponseCodes(settings.RetryCount,
                settings.RetrySleepDurationInMilliseconds,
                settings.TimeoutInMilliseconds,
                "Sending request to return NHS Login Token",
                logger, statusCodesForToken);

            SetupHttpClient();
        }

        public async Task<NhsLoginToken> GetAccessTokenAsync(string refreshToken)
        {
            logger.LogTraceAndDebug($"{nameof(GetAccessTokenAsync)} was invoked");

            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentNullException("The refresh token was empty.");

            var nhsLoginToken = await SendRequestAndReturnNhsLoginTokenAsync(refreshToken);

            logger.LogTraceAndDebug($"{nameof(GetAccessTokenAsync)} has finished ");

            return nhsLoginToken;
        }

        public async Task<NhsLoginToken> GetAccessTokenAsync(string authorisationCode, string redirectUri)
        {
            logger.LogTraceAndDebug($"{nameof(GetAccessTokenAsync)} was invoked");

            if (string.IsNullOrWhiteSpace(authorisationCode))
                throw new ArgumentNullException("The authorisation code was empty.");
            if (string.IsNullOrWhiteSpace(redirectUri))
                throw new ArgumentNullException("The redirect uri was empty.");

            var nhsLoginToken = await SendRequestAndReturnNhsLoginTokenAsync(authorisationCode, redirectUri);

            logger.LogTraceAndDebug($"{nameof(GetAccessTokenAsync)} has finished");

            return nhsLoginToken;
        }

        public async Task<NhsUserInfo> GetUserInfoAsync(string accessToken)
        {
            logger.LogTraceAndDebug($"{nameof(GetUserInfoAsync)} was invoked");
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                logger.LogTraceAndDebug($"{nameof(GetUserInfoAsync)} has finished");
                throw new ArgumentNullException(accessToken, "The access token was empty.");
            }

            var accessTokenHash = accessToken.GetHashString();
            string key = $"GetUserInfo:{accessTokenHash}";

            (var cachedResponse, bool exists) = await redisCacheService.GetKeyValueAsync<string>(key);

            if (!exists)
            {
                logger.LogTraceAndDebug($"Calling Userinfo Api ");
                
                var response = await retryPolicyGetUserInfo.ExecuteAsync(async () =>
                {
                    var message = new HttpRequestMessage(HttpMethod.Get, nhsLoginSettings.UserInfoEndpoint);
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    return await httpClient.SendAsync(message);
                });

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogTraceAndDebug($"{nameof(GetUserInfoAsync)} has finished");
                    await NhsLoginUserInfoResponseErrorHandlerAsync(response);
                }
                var content = await response.Content.ReadAsStringAsync();
                await redisCacheService.AddKeyAsync<string>(key, content, RedisLifeSpanLevel.FiveMinutes);

                logger.LogTraceAndDebug($"{nameof(GetUserInfoAsync)} has finished");
                return JsonConvert.DeserializeObject<NhsUserInfo>(content);
            }
            logger.LogTraceAndDebug($"{nameof(GetUserInfoAsync)} has finished");
            return JsonConvert.DeserializeObject<NhsUserInfo>(cachedResponse);
        }

        private HttpRequestMessage CreateRefreshTokenRequest(string refreshToken)
        {
            logger.LogTraceAndDebug($"{nameof(CreateRefreshTokenRequest)} was invoked");

            var dict = SetupCommonRequestParameters();
            dict.Add("grant_type", refreshTokenGrantType);
            dict.Add("refresh_token", refreshToken);

            logger.LogTraceAndDebug($"{nameof(CreateRefreshTokenRequest)} has finished");

            return new HttpRequestMessage(HttpMethod.Post, nhsLoginSettings.TokenUri) { Content = new FormUrlEncodedContent(dict) };
        }

        private HttpRequestMessage CreateAuthorisationRequest(string authorisationCode, string redirectUri)
        {
            logger.LogTraceAndDebug($"{nameof(CreateAuthorisationRequest)} was invoked");

            var dict = SetupCommonRequestParameters();
            dict.Add("grant_type", AuthorizationCodeGrantType);
            dict.Add("redirect_uri", redirectUri);
            dict.Add("code", authorisationCode);

            logger.LogTraceAndDebug($"{nameof(CreateAuthorisationRequest)} has finished");

            return new HttpRequestMessage(HttpMethod.Post, nhsLoginSettings.TokenUri) { Content = new FormUrlEncodedContent(dict) };
        }

        private Dictionary<string, string> SetupCommonRequestParameters()
        {
            logger.LogTraceAndDebug($"{nameof(SetupCommonRequestParameters)} was invoked");

            var payload = new Dictionary<string, object>()
            {
                {"sub", nhsLoginSettings.ClientId},
                {"aud", nhsLoginSettings.TokenUri},
                {"iss", nhsLoginSettings.ClientId},
                {"exp", DateTimeOffset.UtcNow.AddHours(nhsLoginSettings.TokenLifeTime).ToUnixTimeSeconds()},
                {"jti", Guid.NewGuid()}
            };

            logger.LogTraceAndDebug($"{nameof(SetupCommonRequestParameters)} has finished");

            return new Dictionary<string, string>()
            {
                {"client_id", nhsLoginSettings.ClientId},
                {"client_assertion_type", nhsLoginSettings.ClientAssertionType},
                {"client_assertion", nhsKeyRing.SignData(payload)}
            };
        }

        private async Task<NhsLoginToken> SendRequestAndReturnNhsLoginTokenAsync(string code, string redirectUri = "")
        {
            logger.LogTraceAndDebug($"{nameof(SendRequestAndReturnNhsLoginTokenAsync)} was invoked");
            
            var response = await retryPolicyNhsLoginToken.ExecuteAsync(async () =>
            {
                var request = string.IsNullOrEmpty(redirectUri) ? CreateRefreshTokenRequest(code) : CreateAuthorisationRequest(code, redirectUri);
                return await httpClient.SendAsync(request);
            });

            logger.LogInformation($"Authorization code starting with {code.MaxLength(8)} gave response code {((int)response.StatusCode)}");
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning($"NhsLogin error with Code: {code} and redirectUri: {redirectUri}");
                await NhsLoginTokenResponseErrorHandlerAsync(response);
            }

            logger.LogTraceAndDebug($"{nameof(SendRequestAndReturnNhsLoginTokenAsync)} has finished");

            return await GetNhsLoginTokenFromResponseAsync(response);
        }

        private async Task<NhsLoginToken> GetNhsLoginTokenFromResponseAsync(HttpResponseMessage response)
        {
            logger.LogTraceAndDebug($"{nameof(GetNhsLoginTokenFromResponseAsync)} was invoked");

            var responseString = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<NhsLoginToken>(responseString) ??
                throw new FormatException($"No properties was populated or the JSON constructor was inaccessible. Response content: {responseString}");
        }

        // The error scenarios is based on the documentation of NHS Login /UserInfo error responses - "Interface Specification - Federation" found on NHS Login's GitHub:
        private async Task NhsLoginTokenResponseErrorHandlerAsync(HttpResponseMessage response)
        {
            var responseStr = await response.Content.ReadAsStringAsync();
            NhsErrorResponse error;
            try
            {
                error = JsonConvert.DeserializeObject<NhsErrorResponse>(responseStr) ??
                    throw new FormatException($"No properties was populated or the JSON constructor was inaccessible. Response content: {responseStr}");
            }
            catch (Exception e)
            {
                // We throw a new exception here, to isolate issues casued by the response not matching the rules of the Authorization Framework OAuth 2.0 [RFC6749] documentation. (This is known to happen in some cases for invalid_client and unsupported_grant_type) 
                throw new UnauthorizedException($"Unauthorized request to NHS Login /Token. StatusCode: {response.StatusCode}. Response Value: {responseStr}.", e);
            }

            switch ((response.StatusCode, error.ErrorType))
            {
                case (HttpStatusCode.BadRequest, "invalid_request"):
                    throw new BadRequestException("The request is missing a required parameter, includes an unsupported parameter value (other than grant type), repeats a parameter, includes multiple credentials, utilizes more than one mechanism for authenticating the client, or is otherwise malformed.");
                case (HttpStatusCode.BadRequest, "invalid_client"):
                    throw new HttpRequestException("Client authentication failed (e.g., unknown client, no client authentication included, or unsupported authentication method).");
                case (HttpStatusCode.BadRequest, "invalid_grant"):
                    throw new UnauthorizedException("The provided authorization grant (e.g., authorization code, resource owner credentials) or refresh token is invalid, expired, revoked, does not match the redirection URI used in the authorization request, or was issued to another client.");
                case (HttpStatusCode.BadRequest, "unauthorized_client"):
                    throw new HttpRequestException("The authenticated client is not authorized to use this authorization grant type.");
                case (HttpStatusCode.BadRequest, "unsupported_grant_type"):
                    throw new HttpRequestException("The authorization grant type is not supported by the Platform.");
                case (HttpStatusCode.BadRequest, "invalid_scope"):
                    throw new BadRequestException("The requested scope is invalid, unknown, malformed, or exceeds the scope granted by the resource owner");
                default:
                    if (response.StatusCode is HttpStatusCode.InternalServerError)
                        throw new Exception($"An InternalServerError occurred on the side of NHS login: {error.ErrorType}.");
                    else
                        throw new Exception($"An unexpected error occurred for the NHS login request with StatusCode: {response.StatusCode} and error: {error.ErrorType}.");
            }
        }
        // The error scenarios is based on the documentation of NHS Login /UserInfo error responses - "Interface Specification - Federation" found on NHS Login's GitHub:
        // https://github.com/nhsconnect/nhslogin
        private async Task NhsLoginUserInfoResponseErrorHandlerAsync(HttpResponseMessage response)
        {
            var responseStr = await response.Content.ReadAsStringAsync();
            NhsErrorResponse error;
            try
            {
                if (responseStr.Contains("error"))
                {
                    error = JsonConvert.DeserializeObject<NhsErrorResponse>(responseStr) ??
                                        throw new FormatException($"No properties was populated or the JSON constructor was inaccessible. Response content: {responseStr}");
                }
                else
                {
                    var errorMessage = "{\"error\":\"Missing Error Property in response\",\"error_description\":\"Error property was not in response\",\"error_uri\":\"Unknown\"}";
                    error = JsonConvert.DeserializeObject<NhsErrorResponse>(errorMessage) ??
                                  throw new FormatException($"No properties was populated or the JSON constructor was inaccessible. Response content: {responseStr}");
                }
            }
            catch (Exception e)
            {
                throw new UnauthorizedException($"Unauthorized request to NHS Login /UserInfo. StatusCode: {response.StatusCode}. Response Value: {responseStr}. WWW-Authenticate header: {response.Headers.WwwAuthenticate}.", e);
            }
            // We throw a new exception here, to isolate issues caused by the response not matching the rules of the Authorization Framework OAuth 2.0 [RFC6749] documentation.

            switch (response.StatusCode)
            {
                case (HttpStatusCode.BadRequest):
                    throw new BadRequestException("The request is missing a required parameter, includes an unsupported parameter or parameter value, repeats the same parameter, uses more than one method for including an access token, or is otherwise malformed.");
                case (HttpStatusCode.Unauthorized):
                    throw new UnauthorizedException("The access token provided is expired, revoked, malformed, or invalid for other reasons.");
                case (HttpStatusCode.Forbidden):
                    throw new ForbiddenException("The request requires higher privileges than provided by the access token.");
                default:
                    if (response.StatusCode is HttpStatusCode.InternalServerError)
                        throw new Exception($"An InternalServerError occurred on the side of NHS login: {error.ErrorType}.");
                    else
                        throw new Exception($"An unexpected error occurred for the NHS login request with StatusCode: {response.StatusCode}. Error: {error.ErrorType}. WWW-Authenticate header: {response.Headers.WwwAuthenticate}.");
            }
        }

        private class NhsErrorResponse
        {
            [JsonProperty("error"), JsonRequired]
            public string ErrorType { get; private set; }
            [JsonProperty("error_description")] // Note: this field is currently on NHS Login's backlog, and for that reason not supported, until NHS Login adds the support. It should be used to do a proper mapping of errors.
            public string ErrorMessage { get; private set; }
            [JsonProperty("error_uri")] // Note: this field is currently on NHS Login's backlog, and for that reason not supported, until NHS Login adds the support. It should be used to do a proper mapping of errors.
            public string ErrorUri { get; private set; }
        }
    }
}
