using CovidCertificate.Backend.Models.Pocos;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Security.Claims;
using CovidCertificate.Backend.Utils;
using Microsoft.AspNetCore.Mvc;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Settings;
using System.Linq;
using CovidCertificate.Backend.Interfaces.DateTimeProvider;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Interfaces.JwtServices;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;
using MongoDB.Driver;
using StackExchange.Redis;

namespace CovidCertificate.Backend.Configuration.Bases.ValidationService
{
    public class EndpointAuthorizationService : IEndpointAuthorizationService
    {
        private readonly int _minimumSecondsBeforeExpiry;
        private readonly IList<string> allowedP5Endpoints;
        private readonly IList<string> allowedP5PlusEndpoints;
        private readonly IList<string> allowedNoDomesticEndPoints;
        private readonly IList<string> allowedP5U12Endpoints;
        private readonly IList<string> allowedP5PlusU12Endpoints;
        private readonly IConfigurationRefresher configurationRefresher;
        private readonly IJwtValidator jwtValidator;
        private readonly IConfiguration configuration;
        private readonly IMongoRepository<OdsCodeCountryModel> mongoRepository;
        private readonly IRedisCacheService redisCacheService;
        private readonly ILogger<EndpointAuthorizationService> logger;
        private readonly IFeatureManager featureManager;
        private readonly IGracePeriodService gracePeriodService;
        private readonly IDateTimeProviderService dateTimeProviderService;

        public EndpointAuthorizationService(IConfiguration configuration,
            ILogger<EndpointAuthorizationService> logger,
            IMongoRepository<OdsCodeCountryModel> mongoRepository,
            IRedisCacheService redisCacheService,
            IConfigurationRefresher configurationRefresher,
            IFeatureManager featureManager,
            IJwtValidator jwtValidator,
            IGracePeriodService gracePeriodService,
            IDateTimeProviderService dateTimeProviderService)
        {
            this.logger = logger;
            this.mongoRepository = mongoRepository;
            this.redisCacheService = redisCacheService;
            this.configurationRefresher = configurationRefresher;
            this.featureManager = featureManager;
            this.dateTimeProviderService = dateTimeProviderService;

            _minimumSecondsBeforeExpiry = int.TryParse(configuration["MinimumSecondsBeforeTokenExpiry"], out _minimumSecondsBeforeExpiry)
                ? this._minimumSecondsBeforeExpiry : 0;
            allowedP5Endpoints = GetEndpointListFromString(configuration["AllowedP5EndPoints"]);
            allowedP5PlusEndpoints = GetEndpointListFromString(configuration["AllowedP5PlusEndPoints"]);
            allowedP5PlusU12Endpoints = GetEndpointListFromString(configuration["AllowedP5PlusU12Endpoints"]);
            allowedP5U12Endpoints = GetEndpointListFromString(configuration["AllowedP5U12Endpoints"]);
            allowedNoDomesticEndPoints = GetEndpointListFromString(configuration["AllowedNoDomesticEndPoints"]);
            this.jwtValidator = jwtValidator;
            this.configuration = configuration;
            this.gracePeriodService = gracePeriodService;
        }

        /// <summary>
        /// Validates a token against our JWT validator
        /// </summary>
        /// <param name="httpRequest">Our base request</param>
        /// <param name="tokenSchema">The token schema to use</param>
        /// <returns>A poco containing a http response or the claims if valid</returns>
        public async Task<ValidationResponsePoco> AuthoriseEndpointAsync(HttpRequest httpRequest, string tokenSchema = "CovidCertificate")
        {
            var userProperties = new UserProperties();

            logger.LogTraceAndDebug("AuthoriseEndpoint was invoked");

            await configurationRefresher.TryRefreshAsync();

            var authValidationResult = await ValidateAuthTokenAsync(httpRequest, userProperties);
            if (authValidationResult.IsValid && !authValidationResult.IsForbidden)
            {
                var idValidationResult = await ValidateIdTokenAsync(httpRequest, authValidationResult.TokenClaims, userProperties);
                if (idValidationResult.IsValid && !idValidationResult.IsForbidden && !TokensBelongToSamePerson(httpRequest))
                {
                    idValidationResult.IsValid = false;
                    idValidationResult.Response = new UnauthorizedObjectResult("Auth token and id-token do not belong to the same person");
                }
                logger.LogTraceAndDebug("AuthoriseEndpoint has finished");
                return idValidationResult;
            }
            logger.LogTraceAndDebug("AuthoriseEndpoint has finished invoked");
            return authValidationResult;
        }

