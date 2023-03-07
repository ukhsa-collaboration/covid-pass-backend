using System;
using System.Linq;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.DASigningService.Interfaces;
using CovidCertificate.Backend.DASigningService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.DASigningService.Services
{
    public class RegionConfigService : IRegionConfigService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<RegionConfigService> logger;

        public RegionConfigService(IConfiguration configuration, ILogger<RegionConfigService> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public RegionConfig GetRegionConfig(string regionSubscriptionHeader, ErrorHandler errorHandler)
        {
            try
            {
                var regionCode = GetRegionCode(regionSubscriptionHeader);

                var regionConfigs = configuration.GetSection("RegionMappings").Get<RegionConfig[]>();
                var regionConfig = regionConfigs.FirstOrDefault(x => x.SubscriptionKeyIdentifier == regionCode);

                if (regionConfig == null)
                {
                    logger.LogError("'regionConfig' is null.");

                    errorHandler.AddError(ErrorCode.UNEXPECTED_SYSTEM_ERROR, "Unrecognized region code in 'Region-Subscription-Name' header: " + regionCode);

                    return null;
                }

                logger.LogDebug("regionConfig found.");

                return regionConfig;
            }
            catch (Exception e)
            {
                logger.LogCritical(e, $"Error occured when obtaining regionCode from header. ex. message: '{e.Message}'.");

                errorHandler.AddError(ErrorCode.UNEXPECTED_SYSTEM_ERROR, "Unexpected error retrieving calling region");

                return null;
            }
        }

        private string GetRegionCode(string subscriptionName)
        {
            var regionalCode = subscriptionName.Split("-")[1];

            return regionalCode;
        }
    }
}
