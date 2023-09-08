using CovidCertificate.Backend;
using CovidCertificate.Backend.Configuration.Bases;
using CovidCertificate.Backend.Configuration.Extensions;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Services;
using CovidCertificate.Backend.Services.Certificates;
using CovidCertificate.Backend.Services.PdfGeneration;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using CovidCertificate.Backend.NhsApiIntegration.Interfaces;
using CovidCertificate.Backend.NhsApiIntegration.Services;
using CovidCertificate.Backend.Services.KeyServices;
using CovidCertificate.Backend.Services.Mappers;
using CovidCertificate.Backend.Services.QrCodes;
using CovidCertificate.Backend.Services.SecurityServices;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Configuration.DIExtensions;
using CovidCertificate.Backend.Interfaces.PdfLimiters;
using CovidCertificate.Backend.Services.PdfLimiters;
using CovidCertificate.Backend.Configuration.Bases.ValidationService;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Interfaces.International;
using CovidCertificate.Backend.Interfaces.JwtServices;
using CovidCertificate.Backend.Interfaces.TwoFactor;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.NhsApiIntegration.Models;
using CovidCertificate.Backend.Services.Certificates.UVCI;
using CovidCertificate.Backend.Services.GracePeriodServices;
using CovidCertificate.Backend.Services.International;
using CovidCertificate.Backend.Services.Notifications;
using Microsoft.ApplicationInsights;
using System.Diagnostics.CodeAnalysis;

[assembly: FunctionsStartup(typeof(Startup))]

namespace CovidCertificate.Backend
{
    [ExcludeFromCodeCoverage]
    public class Startup : StartupBase
    {
        public override void SetupFunctionSpecificSettings(IFunctionsHostBuilder builder)
        {
            builder.AddSetting<NotificationTemplates>(Configuration, "NotificationTemplates");
            builder.AddSetting<CovidJwtSettings>(Configuration, "CovidSecuritySettings");
            builder.AddSetting<EmailSenderCredentialSettings>(Configuration, "AzureEmailSenderCredential", "AzureEmailSenderCredentialSecret");
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
        }

        public override void SetupFunctionSpecificDependencyInjection(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IQRCodeGenerator, QRCodeGenerator>();
            builder.Services.AddSingleton<IJwtGenerator, JwtGeneratorService>();
            builder.Services.AddSingleton<INhsLoginService, NhsLoginService>();
            builder.Services.AddSingleton<IConfigurationValidityCalculator, ConfigurationValidityCalculator>();
            builder.Services.AddSingleton<ICovidCertificateBuilder, CovidCertificateBuilder>();
            builder.Services.AddSingleton<ICovidResultsService, CovidResultsService>();
            builder.Services.AddSingleton<IGooglePassJwt, GooglePassJwt>();
            builder.Services.AddSingleton<IHtmlGeneratorService, HtmlGeneratorService>();
            builder.Services.AddSingleton<IEmailService, NHSEmailService>();
            builder.Services.AddSingleton<IGenerateApplePass, ApplePassGenerator>();
            builder.Services.AddSingleton<IQrImageGenerator, QrImageGenerator>();
            builder.Services.AddSingleton<IDiagnosticTestResultsService, DiagnosticTestResultsService>();
            builder.Services.AddSingleton<IVaccinationMapper, VaccinationMapper>();
            builder.Services.AddSingleton<IGeneratePassData, GeneratePassData>();
            builder.Services.AddSingleton<IEncoderService, EncoderService>();
            builder.Services.AddSingleton<ICondensorService, CondensorService>();
            builder.Services.AddSingleton<ICBORFlow, CBORFlow>();
            builder.Services.AddSingleton<ITestResultFilter, TestResultFilter>();
            builder.Services.AddSingleton<IProofingLevelValidatorService, ProofingLevelValidatorService>();

            builder.Services.AddSingleton<IJwtValidator, JwtValidator>();
            builder.Services.AddSingleton(typeof(IUVCIRepository<>), typeof(UVCIRepository<>));
            builder.Services.AddSingleton<IDomesticUVCIGenerator, DomesticUVCIGenerator>();
            builder.Services.AddSingleton<IRegionUVCIGenerator, RegionUVCIGenerator>();
            builder.Services.AddSingleton<IUVCIGeneratorService, UVCIGeneratorService>();
            builder.Services.AddSingleton<IEligibilityConfigurationService, EligibilityConfigurationService>();

            builder.Services.AddDomesticExemptionServices();
            builder.Services.AddQRCodeSigningServices();
            builder.Services.AddEndpointValidationServices();

            builder.Services.AddSingleton<IPublicKeyService, PublicKeyService>();
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
            builder.Services.AddSingleton<IGracePeriodCache, GracePeriodCache>();
            builder.Services.AddSingleton<IGracePeriodService, GracePeriodService>();
            builder.Services.AddSingleton<IDomesticPdfLimiter, DomesticPdfLimiter>();
            builder.Services.AddSingleton<IPostEndpointValidationService, PostEndpointValidationService>();
            builder.Services.AddSingleton<IBoosterValidityService, BoosterValidityService>();
            builder.Services.AddSingleton<IVaccineService, VaccineService>();
            builder.Services.AddSingleton<IVaccineFilterService, VaccineFilterService>();
            builder.Services.AddSingleton<ICovidCertificateService, CovidCertificateService>();
            builder.Services.AddSingleton<IEmailLimiter, EmailLimiter>();
            builder.Services.AddSingleton<TelemetryClient, TelemetryClient>();
            builder.Services.AddSingleton<IUnattendedSecurityService, UnattendedSecurityService>();
            builder.Services.AddSingleton<IDomesticCertificateWrapper, DomesticCertificateWrapper>();
            builder.Services.AddSingleton<IInternationalCertificateWrapper, InternationalCertificateWrapper>();

        }
    }
}