        public string GetIdToken(HttpRequest request, bool isIdTokenRequired = false)
        {
            if (request.Headers["id-token"].Any())
            {
                return request.Headers["id-token"];
            }

            if (isIdTokenRequired)
                throw new BadRequestException("No id-token in the request");

            return "";
        }

        public bool ResponseIsInvalid(ValidationResponsePoco validationResponse)
        {
            if (validationResponse.IsForbidden || !validationResponse.IsValid)
            {
                logger.LogInformation("validationResult is invalid");
                return true;
            }
            return false;
        }

        private bool TokensBelongToSamePerson(HttpRequest httpRequest)
        {
            string formattedToken = JwtTokenUtils.GetFormattedAuthToken(httpRequest);
            string idToken = GetIdToken(httpRequest);


            string nhsNumberToken = JwtTokenUtils.GetClaim(formattedToken, JwtTokenUtils.NhsNumberClaimName);
            string nhsNumberIdToken = JwtTokenUtils.GetClaim(idToken, JwtTokenUtils.NhsNumberClaimName);

            return nhsNumberToken.Equals(nhsNumberIdToken);
        }

        /// <summary>
        /// Validates the Id token
        /// </summary>
        /// <param name="httpRequest">Our base request</param>
        /// <param name="tokenClaims">The token schema to use</param>
        /// <param name="userProperties">User's properties</param>
        /// <returns>A poco containing a http response or the claims if valid</returns>
        private async Task<ValidationResponsePoco> ValidateIdTokenAsync(HttpRequest httpRequest, ClaimsPrincipal tokenClaims,
            UserProperties userProperties)
        {
            logger.LogTraceAndDebug("ValidateIdToken was invoked");

            var idToken = GetIdToken(httpRequest, true);
            if (string.IsNullOrEmpty(idToken))
                return new ValidationResponsePoco("Does not contain token (id-token)", new UserProperties());

            try
            {
                var jwtToken = new JwtSecurityToken(idToken);
                if (IsTokenCloseToExpire(jwtToken))
                    return new ValidationResponsePoco("Token (id-token) expired or close to expiry", new UserProperties());
                if (!CheckAudiencesMatch(jwtToken))
                    return new ValidationResponsePoco("Token Audience does not match", new UserProperties());
                var tokenSchema = "id-token";

                var isJwtTokenValid = await jwtValidator.IsValidTokenAsync(idToken, tokenSchema);

                if (!isJwtTokenValid)
                    return new ValidationResponsePoco("The jwt token (id-token) is not valid", new UserProperties());

                return new ValidationResponsePoco(tokenClaims, userProperties);
            }
            catch (ArgumentException e)
            {
                logger.LogWarning(e, e.Message);
                return new ValidationResponsePoco("Invalid token (id-token)", new UserProperties());
            }
            catch (SecurityTokenException e)
            {
                logger.LogWarning(e, e.Message);
                return new ValidationResponsePoco("Invalid token (id-token)", new UserProperties());
            }
        }

