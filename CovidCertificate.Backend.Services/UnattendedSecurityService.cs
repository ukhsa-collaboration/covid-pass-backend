using System;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Utils.Extensions;

namespace CovidCertificate.Backend.Services
{
    public class UnattendedSecurityService : IUnattendedSecurityService
    {
        private readonly string validTokenHash = "BB686CA239E966188AA50D67FA2C8B0C68981962BEE3AD910CB02595AACAFBD2";

        public void Authorize()
        {
            string functionToken;
            try
            {
                functionToken = Environment.GetEnvironmentVariable("unattended_api_access_token");
            }
            catch (ArgumentNullException)
            {
                throw new UnauthorizedUnattendedApiCallException("Configuration value not found");
            }

            if (string.IsNullOrEmpty(functionToken))
                throw new UnauthorizedUnattendedApiCallException("Token is empty");

            var functionTokenHash = StringUtils.GetHashString(functionToken);
            if (functionTokenHash != validTokenHash)
            {
                throw new UnauthorizedUnattendedApiCallException("Incorrect token");
            }
        }
    }
}
