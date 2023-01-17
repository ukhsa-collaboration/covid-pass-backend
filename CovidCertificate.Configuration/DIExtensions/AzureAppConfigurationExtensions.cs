using System;
using Azure.Identity;
using CovidCertificate.Backend.Services.Stubs;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;

namespace CovidCertificate.Backend.Configuration.DIExtensions
{
    public static class AppConfigurationExtensions
    {
        private static IConfigurationRefresher ConfigurationRefresher { set; get; }

        public static void AddConfigurationRefresher(this IServiceCollection services)
        {
            var environmentName = Environment.GetEnvironmentVariable("ENVIRONMENT")?.ToLower();

            if (environmentName == "local")
                services.AddSingleton<IConfigurationRefresher, ConfigurationRefresherStub>();
            else
                services.AddSingleton(ConfigurationRefresher);
        }

        public static void AddKeyVaultServices(this IFunctionsConfigurationBuilder builder)
        {
            var keyVaultUrl = Environment.GetEnvironmentVariable("VaultUri");

            builder.ConfigurationBuilder.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());
        }

        public static void AddAzureAppConfiguration(this IFunctionsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder.AddAzureAppConfiguration(options =>
            {
                // Set up managed identity
                var credentials = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeSharedTokenCacheCredential = true });
                options.Connect(new Uri(Environment.GetEnvironmentVariable("ConfigurationUri")), credentials)
                    // Load all keys with no label filter (should only be those that are similar for all envs)
                    .Select(KeyFilter.Any, LabelFilter.Null)
                    .UseFeatureFlags(featureFlagOptions =>
                    {
                        featureFlagOptions.CacheExpirationInterval = TimeSpan.FromMinutes(2);
                    })
                    .ConfigureKeyVault(kv =>
                    {
                        kv.SetCredential(credentials);
                    })
                    // Configure to reload configuration if the registered 'Sentinel' key is modified
                    .ConfigureRefresh(refreshOptions =>
                        refreshOptions.Register("AppConfiguration:SentinelKey", refreshAll: true)
                            .SetCacheExpiration(TimeSpan.FromSeconds(60)));

                ConfigurationRefresher = options.GetRefresher();
            });
        }
    }
}