        /// <summary>
        /// Validates the Authorization token against our JWT validator
        /// </summary>
        /// <param name="httpRequest">Our base request</param>
        /// <param name="userProperties">User's properties</param>
        /// <returns>A poco containing a http response or the claims if valid</returns>
        private async Task<ValidationResponsePoco> ValidateAuthTokenAsync(HttpRequest httpRequest, UserProperties userProperties)
        {
            logger.LogTraceAndDebug("ValidateAuthTokenAsync was invoked");

            if (httpRequest == null)
            {
                return new ValidationResponsePoco("HttpRequest is null", new UserProperties());
            }

            var formattedToken = JwtTokenUtils.GetFormattedAuthToken(httpRequest);
            if (string.IsNullOrEmpty(formattedToken))
            {
                return new ValidationResponsePoco("Does not contain token", new UserProperties());
            }

            try
            {
                var jwtToken = new JwtSecurityToken(formattedToken);

                if (IsTokenCloseToExpire(jwtToken))
                {
                    return new ValidationResponsePoco("Token (auth) expired or close to expiry", new UserProperties());
                }
                if (!CheckAudiencesMatch(jwtToken))
                    return new ValidationResponsePoco("Token Audience does not match", new UserProperties());
                var tokenSchema = jwtToken.Issuer;
                logger.LogTraceAndDebug($"tokenSchema is {tokenSchema}");

                if (!await jwtValidator.IsValidTokenAsync(formattedToken, tokenSchema))
                {
                    return new ValidationResponsePoco("The jwt token is not valid", new UserProperties());
                }

                var tokenClaims = await jwtValidator.GetClaimsAsync(formattedToken, tokenSchema);
                if (tokenClaims == null)
                {
                    return new ValidationResponsePoco("Invalid token claims", new UserProperties());
                }

                var dateOfBirth = ParseTokenClaimDobToDateTime(tokenClaims.FindFirst(ClaimTypes.DateOfBirth)?.Value);
                var odsCode = tokenClaims.FindFirst("GPODSCode")?.Value;
                var odsCodeCountry = await GetCountryFromOdsCodeAsync(odsCode);
                userProperties.Country = odsCodeCountry;

                if (UnderAppAccessAge(dateOfBirth))
                {
                    var minAppAccessAge = Int32.Parse(configuration["MinimumAppAccessAge"]);
                    return new ValidationResponsePoco($"The user is under {minAppAccessAge} years old.", userProperties);
                }
                
                logger.LogTraceAndDebug("ValidateAuthTokenAsync has finished");

                var callingEndpoint = GetCallerEndpoint(httpRequest.Path);
                var domesticAccessLevel = await GetDomesticAccessLevelAsync(dateOfBirth);
                userProperties.DomesticAccessLevel = domesticAccessLevel;

                if (domesticAccessLevel == DomesticAccessLevel.NoAccess &&
                    !IsValidNoDomesticAccessEndpoint(callingEndpoint))
                {
                    return new ValidationResponsePoco( true, "You are too young to access the domestic pass.");
                }

                var identityProofingLevel = await GetIdentityProofingLevelAsync(tokenClaims, userProperties);
                if(domesticAccessLevel == DomesticAccessLevel.U12)
                {
                    return identityProofingLevel switch
                    {
                        IdentityProofingLevel.P5 when !IsValidU12P5Endpoint(callingEndpoint) => new ValidationResponsePoco(true, "U12 P5 users cannot access this service"),
                        IdentityProofingLevel.P5Plus when !IsValidU12P5PlusEndpoint(callingEndpoint) => new ValidationResponsePoco(true, "U12 P5+ users cannot access this service"),
                        _ => new ValidationResponsePoco(tokenClaims, userProperties)
                    };
                }
                return identityProofingLevel switch
                {
                    IdentityProofingLevel.P5 when !IsValidP5Endpoint(callingEndpoint) => new ValidationResponsePoco(true, "P5 Users are forbidden to use this service."),
                    IdentityProofingLevel.P5Plus when !IsValidP5PlusEndpoint(callingEndpoint) => new ValidationResponsePoco(true, "P5+ Users are forbidden to use this service."),
                    
                    _ => new ValidationResponsePoco(tokenClaims, userProperties)
                };
            }
            catch (ArgumentException e)
            {
                logger.LogWarning(e, e.Message);
                return new ValidationResponsePoco("Invalid token", userProperties);
            }
            catch (SecurityTokenException e)
            {
                logger.LogWarning(e, e.Message);
                return new ValidationResponsePoco("Invalid token", userProperties);
            }
        }

