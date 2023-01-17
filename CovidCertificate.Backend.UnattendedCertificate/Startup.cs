using CovidCertificate.Backend.Configuration.Bases;
using CovidCertificate.Backend.Configuration.DIExtensions;
using CovidCertificate.Backend.Configuration.Extensions;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Interfaces.International;
using CovidCertificate.Backend.Interfaces.JwtServices;
using CovidCertificate.Backend.Interfaces.TwoFactor;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.NhsApiIntegration.Interfaces;
using CovidCertificate.Backend.NhsApiIntegration.Models;
using CovidCertificate.Backend.NhsApiIntegration.Services;
using CovidCertificate.Backend.Services;
using CovidCertificate.Backend.Services.AzureServices;
using CovidCertificate.Backend.Services.Certificates;
using CovidCertificate.Backend.Services.Certificates.UVCI;
using CovidCertificate.Backend.Services.GracePeriodServices;
using CovidCertificate.Backend.Services.International;
using CovidCertificate.Backend.Services.Mappers;
using CovidCertificate.Backend.Services.Notifications;
using CovidCertificate.Backend.Services.PdfGeneration;
using CovidCertificate.Backend.Services.QrCodes;
using CovidCertificate.Backend.Services.SecurityServices;
using CovidCertificate.Backend.UnattendedCertificate;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace CovidCertificate.Backend.UnattendedCertificate
{
    public class Startup : StartupBase
    {
        public override void SetupFunctionSpecificSettings(IFunctionsHostBuilder builder)
        {
            builder.AddSetting<CovidJwtSettings>(Configuration, "CovidSecuritySettings");
            builder.AddSetting<HtmlGeneratorSettings>(Configuration, "HtmlGeneratorSettings");
            builder.AddSetting<PassSettings>(Configuration, "PassSettings");
            builder.AddSetting<NhsLoginSettings>(Configuration, "NhsLoginConfig");
            builder.AddSetting<NhsTestResultsHistoryApiSettings>(Configuration, "NhsTestResultsHistoryApiSettings");
            builder.AddSetting<DomesticExemptionSettings>(Configuration, "DomesticExemptionsSettings");
            builder.AddSetting<GracePeriodSettings>(Configuration, "GracePeriodSettings");
            builder.AddSetting<DomesticQRValues>(Configuration, "DomesticQRValues");
            builder.AddSetting<DomesticPolicy>(Configuration, "DomesticPolicy");
            builder.AddSetting<RetryPolicySettings>(Configuration, "RetryPolicySettings");
            builder.AddSetting<MedicalExemptionApiSettings>(Configuration, "MedicalExemptionApiSettings");
            builder.AddSetting<NotificationTemplates>(Configuration, "NotificationTemplates");
        }

        public override void SetupFunctionSpecificDependencyInjection(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IQRCodeGenerator, QRCodeGenerator>();
            builder.Services.AddSingleton<IJwtGenerator, JwtGeneratorService>();
            builder.Services.AddSingleton<ICovidCertificateService, CovidCertificateService>();
            builder.Services.AddSingleton<ICovidCertificateBuilder, CovidCertificateBuilder>();
            builder.Services.AddSingleton<ICovidResultsService, CovidResultsService>();
            builder.Services.AddSingleton<IQrImageGenerator, QrImageGenerator>();
            builder.Services.AddSingleton<IVaccineService, VaccineService>();
            builder.Services.AddSingleton<IVaccineFilterService, VaccineFilterService>();
            builder.Services.AddSingleton<IDiagnosticTestResultsService, DiagnosticTestResultsService>();
            builder.Services.AddSingleton<IVaccinationMapper, VaccinationMapper>();
            builder.Services.AddSingleton<IGeneratePassData, GeneratePassData>();
            builder.Services.AddSingleton<IEncoderService, EncoderService>();
            builder.Services.AddSingleton<ICondensorService, CondensorService>();
            builder.Services.AddSingleton<ICBORFlow, CBORFlow>();
            builder.Services.AddSingleton<ITestResultFilter, TestResultFilter>();
            builder.Services.AddSingleton<IProofingLevelValidatorService, ProofingLevelValidatorService>();

            builder.Services.AddSingleton(typeof(IUVCIRepository<>), typeof(UVCIRepository<>));
            builder.Services.AddSingleton<IDomesticUVCIGenerator, DomesticUVCIGenerator>();
            builder.Services.AddSingleton<IRegionUVCIGenerator, RegionUVCIGenerator>();
            builder.Services.AddSingleton<IUVCIGeneratorService, UVCIGeneratorService>();
            builder.Services.AddSingleton<IEligibilityConfigurationService, EligibilityConfigurationService>();

            builder.Services.AddDomesticExemptionServices();
            builder.Services.AddQRCodeSigningServices();
            builder.Services.AddSingleton<INhsTestResultsHistoryApiAccessTokenService, NhsTestResultsHistoryApiAccessTokenService>();
            builder.Services.AddSingleton<INhsdFhirApiService, NhsdFhirApiService>();
            builder.Services.AddSingleton<IFhirBundleMapper<TestResultNhs>, DiagnosticTestFhirBundleMapper>();
            builder.Services.AddSingleton<IPdfGeneratorService, PdfHtmlGeneratorService>();
            builder.Services.AddSingleton<IConfigurationValidityCalculator, ConfigurationValidityCalculator>();
            builder.Services.AddSingleton<IIneligibilityService, IneligibilityService>();
            builder.Services.AddSingleton<IGetTimeZones, GetTimeZones>();
            builder.Services.AddSingleton<IUserConfigurationService, UserConfigurationService>();
            builder.Services.AddSingleton<IUserPreferenceService, UserPreferenceService>();
            builder.Services.AddSingleton<IUserPolicyService, UserPolicyService>();
            builder.Services.AddSingleton<IGracePeriodService, GracePeriodService>();
            builder.Services.AddSingleton<IGracePeriodCache, GracePeriodCache>();
            builder.Services.AddSingleton<IBoosterValidityService, BoosterValidityService>();
            builder.Services.AddSingleton<TelemetryClient, TelemetryClient>();
            builder.Services.AddSingleton<IUnattendedSecurityService, UnattendedSecurityService>();
            builder.Services.AddSingleton<IInternationalCertificateWrapper, InternationalCertificateWrapper>();
            builder.Services.AddSingleton<IDomesticCertificateWrapper, DomesticCertificateWrapper>();
            builder.Services.AddSingleton<IPdfContentGenerator, PdfContentGenerator>();
            builder.Services.AddSingleton<IEmailService, NHSEmailService>();
            builder.Services.AddSingleton<IQueueService, ServiceBusQueueService>();
        }
    }
}
