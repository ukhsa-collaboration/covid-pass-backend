using StackExchange.Redis;

namespace CovidCertificate.Backend.Interfaces.Redis
{
    public interface IMultiplexerEventHandler
    {
        void ConfigurationChanged(object sender, EndPointEventArgs e);

        void ConfigurationChangedBroadcast(object sender, EndPointEventArgs e);

        void ConnectionFailed(object sender, ConnectionFailedEventArgs e);

        void ConnectionRestored(object sender, ConnectionFailedEventArgs e);

        void ErrorMessage(object sender, RedisErrorEventArgs e);

        void InternalError(object sender, InternalErrorEventArgs e);
    }
}
