using CovidCertificate.Backend.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Reflection;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.BlobService;
using Microsoft.FeatureManagement;
using CovidCertificate.Backend.Configuration.DIExtensions;
using CovidCertificate.Backend.Configuration.Extensions;
using CovidCertificate.Backend.Interfaces.ManagementInformation;
using CovidCertificate.Backend.Interfaces.ModelVersioning;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Services.AzureServices;
using CovidCertificate.Backend.Services.InMemoryCache;
using CovidCertificate.Backend.Services.ManagementInformation;
using CovidCertificate.Backend.Services.ModelVersioning;
using CovidCertificate.Backend.PKINationalBackend.Models;
using CovidCertificate.Backend.Interfaces.PKINationaBackend;
using CovidCertificate.Backend.Services.PKINationaBackend;
using System.Runtime.InteropServices;
using CovidCertificate.Backend.Interfaces.DateTimeProvider;
using CovidCertificate.Backend.PKINationalBackend.Services;
using CovidCertificate.Backend.Services.DateTimeProvider;
using CovidCertificate.Backend.Services.Mocks;

namespace CovidCertificate.Backend.Configuration.Bases
{
    public abstract class StartupBase : FunctionsStartup
    {
        protected IConfiguration Configuration { get; set; }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            SetupJsonConvertSettings();
            SetupCommonServices(builder);
            SetupCommonDependencyInjection(builder);
            SetupCommonSettings(builder);
            SetupFunctionSpecificDependencyInjection(builder);
            SetupFunctionSpecificSettings(builder);
        }

        /// <summary>
        /// Use this function for any specific settings you need to set for your application
        /// </summary>
        /// <param name="builder"></param>
        public virtual void SetupFunctionSpecificSettings(IFunctionsHostBuilder builder)
        {
        }

        /// <summary>
        /// Use this function for any specific dependency injection you need to set for your application
        /// </summary>
        /// <param name="builder"></param>
        public virtual void SetupFunctionSpecificDependencyInjection(IFunctionsHostBuilder builder)
        {
        }

        private void SetupCommonServices(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();
            builder.Services.AddApplicationInsightsTelemetry();
            builder.Services.AddAzureAppConfiguration();
            builder.Services.AddFeatureManagement();
            builder.Services.AddHttpClient();
            builder.Services
                .AddSingleton<IManagementInformationReportingService, ManagementInformationReportingService>();
            builder.Services.AddSingleton<IModelVersionService, ModelVersionService>();
            builder.Services.AddMemoryCache();
        }

        private void SetupCommonDependencyInjection(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton(typeof(IBlobFilesInMemoryCache<>), typeof(BlobFilesInMemoryCache<>));
            builder.Services.AddSingleton<IBlobService, BlobService>();
            builder.Services.AddSingleton<IQueueService, ServiceBusQueueService>();
            builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
            builder.Services.AddSingleton<IMemoryCacheService, MemoryCacheService>();
            builder.Services.AddSingleton<IValueSetService, ValueSetService>();
            builder.Services.AddSingleton<IDateTimeProviderService, DateTimeProviderService>();

            //Can't do mutual TLS on Windows so need to mock it
            var env = Environment.GetEnvironmentVariable("ENVIRONMENT").ToLower();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && env != "production")
            {
                builder.Services.AddSingleton<IDGCGMutualTLSService, MockMutualTLSService>();
            }
            else
            {
                builder.Services.AddSingleton<IDGCGMutualTLSService, DGCGMutualTLSService>();
            }

            builder.Services.AddConfigurationRefresher();
            builder.Services.AddMongoDbServices(Configuration);
            builder.Services.AddJwtValidatorServices();
        }

        private void SetupCommonSettings(IFunctionsHostBuilder builder)
        {
            builder.AddSetting<MongoDbSettings>(Configuration, "MongoDbSettings");
            builder.AddSetting<BlobServiceSettings>(Configuration, "BlobSettings");
            builder.AddSetting<NhsLoginSettings>(Configuration, "NhsLoginConfig");
            builder.AddSetting<DGCGSettings>(Configuration, "DGCGConfig");
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var environmentName = Environment.GetEnvironmentVariable("ENVIRONMENT").ToLower();
            var basePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "Settings");
            builder.ConfigurationBuilder.SetBasePath(basePath);

            if (environmentName == "local")
            {
                AddLocalConfiguration(builder);
            }
            else
            {
                AddConfigurationFromEnvironment(builder);
            }

            Configuration = builder.ConfigurationBuilder.Build();
        }

        private void AddLocalConfiguration(IFunctionsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder.AddJsonFile("appConfigurationLocal.json");
            builder.ConfigurationBuilder.AddJsonFile("FeatureManagement.json", false);
            builder.ConfigurationBuilder.AddJsonFile("secrets.json");
        }

        private void AddConfigurationFromEnvironment(IFunctionsConfigurationBuilder builder)
        {
            builder.AddAzureAppConfiguration();
            builder.AddKeyVaultServices();
        }

        private static void SetupJsonConvertSettings()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
        }
    }
}
