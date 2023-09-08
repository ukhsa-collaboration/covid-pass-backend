using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.ModelVersioning;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Interfaces.Redis;
using System.Diagnostics.CodeAnalysis;

namespace CovidCertificate.Backend.Services.Redis
{
    [ExcludeFromCodeCoverage]
    public class RedisCacheService : IRedisCacheService
    {
        private readonly ILogger<RedisCacheService> logger;
        private readonly IRedisDatabasesProvider redisDatabasesProvider;
        private readonly IFeatureManager featureManager;
        private readonly IModelVersionService modelVersionService;

        public RedisCacheService(
            ILogger<RedisCacheService> logger,
            IFeatureManager featureManager,
            IModelVersionService modelVersionService,
            IRedisDatabasesProvider redisDatabasesProvider)
        {
            this.logger = logger;
            this.featureManager = featureManager;
            this.modelVersionService = modelVersionService;
            this.redisDatabasesProvider = redisDatabasesProvider;
        }

        private TimeSpan GetExpirationTime(RedisLifeSpanLevel redisLifeSpanLevel)
            => redisLifeSpanLevel switch
            {
                RedisLifeSpanLevel.FiveMinutes => TimeSpan.FromMinutes(5),
                RedisLifeSpanLevel.ThirtyMinutes => TimeSpan.FromMinutes(30),
                RedisLifeSpanLevel.OneHour => TimeSpan.FromHours(1),
                RedisLifeSpanLevel.TenHours => TimeSpan.FromHours(10),
                RedisLifeSpanLevel.OneDay => TimeSpan.FromDays(1),
                _ => TimeSpan.FromMinutes(5),
            };

        public async Task<bool> AddKeyAsync<T>(string key, T value, RedisLifeSpanLevel redisLifeSpanLevel)
        {
            logger.LogInformation($"AddKeyAsync was invoked ({nameof(RedisCacheService)})");

            if (!await featureManager.IsEnabledAsync(FeatureFlags.RedisEnabled))
            {
                return true;
            }

            var isSuccess = false;
            var redisKey = BuildRedisKey(key);

            try
            {
                var tasks = new List<Task<bool>>();

                var databases = redisDatabasesProvider.GetDatabases();

                foreach (var database in databases)
                {
                    tasks.Add(database.StringSetAsync(
                        redisKey, 
                        JsonConvert.SerializeObject(value),
                        GetExpirationTime(redisLifeSpanLevel)));

                    logger.LogDebug(LogType.Redis, $"Key '{redisKey}' added to task list.");
                }

                var setResults = await Task.WhenAll(tasks);
                isSuccess = setResults.All(x => x);
                logger.LogTraceAndDebug(LogType.Redis, $"Key {redisKey} {(isSuccess ? string.Empty : "not")} added to Redis cache.");
            }
            catch (RedisConnectionException rex)
            {
                logger.LogError(LogType.Redis, $"{nameof(RedisConnectionException)} thrown when attempting to add key {redisKey} to Redis cache. Exception Message: {rex}");
            }
            catch (RedisTimeoutException rex)
            {
                logger.LogError(LogType.Redis, $"{nameof(RedisTimeoutException)} thrown when attempting to add key {redisKey} to Redis cache. Exception Message: {rex}");
            }
            catch (Exception ex)
            {
                logger.LogError(LogType.Redis, $"Error in adding key {redisKey} to Redis cache.", ex);
            }

            return isSuccess;
        }

        public async Task<(T, bool)> GetKeyValueAsync<T>(string key)
        {
            logger.LogInformation($"GetKeyValueAsync was invoked ({nameof(RedisCacheService)})");

            if (!await featureManager.IsEnabledAsync(FeatureFlags.RedisEnabled))
            {
                return (default, false);
            }

            var redisKey = BuildRedisKey(key);

            try
            {
                var tasks = new List<Task<RedisValue>>();

                var databases = redisDatabasesProvider.GetDatabases();

                foreach (var database in databases)
                {
                    tasks.Add(database.StringGetAsync(redisKey));
                }

                var item = await (await Task.WhenAny(tasks));
                var itemExists = !item.IsNullOrEmpty;
                logger.LogDebug(LogType.Redis, $"Key {redisKey} {(itemExists ? string.Empty : "not")} found in Redis cache.");
                return itemExists ? (JsonConvert.DeserializeObject<T>(item), true) : (default(T), false);
            }
            catch (RedisConnectionException rex)
            {
                logger.LogWarning(LogType.Redis, $"{nameof(RedisConnectionException)} thrown when attempting to get value from Redis cache for key {redisKey}. Exception Message: '{rex.Message}'.");
                return (default(T), false);
            }
            catch (RedisTimeoutException rex)
            {
                logger.LogWarning(LogType.Redis, $"{nameof(RedisTimeoutException)} thrown when attempting to get value from Redis cache for key {redisKey}. Exception Message: '{rex.Message}'.");
                return (default(T), false);
            }
            catch (Exception ex)
            {
                logger.LogError(LogType.Redis, $"Error in getting value from Redis cache for key {redisKey}. Exception Message: '{ex.Message}'.");
                return (default(T), false);
            }
        }

        private string BuildRedisKey(string key)
            => $"{modelVersionService.ModelVersion}:{key}";
    }
}
