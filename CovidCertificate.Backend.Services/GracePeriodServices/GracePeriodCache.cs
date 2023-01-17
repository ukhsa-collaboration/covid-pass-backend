using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.GracePeriodServices
{
    public class GracePeriodCache : IGracePeriodCache
    {
        private readonly ILogger<GracePeriodCache> logger;
        private readonly IRedisCacheService redisCacheService;
        private readonly IMongoRepository<UserPolicies> mongoRepository;

        private const string cacheKeyPrefix = "GracePeriod:";

        public GracePeriodCache(
            ILogger<GracePeriodCache> logger,
            IRedisCacheService redisCacheService,
            IMongoRepository<UserPolicies> mongoRepository)
        {
            this.logger = logger;
            this.redisCacheService = redisCacheService;
            this.mongoRepository = mongoRepository;
        }

        public async Task<GracePeriod> GetGracePeriodAsync(string nhsNumberDobHash)
        {
            logger.LogInformation("GetGracePeriod was invoked");
            var cacheKey = cacheKeyPrefix + nhsNumberDobHash;

            (var gracePeriodFromCache, bool gracePeriodExistsInCache) = await redisCacheService.GetKeyValueAsync<GracePeriod>(cacheKey);

            if (gracePeriodExistsInCache)
            {
                logger.LogTraceAndDebug($"Found GracePeriod in cache for user {nhsNumberDobHash}");
                return gracePeriodFromCache;
            }

            var gracePeriodFromDb = await GetGracePeriodFromDbAsync(nhsNumberDobHash);

            if (gracePeriodFromDb != default)
            {
                await redisCacheService.AddKeyAsync(cacheKey, gracePeriodFromDb, RedisLifeSpanLevel.TenHours);
            }

            logger.LogTraceAndDebug($"Found GracePeriod in database for user {nhsNumberDobHash}");
            logger.LogInformation("GetGracePeriod has finished");
            return gracePeriodFromDb;
        }

        public async Task<bool> AddToCacheAsync(GracePeriod gracePeriod, string nhsNumberDobHash)
        {
            var cacheKey = cacheKeyPrefix + nhsNumberDobHash;
            if (gracePeriod != default)
            {
                return await redisCacheService.AddKeyAsync(cacheKey, gracePeriod, RedisLifeSpanLevel.TenHours);
            }

            return false;
        }

        private async Task<GracePeriod> GetGracePeriodFromDbAsync(string nhsNumberDobHash)
        {
            logger.LogInformation("GetGracePeriodFromDbAsync was invoked");

            var userPolicies = await mongoRepository.FindOneAsync(x => x.NhsNumberDobHash == nhsNumberDobHash);
            var currentGracePeriod = userPolicies?.GracePeriod;

            logger.LogInformation("GetGracePeriodFromDbAsync has finished");

            return currentGracePeriod;
        }
    }
}
