
using CovidCertificate.Backend.Models.Settings;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CovidCertificate.Backend.Configuration.Extensions
{
    public static class StartupExtensions
    {
        public static void AddSetting<T>(this IFunctionsHostBuilder builder, IConfiguration configuration, string settingSection) where T : class, new()
        {
            var setting = new T();
            configuration.GetSection(settingSection).Bind(setting);

            builder.Services.AddSingleton(setting);
        }

        public static void AddSetting<T>(this IFunctionsHostBuilder builder, IConfiguration configuration, string settingSection, string vaultKey) where T : BaseSettings
        {
            var setting = BaseTypeFactory.BuildBaseType<T>(configuration, vaultKey);
            configuration.GetSection(settingSection).Bind(setting);

            builder.Services.AddSingleton<T>(setting);
        }

    }
}
