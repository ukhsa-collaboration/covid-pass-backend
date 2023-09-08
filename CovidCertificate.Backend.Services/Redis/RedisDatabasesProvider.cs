using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Authentication;
using CovidCertificate.Backend.Interfaces.Redis;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CovidCertificate.Backend.Services.Redis
{
    [ExcludeFromCodeCoverage]
    public class RedisDatabasesProvider : IRedisDatabasesProvider
    {
        private const bool DefaultAbortOnFail = true;
        private const bool DefaultEnableSSL = true;
        private const int DefaultConnectionTimeout = 5000;
        private const int DefaultConnectionRetry = 3;
        private const int DefaultKeepAlive = 120;
        private const SslProtocols DefaultSslProtocols = SslProtocols.Tls12;
        private readonly IReconnectRetryPolicy RetryPolicy = new ExponentialRetry(3000, 5000);

        private readonly List<ConnectionMultiplexer> connectionMultiplexers = new();
        private readonly ILogger<RedisDatabasesProvider> logger;

        public RedisDatabasesProvider(
            ILogger<RedisDatabasesProvider> logger,
            IConfiguration configuration,
            IMultiplexerEventHandler multiplexerEventHandler)
        {
            this.logger = logger;

            try
            {
                InitializeMultiplexers(configuration, multiplexerEventHandler);
            }
            catch (Exception e)
            {
                logger.LogError(LogType.Redis, $"Error during initializing Redis multiplexers, ex message: '{e.Message}'.");
                throw;
            }
        }

        public List<IDatabase> GetDatabases()
        {
            logger.LogInformation($"GetDatabases was invoked ({nameof(RedisDatabasesProvider)})");

            var databases = connectionMultiplexers.Select(x => x.GetDatabase()).ToList();
            return databases;
        }

        private void InitializeMultiplexers(IConfiguration configuration, IMultiplexerEventHandler multiplexerEventHandler)
        {
            logger.LogInformation("Multiplexers initialization started.");

            var abortOnFail = bool.TryParse(configuration["RedisAbortOnConnectFail"], out var _abortOnFail)
                ? _abortOnFail
                : DefaultAbortOnFail;
            var connectRetry = int.TryParse(configuration["RedisConnectRetries"], out var retries) ? retries : DefaultConnectionRetry;
            var connectionTimeout = int.TryParse(configuration["RedisConnectionTimeout"], out var connectTimeout)
                ? connectTimeout
                : DefaultConnectionTimeout;
            var keepAlive = DefaultKeepAlive;
            var clientName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "<EmptyWEBSITE_SITE_NAME>";

            var connectionStrings = configuration["RedisConnectionString"].Split(';');

            foreach (var connectionString in connectionStrings)
            {
                var options = ConfigurationOptions.Parse(connectionString);
                options.AbortOnConnectFail = abortOnFail;
                options.ConnectRetry = connectRetry;
                options.ConnectTimeout = connectionTimeout;
                options.KeepAlive = keepAlive;
                options.ClientName = clientName;
                options.Ssl = DefaultEnableSSL;
                options.SslProtocols = DefaultSslProtocols;
                options.ReconnectRetryPolicy = RetryPolicy;

                logger.LogInformation(LogType.Redis, $"Redis connection options: '{JsonConvert.SerializeObject(options)}'.");

                var multiplexer = ConnectionMultiplexer.Connect(options);

                multiplexer.ConfigurationChanged += multiplexerEventHandler.ConfigurationChanged;
                multiplexer.ConfigurationChangedBroadcast += multiplexerEventHandler.ConfigurationChangedBroadcast;
                multiplexer.ConnectionFailed += multiplexerEventHandler.ConnectionFailed;
                multiplexer.ErrorMessage += multiplexerEventHandler.ErrorMessage;
                multiplexer.ConnectionRestored += multiplexerEventHandler.ConnectionRestored;
                multiplexer.InternalError += multiplexerEventHandler.InternalError;

                connectionMultiplexers.Add(multiplexer);
            }
        }
    }
}
