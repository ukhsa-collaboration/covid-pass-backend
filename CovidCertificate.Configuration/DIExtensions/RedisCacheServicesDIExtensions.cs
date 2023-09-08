using CovidCertificate.Backend.Interfaces.Redis;
using CovidCertificate.Backend.Services.Redis;
using Microsoft.Extensions.DependencyInjection;

namespace CovidCertificate.Backend.Configuration.DIExtensions
{
    public static class RedisCacheServicesDIExtensions
    {
        public static void AddRedisCacheServices(this IServiceCollection services)
        {
            // Old Redis Service implementation
            services.AddSingleton<IRedisCacheService, RedisCacheServiceOld>();
        }

        public static void AddNewRedisCacheServices(this IServiceCollection services)
        {
            // New Redis service implementation - temporary commented
            services.AddSingleton<IMultiplexerEventHandler, MultiplexerEventHandler>();
            services.AddSingleton<IRedisDatabasesProvider, RedisDatabasesProvider>();
            services.AddSingleton<IRedisCacheService, RedisCacheService>();
        }
    }
}
