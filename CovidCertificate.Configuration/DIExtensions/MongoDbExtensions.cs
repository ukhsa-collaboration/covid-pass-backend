using System;
using System.Security.Authentication;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace CovidCertificate.Backend.Configuration.DIExtensions
{
    public static class MongoDbExtensions
    {
        public static void AddMongoDbServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IMongoClient>(serviceProvider =>
            {
                string connectionString = configuration["CosmosDbConnectionString"];
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentNullException("Mongo Config string is null or empty");
                }

                var url = new MongoUrl(connectionString);
                var settings = MongoClientSettings.FromUrl(url);
                settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
                settings.ReadPreference = ReadPreference.Nearest;
                return new MongoClient(settings);
            });

            services.AddSingleton(typeof(IMongoRepository<>), typeof(MongoRepository<>));
        }
    }
}


