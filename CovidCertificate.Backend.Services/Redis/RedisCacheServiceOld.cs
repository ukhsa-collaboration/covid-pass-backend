using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.ModelVersioning;
using CovidCertificate.Backend.Interfaces.Redis;
using CovidCertificate.Backend.Models.Helpers;

namespace CovidCertificate.Backend.Services.Redis
{
    public class RedisCacheServiceOld : IRedisCacheService
    {
        private readonly ILogger<RedisCacheService> logger;
        private static IConfiguration configuration;
        private static ConcurrentBag<ConcurrentBag<Lazy<ConnectionMultiplexer>>> connections;
        private readonly IFeatureManager featureManager;
        private readonly IModelVersionService modelVersionService;

        public RedisCacheServiceOld(
            ILogger<RedisCacheService> _logger,
            IConfiguration _configuration,
            IFeatureManager _featureManager,
            IModelVersionService modelVersionService)
        {
            logger = _logger;
            configuration = _configuration;
            featureManager = _featureManager;
            this.modelVersionService = modelVersionService;

            var redisConnection = configuration["RedisConnectionString"];

            if (redisConnection == default)
            {
                logger.LogError(LogType.Redis, "Redis cache connection is not setup");
                return;
            }

            Initialize();
        }

        private void Initialize()
        {
            try
            {
                connections = new ConcurrentBag<ConcurrentBag<Lazy<ConnectionMultiplexer>>>();
                const int defaultIntConnectionTimeout = 30000;
                var connectRetry = int.TryParse(configuration["RedisConnectRetries"], out var retries) ? retries : 1;
                var abortOnFail = (configuration["RedisAbortOnConnectFail"] ?? "False") == bool.TrueString;
                var connectTimeout = int.TryParse(configuration["RedisConnectionTimeout"], out var connectionTimeout) ? connectionTimeout : defaultIntConnectionTimeout;
                var connectionStrings = configuration["RedisConnectionString"].Split(';');
                var poolSize = int.TryParse(configuration["RedisPoolSize"], out var size) ? size : 5;

                foreach (var connectionString in connectionStrings)
                {
                    var options = ConfigurationOptions.Parse(connectionString);
                    options.ConnectRetry = connectRetry;
                    options.AbortOnConnectFail = abortOnFail;
                    options.ConnectTimeout = connectTimeout;
                    options.Ssl = true;
                    options.SslProtocols = SslProtocols.Tls12;
                    options.ReconnectRetryPolicy = new ExponentialRetry(5000, 10000);

                    logger.LogInformation($"Redis connection options: connectRetry:{connectRetry}, connectTimeout:{connectTimeout}.");

                    var connection = new ConcurrentBag<Lazy<ConnectionMultiplexer>>();

                    for (int i = 0; i < poolSize; i++)
                    {
                        connection.Add(new Lazy<ConnectionMultiplexer>(() =>
                            ConnectionMultiplexer.Connect(options)));
                    }

                    connections.Add(connection);
                }
            }
            catch (Exception e)
            {
                logger.LogError(LogType.Redis, e.Message);
            }
        }

        private static IConnectionMultiplexer GetConnection(ConcurrentBag<Lazy<ConnectionMultiplexer>> connection)
        {
            var loadedLazys = connection.Where(lazy => lazy.IsValueCreated);

            var response = loadedLazys.Count() == connection.Count
                ? connection.OrderBy(x => x.Value.GetCounters().TotalOutstanding).First()
                : connection.First(lazy => !lazy.IsValueCreated);

            return response.Value;
        }

        private static IDatabase GetDatabase(ConcurrentBag<Lazy<ConnectionMultiplexer>> connection) => GetConnection(connection).GetDatabase();

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
            if (!await featureManager.IsEnabledAsync(FeatureFlags.RedisEnabled))
            {
                return true; //Successfully decided to not use Redis at all
            }

            bool IsSuccess = false;
            var redisKey = BuildRedisKey(key);

            try
            {
                var tasks = new List<Task<bool>>();

                foreach (var connection in connections)
                {
                    IDatabase db = GetDatabase(connection);
                    if (db.IsConnected(redisKey))
                    {
                        tasks.Add(db.StringSetAsync(redisKey, JsonConvert.SerializeObject(value),
                            GetExpirationTime(redisLifeSpanLevel)));

                        logger.LogDebug(LogType.Redis, $"Key {redisKey} added to task list.");
                    }
                    else
                    {
                        logger.LogWarning(LogType.Redis, $"No connection available to add the value for key:'{redisKey}'.");
                    }
                }

                var item = await Task.WhenAll(tasks);
                IsSuccess = item.All(x => x == true);
                logger.LogTraceAndDebug(LogType.Redis, $"Key {redisKey} {(IsSuccess ? string.Empty : "not")} added to Redis cache.");
            }
            catch (RedisConnectionException rex)
            {
                logger.LogWarning(LogType.Redis, $"{nameof(RedisConnectionException)} thrown when attempting to add key {redisKey} to Redis cache. Exception Message: {rex}");
            }
            catch (RedisTimeoutException rex)
            {
                logger.LogWarning(LogType.Redis, $"{nameof(RedisTimeoutException)} thrown when attempting to add key {redisKey} to Redis cache. Exception Message: {rex}");
            }
            catch (Exception ex)
            {
                logger.LogError(LogType.Redis, $"Error in adding key {redisKey} to Redis cache.", ex);
            }
            return IsSuccess;
        }

        public async Task<(T, bool)> GetKeyValueAsync<T>(string key)
        {
            if (!await featureManager.IsEnabledAsync(FeatureFlags.RedisEnabled))
            {
                return (default, false);
            }

            var redisKey = BuildRedisKey(key);

            try
            {
                var tasks = new List<Task<RedisValue>>();

                foreach (var connection in connections)
                {
                    IDatabase db = GetDatabase(connection);

                    if (db.IsConnected(redisKey))
                    {
                        tasks.Add(db.StringGetAsync(redisKey));
                    }
                    else
                    {
                        logger.LogInformation(LogType.Redis, $"No connection available to get the value based on key:{redisKey}");
                    }
                }

                var item = await (await Task.WhenAny(tasks));
                bool IsExist = !item.IsNullOrEmpty;
                logger.LogDebug(LogType.Redis, $"Key {redisKey} {(IsExist ? string.Empty : "not")} found in Redis cache.");
                return IsExist ? (JsonConvert.DeserializeObject<T>(item), true) : (default(T), false);
            }
            catch (RedisConnectionException rex)
            {
                logger.LogWarning(LogType.Redis, $"{nameof(RedisConnectionException)} thrown when attempting to get value from Redis cache for key {redisKey}. Exception Message: '{rex.Message}'.");
                return (default(T), false);
            }
            catch (RedisTimeoutException rex)
            {
                logger.LogWarning(LogType.Redis, $"{nameof(RedisConnectionException)} thrown when attempting to get value from Redis cache for key {redisKey}. Exception Message: '{rex.Message}'.");
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