        private async Task<string> GetCountryFromOdsCodeAsync(string odsCode)
        {
            logger.LogTraceAndDebug($"{nameof(GetCountryFromOdsCodeAsync)} was invoked");

            if (string.IsNullOrEmpty(odsCode))
            {
                logger.LogTraceAndDebug("ODS Code is null or empty ");
                return StringUtils.UnknownCountryString;
            }
            try
            {
                var odsCodeCountryHash = odsCode.GetHashString();
                var key = $"GetOdsCode:{odsCodeCountryHash}";
                (var cachedResponse, var cacheExists) = await redisCacheService.GetKeyValueAsync<string>(key);

                logger.LogTraceAndDebug($"Searching for Cached Response, {nameof(redisCacheService.GetKeyValueAsync)} was invoked");

                if (cacheExists)
                { 
                    logger.LogTraceAndDebug($"{odsCode} Code exists in cache. {nameof(GetCountryFromOdsCodeAsync)}, has finished"); 
                    return cachedResponse;
                }

                logger.LogTraceAndDebug($"{odsCode} does not exist in cache");

                var odsCodeCountryModel = await mongoRepository.FindOneAsync(x => x.OdsCode == odsCode);

                if (odsCodeCountryModel == null)
                {
                    logger.LogInformation($"No records in the database for specified OdsCode: {odsCode}");
                    return StringUtils.UnknownCountryString;
                }

                if (string.IsNullOrEmpty(odsCodeCountryModel.Country))
                {
                    logger.LogInformation($"The country of specified OdsCode: {odsCode} is empty.");
                    return StringUtils.UnknownCountryString;
                }

                logger.LogTraceAndDebug($"{nameof(redisCacheService.AddKeyAsync)} will be invoked");
                await redisCacheService.AddKeyAsync<string>(key, odsCodeCountryModel.Country, RedisLifeSpanLevel.OneDay);
  
                return odsCodeCountryModel.Country;
            }
            catch (Exception e)
            {
                logger.LogWarning($"Error Message: {e}");
                return StringUtils.UnknownCountryString;
            }
        }

        private bool AgeIsBelowLimit(DateTime dateOfBirthInUTC, int ageLimit)
        {
            var age = DateUtils.GetAgeInYears(dateOfBirthInUTC);

            if (age < ageLimit)
                return true;

            return false;
        }

        private bool UnderDomesticPassAccessAge(DateTime dateOfBirth)
        {
            var minDomesticAccessAge = Int32.Parse(configuration["MinimumDomesticAccessAge"]);
            return AgeIsBelowLimit(dateOfBirth, minDomesticAccessAge);

        }
        private bool UnderAppAccessAge(DateTime dateOfBirth)
        {
            var minAppAccessAge = Int32.Parse(configuration["MinimumAppAccessAge"]);
            return AgeIsBelowLimit(dateOfBirth, minAppAccessAge);
        }

        private bool U12TravelPassAge(DateTime dateOfBirth)
        {
            var U12AccessAge = Int32.Parse(configuration["U12AccessAge"]);
            return AgeIsBelowLimit(dateOfBirth, U12AccessAge);
        }

        private async Task<DomesticAccessLevel> GetDomesticAccessLevelAsync(DateTime dateOfBirth)
        {
           
            var u12TravelPass = await featureManager.IsEnabledAsync(FeatureFlags.U12TravelPass);
            if(u12TravelPass && U12TravelPassAge(dateOfBirth))
            {
                return DomesticAccessLevel.U12;
            }
            var ageBasedDomesticAccessIsEnabled = await featureManager.IsEnabledAsync(FeatureFlags.DomesticPassAgeLimit);
            if (ageBasedDomesticAccessIsEnabled && UnderDomesticPassAccessAge(dateOfBirth) )
            {
                return DomesticAccessLevel.NoAccess;
            }

            
            return DomesticAccessLevel.Access;
        }

        private async Task<IdentityProofingLevel> GetIdentityProofingLevelAsync(ClaimsPrincipal tokenClaims,
            UserProperties userProperties)
        {
            logger.LogTraceAndDebug($"{nameof(GetIdentityProofingLevel)} was invoked");

            var identityProofingClaim = tokenClaims.FindFirst("IdentityProofingLevel");

            IdentityProofingLevel identityProofingLevel;
            var parsedIdentityProofingLevel = Enum.TryParse(identityProofingClaim.Value, true, out identityProofingLevel);

            if (!parsedIdentityProofingLevel)
            {
                throw new DataMisalignedException("IdentityProofingLevel not recognised. " + identityProofingClaim.Value);
            }

            if (identityProofingLevel == IdentityProofingLevel.P5)
            {

                identityProofingLevel = await IsUserP5PlusAsync(tokenClaims, userProperties) ?
                    IdentityProofingLevel.P5Plus : IdentityProofingLevel.P5;
            }

            logger.LogTraceAndDebug($"User identity proofing level: {identityProofingLevel}");
            userProperties.IdentityProofingLevel = identityProofingLevel;

            logger.LogTraceAndDebug($"{nameof(GetIdentityProofingLevel)} has finished");
            return identityProofingLevel;
        }

