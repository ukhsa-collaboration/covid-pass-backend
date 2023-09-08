using System.Diagnostics.CodeAnalysis;
using CovidCertificate.Backend.Interfaces.Redis;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CovidCertificate.Backend.Services.Redis
{
    [ExcludeFromCodeCoverage]
    public class MultiplexerEventHandler : IMultiplexerEventHandler
    {
        private readonly ILogger<MultiplexerEventHandler> logger;

        public MultiplexerEventHandler(ILogger<MultiplexerEventHandler> logger)
        {
            this.logger = logger;
        }

        public void ConfigurationChanged(object sender, EndPointEventArgs e)
        {
            logger.LogInformation(LogType.Redis, $"Multiplexer ConfigurationChanged sender: '{sender}', endpoint: '{e.EndPoint}'.");
        }

        public void ConfigurationChangedBroadcast(object sender, EndPointEventArgs e)
        {
            logger.LogInformation(LogType.Redis, $"Multiplexer ConfigurationChangedBroadcast sender: '{sender}', endpoint: '{e.EndPoint}'.");
        }

        public void ConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            logger.LogError(LogType.Redis, $"Multiplexer ConnectionFailed sender: '{sender}', endpoint: '{e.EndPoint}', failureType: '{e.FailureType}'.");
        }

        public void ConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            logger.LogInformation(LogType.Redis, $"Multiplexer ConnectionRestored sender: {sender}, endpoint: '{e.EndPoint}'.");
        }

        public void ErrorMessage(object sender, RedisErrorEventArgs e)
        {
            logger.LogError(LogType.Redis, $"Multiplexer ErrorMessage error sender: '{sender}', endpoint: '{e.EndPoint}' redisErrorMessage: '{e.Message}'");
        }

        public void InternalError(object sender, InternalErrorEventArgs e)
        {
            logger.LogError(LogType.Redis, $"Multiplexer InternalError sender: '{sender}', endpoint: '{e.EndPoint}', origin: '{e.Origin}', exception message: '{e.Exception?.Message}'.");
        }
    }
}
