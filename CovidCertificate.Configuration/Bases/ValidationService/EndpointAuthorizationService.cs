using CovidCertificate.Backend.Models.Pocos;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using CovidCertificate.Backend.Utils;
using Microsoft.AspNetCore.Mvc;
using CovidCertificate.Backend.Models;
using System.Linq;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Interfaces.TokenValidation;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using CovidCertificate.Backend.Models.Exceptions;

namespace CovidCertificate.Backend.Configuration.Bases.ValidationService
{
    public class EndpointAuthorizationService : IEndpointAuthorizationService
    {
        private readonly IConfigurationRefresher configurationRefresher;
        private readonly ILogger<EndpointAuthorizationService> logger;
        private readonly IIdTokenValidationService idTokenValidationService;
        private readonly IAuthTokenValidationService authTokenValidationService;

        public EndpointAuthorizationService(IConfiguration configuration,
            ILogger<EndpointAuthorizationService> logger,
            IConfigurationRefresher configurationRefresher,
            IIdTokenValidationService idTokenValidationService,
            IAuthTokenValidationService authTokenValidationService)
        {
            this.logger = logger;
            this.configurationRefresher = configurationRefresher;
            this.idTokenValidationService = idTokenValidationService;
            this.authTokenValidationService = authTokenValidationService;
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

            if (httpRequest == null)
            {
                return new ValidationResponsePoco("HttpRequest is null", new UserProperties());
            }

            var formattedToken = JwtTokenUtils.GetFormattedAuthToken(httpRequest);
            var callingEndpoint = GetCallerEndpoint(httpRequest.Path);

            var authValidationResult = await authTokenValidationService.ValidateAuthTokenAsync(formattedToken, userProperties, callingEndpoint);
            if (authValidationResult.IsValid && !authValidationResult.IsForbidden)
            {
                var idToken = GetIdToken(httpRequest, true);
                var idValidationResult = await idTokenValidationService.ValidateIdTokenAsync(idToken, authValidationResult.TokenClaims, userProperties);
                if (idValidationResult.IsValid && !idValidationResult.IsForbidden && !TokenValidationUtils.TokensBelongToSamePerson(formattedToken, idToken))
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

        private string GetCallerEndpoint(string path)
        {
            var endpoint = path.Split('/');
            return endpoint[endpoint.Length - 1];
        }
    }
}
