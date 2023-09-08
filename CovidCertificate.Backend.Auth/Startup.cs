using CovidCertificate.Backend.Auth;
using CovidCertificate.Backend.Configuration.Bases;
using CovidCertificate.Backend.Configuration.Bases.ValidationService;
using CovidCertificate.Backend.Configuration.Extensions;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Services;
using CovidCertificate.Backend.Services.KeyServices;
using CovidCertificate.Backend.Services.SecurityServices;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Interfaces.JwtServices;
using CovidCertificate.Backend.Services.GracePeriodServices;
using CovidCertificate.Backend.Interfaces.TwoFactor;
using CovidCertificate.Backend.Services.Notifications;
using CovidCertificate.Backend.Configuration.DIExtensions;

[assembly: FunctionsStartup(typeof(Startup))]

namespace CovidCertificate.Backend.Auth
{
    [ExcludeFromCodeCoverage]
    public class Startup : StartupBase
    {
        public override void SetupFunctionSpecificSettings(IFunctionsHostBuilder builder)
        {
            builder.AddSetting<NhsLoginSettings>(Configuration, "NhsLoginConfig");
            builder.AddSetting<GracePeriodSettings>(Configuration, "GracePeriodSettings");
            builder.AddSetting<NhsTestResultsHistoryApiSettings>(Configuration, "NhsTestResultsHistoryApiSettings");
            builder.AddSetting<RetryPolicySettings>(Configuration, "RetryPolicySettings");
        }

        public override void SetupFunctionSpecificDependencyInjection(IFunctionsHostBuilder builder)
        {
            builder.Services.AddEndpointValidationServices();

            builder.Services.AddSingleton<INhsLoginService, NhsLoginService>();
            builder.Services.AddSingleton<ISmsService, NHSSmsService>();
            builder.Services.AddScoped<IAssertedLoginIdentityService, AssertedLoginIdentityService>();
            builder.Services.AddScoped<IJwtGenerator, JwtGeneratorService>();
            builder.Services.AddSingleton<INhsKeyRing, NhsKeyRing>();
            builder.Services.AddSingleton<IPublicKeyService, PublicKeyService>();
            builder.Services.AddSingleton<IUserConfigurationService, UserConfigurationService>();
            builder.Services.AddSingleton<IUserPreferenceService, UserPreferenceService>();
            builder.Services.AddSingleton<IUserPolicyService, UserPolicyService>();
            builder.Services.AddSingleton<IGracePeriodCache, GracePeriodCache>();
            builder.Services.AddSingleton<IGracePeriodService, GracePeriodService>();
            builder.Services.AddSingleton<IPostEndpointValidationService, PostEndpointValidationService>();
        }
    }
}
