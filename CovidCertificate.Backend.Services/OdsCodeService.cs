using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Redis;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services
{
    public class OdsCodeService : IOdsCodeService
    {
        private readonly ILogger<OdsCodeService> logger;
        private readonly IRedisCacheService redisCacheService;
        private readonly IMongoRepository<OdsCodeCountryModel> mongoRepository;

        public OdsCodeService(ILogger<OdsCodeService> logger,
            IRedisCacheService redisCacheService,
            IMongoRepository<OdsCodeCountryModel> mongoRepository)
        {
            this.logger = logger;
            this.redisCacheService = redisCacheService;
            this.mongoRepository = mongoRepository;
        }

        public async Task<string> GetCountryFromOdsCodeAsync(string odsCode)
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
                    logger.LogInformation($"{odsCode} Code exists in cache. {nameof(GetCountryFromOdsCodeAsync)}, has finished");
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
    }
}