        private async Task<bool> IsUserP5PlusAsync(ClaimsPrincipal tokenClaims, UserProperties userProperties)
        {
            var phoneNumberMatchedWithPdsClaim = tokenClaims.FindFirst("PhoneNumberPdsMatched");
            var nhsNumber = tokenClaims.FindFirst("NHSNumber")?.Value;
            var dateOfBirth = ParseTokenClaimDobToDateTime(tokenClaims.FindFirst(ClaimTypes.DateOfBirth)?.Value);
            var nhsNumberDobHash = HashUtils.GenerateHash(nhsNumber, dateOfBirth);
            var phoneNumberMatchesPds = phoneNumberMatchedWithPdsClaim?.Value == "true";

            if (phoneNumberMatchesPds) // A P5 user is considered P5+ when phone number matches pds.
            {
                return true;
            }
            if(userProperties.DomesticAccessLevel == DomesticAccessLevel.U12) // U12 users do not have a grace period
            {
                userProperties.GracePeriod = null;
                return false;
            }
            var gracePeriod = await gracePeriodService.GetGracePeriodAsync(nhsNumberDobHash);
            
            userProperties.GracePeriod = gracePeriod;
            
            if (gracePeriod.IsActive) // If user is within his grace period, he gets upgraded from p5 -> p5+.
            {
                return true;
            }

            return false;
        }

        private DateTime ParseTokenClaimDobToDateTime(string dateOfBirthTicks)
        {
            if (dateOfBirthTicks != default)
            {
                var isLong = long.TryParse(dateOfBirthTicks, out var dobResult);
                if (isLong)
                {
                    var dateOfBirth = DateTime.FromFileTimeUtc(dobResult);
                    return dateOfBirth;
                }
            }
            throw new DataMisalignedException("Unable to parse " + dateOfBirthTicks + "to DateTime");
        }

        protected bool IsValidNoDomesticAccessEndpoint(string path)
        {
            return allowedNoDomesticEndPoints.Contains(path);
        }

        private bool IsValidP5Endpoint(string path)
        {
            return allowedP5Endpoints.Contains(path);
        }
        private bool IsValidU12P5Endpoint (string path)
        {
            return allowedP5U12Endpoints.Contains(path);
        }
        private bool IsValidU12P5PlusEndpoint(string path)
        {
            return allowedP5PlusU12Endpoints.Contains(path);
        }
        private bool IsTokenCloseToExpire(JwtSecurityToken token)
        {
            return token.ValidTo.AddSeconds(-_minimumSecondsBeforeExpiry) < dateTimeProviderService.UtcNow;
        }

        private bool CheckAudiencesMatch(JwtSecurityToken token)
        {
            return token.Audiences.FirstOrDefault() == configuration["Audience"];
        }

        private async Task<IdentityProofingLevel> GetIdentityProofingLevel(ClaimsPrincipal tokenClaims,
            UserProperties userProperties)
        {
            logger.LogTraceAndDebug($"GetIdentityProofingLevel was invoked");

            var identityProofingClaim = tokenClaims.FindFirst("IdentityProofingLevel");

            IdentityProofingLevel identityProofingLevel;
            var parsedIdentityProofingLevel = Enum.TryParse(identityProofingClaim.Value, true, out identityProofingLevel);

            if (!parsedIdentityProofingLevel)
            {
                throw new DataMisalignedException("IdentityProofingLevel not recognised. " + identityProofingClaim.Value);
            }

            if (identityProofingLevel == IdentityProofingLevel.P5)
            {
                identityProofingLevel = await IsUserP5PlusAsync(tokenClaims, userProperties) ?
                    IdentityProofingLevel.P5Plus : IdentityProofingLevel.P5;
            }

            logger.LogTraceAndDebug($"User identity proofing level: {identityProofingLevel}");
            userProperties.IdentityProofingLevel = identityProofingLevel;

            logger.LogTraceAndDebug($"GetIdentityProofingLevel has finished");
            return identityProofingLevel;
        }

        private bool IsValidP5PlusEndpoint(string path)
        {
            return allowedP5PlusEndpoints.Contains(path);
        }

        private string GetCallerEndpoint(string path)
        {
            var endpoint = path.Split('/');
            return endpoint[endpoint.Length - 1];
        }

        private IList<string> GetEndpointListFromString(string allowedEndpoints)
        {
            var lstEndPoints = new List<string>();
            if (!string.IsNullOrEmpty(allowedEndpoints))
            {
                var endpoints = allowedEndpoints.Split(';');
                lstEndPoints.AddRange(endpoints);
            }
            return lstEndPoints;
        }
    }
}
