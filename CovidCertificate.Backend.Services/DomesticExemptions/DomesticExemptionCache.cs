using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Redis;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Services.DomesticExemptions
{
    public class DomesticExemptionCache : IDomesticExemptionCache
    {
        private const string cacheKeyExemptions = "DomesticExemptions:AllExemptionsTuple";

        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<DomesticExemptionCache> logger;
        private readonly IRedisCacheService redisCacheService;
        private readonly IMemoryCacheService memoryCache;

        private readonly int inMemoryCacheTimeToLiveInSeconds = -1;

        public DomesticExemptionCache(ILogger<DomesticExemptionCache> logger,
            IServiceScopeFactory serviceScopeFactory,
            IRedisCacheService redisCacheService,
            DomesticExemptionSettings settings,
            IMemoryCacheService memoryCache)
        {
            this.logger = logger;
            this.serviceScopeFactory = serviceScopeFactory;
            this.redisCacheService = redisCacheService;
            this.memoryCache = memoryCache;

            inMemoryCacheTimeToLiveInSeconds = settings.InMemoryTimeToLiveSeconds;
        }

        public async Task<IDictionary<string, IEnumerable<DomesticExemptionRecord>>> GetDomesticExemptionsAsync()
        {
            logger.LogTraceAndDebug("GetDomesticExemptions was invoked");

            var exemptions = await memoryCache.GetOrCreateCacheAsync(cacheKeyExemptions,
                async () => await CreateExemptionsCacheAsync(),
                DateTimeOffset.UtcNow.AddSeconds(inMemoryCacheTimeToLiveInSeconds));

            logger.LogTraceAndDebug("GetDomesticExemptions has finished");

            return exemptions.ToDictionary(x => x.First().NhsDobHash);
        }

        private async Task<IEnumerable<IEnumerable<DomesticExemptionRecord>>> CreateExemptionsCacheAsync()
        {
            (var exemptionFromRedisCache, bool exemptionsExistInRedisCache) = await redisCacheService.GetKeyValueAsync<IEnumerable<IEnumerable<DomesticExemptionRecord>>>(cacheKeyExemptions);

            if (exemptionsExistInRedisCache)
            {
                logger.LogTraceAndDebug("Found exemption in Redis cache");
                logger.LogTraceAndDebug("GetDomesticExemptions has finished");

                return exemptionFromRedisCache;
            }

            var exemptionsFromDb = await GetDomesticExemptionsFromDbAsync();

            logger.LogTraceAndDebug("Found exemption in database");
            logger.LogTraceAndDebug("GetDomesticExemptions has finished");

            await UpdateRedisCacheAsync(exemptionsFromDb);

            logger.LogTraceAndDebug("CreateExemptionsCacheAsync has finished");

            return exemptionsFromDb;
        }

        private async Task UpdateRedisCacheAsync(IEnumerable<IEnumerable<DomesticExemptionRecord>> exemptions)
        {
            logger.LogTraceAndDebug("UpdateRedisCacheAsync was invoked");
            await redisCacheService.AddKeyAsync(cacheKeyExemptions, exemptions, RedisLifeSpanLevel.TenHours);
            logger.LogTraceAndDebug("UpdateRedisCacheAsync has finished");
        }

        private async Task<IEnumerable<IEnumerable<DomesticExemptionRecord>>> GetDomesticExemptionsFromDbAsync()
        {
            logger.LogTraceAndDebug("GetDomesticExemptionsFromDbAsync was invoked");

            using (var scope = serviceScopeFactory.CreateScope())
            {
                var mongoRepository = scope.ServiceProvider.GetService<IMongoRepository<DomesticExemptionRecord>>();
                var domesticExemptions = await mongoRepository.FindAllAsync(x => true);   
                
                var sortedDomesticExemptions = domesticExemptions
                    .Where(x => x.NhsDobHash != null)
                    .Where(x => x.Reason != null)
                    .GroupBy(x => x.NhsDobHash);

                logger.LogTraceAndDebug("GetDomesticExemptionsFromDbAsync has finished");
                return sortedDomesticExemptions;
            }
        }
    }
}
