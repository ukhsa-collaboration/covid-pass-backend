using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.International;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Services;
using CovidCertificate.Backend.Services.KeyServices;
using CovidCertificate.Backend.Services.SecurityServices;
using CovidCertificate.Backend.Configuration.Bases;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using CovidCertificate.Backend.Services.Mappers;
using CovidCertificate.Backend.Configuration.Extensions;
using CovidCertificate.Backend.Services.QrCodes;
using CovidCertificate.Backend.Services.PdfGeneration;
using CovidCertificate.Backend.NhsApiIntegration.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.NhsApiIntegration.Services;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Services.Certificates;
using CovidCertificate.Backend.Configuration.DIExtensions;
using CovidCertificate.Backend.Interfaces.PdfLimiters;
using CovidCertificate.Backend.Services.PdfLimiters;
using CovidCertificate.Backend.Interfaces.BlobService;
using CovidCertificate.Backend.Configuration.Bases.ValidationService;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Interfaces.International;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Services.AzureServices;
using CovidCertificate.Backend.Services.Certificates.UVCI;
using CovidCertificate.Backend.Services.International;

[assembly: FunctionsStartup(typeof(Startup))]

namespace CovidCertificate.Backend.International
{
    [ExcludeFromCodeCoverage]
    public class Startup : StartupBase
    {
        public override void SetupFunctionSpecificSettings(IFunctionsHostBuilder builder)
        {
            builder.AddSetting<NhsLoginSettings>(Configuration, "NhsLoginConfig");
            builder.AddSetting<HtmlGeneratorSettings>(Configuration, "HtmlGeneratorSettings");
            builder.AddSetting<CovidJwtSettings>(Configuration, "CovidSecuritySettings");
            builder.AddSetting<NhsTestResultsHistoryApiSettings>(Configuration, "NhsTestResultsHistoryApiSettings");
            builder.AddSetting<RetryPolicySettings>(Configuration, "RetryPolicySettings");
        }

        public override void SetupFunctionSpecificDependencyInjection(IFunctionsHostBuilder builder)
        {
            builder.Services.AddEndpointValidationServices();

            builder.Services.AddSingleton<ICBORFlow, CBORFlow>();
            builder.Services.AddSingleton<ICondensorService, CondensorService>();
            builder.Services.AddSingleton<IEncoderService, EncoderService>();
            builder.Services.AddSingleton<IVaccinationMapper, VaccinationMapper>();
            builder.Services.AddSingleton<IVaccineService, VaccineService>();
            builder.Services.AddSingleton<IVaccineFilterService, VaccineFilterService>();
            builder.Services.AddSingleton<IQrImageGenerator, QrImageGenerator>();
            builder.Services.AddSingleton<IQRCodeGenerator, QRCodeGenerator>();
            builder.Services.AddSingleton<IHtmlGeneratorService, HtmlGeneratorService>();
            builder.Services.AddSingleton<INhsLoginService, NhsLoginService>();
            builder.Services.AddSingleton(typeof(IMongoRepository<>), typeof(MongoRepository<>));
            builder.Services.AddSingleton<IBlobService, BlobService>();
            builder.Services.AddSingleton<INhsKeyRing, NhsKeyRing>();
            builder.Services.AddSingleton<HtmlGeneratorSettings>();
            builder.Services.AddSingleton<IPdfGeneratorService, PdfHtmlGeneratorService>();
            builder.Services.AddSingleton<INhsTestResultsHistoryApiAccessTokenService, NhsTestResultsHistoryApiAccessTokenService>();
            builder.Services.AddSingleton<IDiagnosticTestResultsService, DiagnosticTestResultsService>();
            builder.Services.AddSingleton<INhsdFhirApiService, NhsdFhirApiService>();
            builder.Services.AddSingleton<IFhirBundleMapper<TestResultNhs>, DiagnosticTestFhirBundleMapper>();
            builder.Services.AddSingleton<IConfigurationValidityCalculator, ConfigurationValidityCalculator>();
            builder.Services.AddSingleton<IIneligibilityService, IneligibilityService>();
            builder.Services.AddSingleton<IGetTimeZones, GetTimeZones>();
            builder.Services.AddSingleton(typeof(IUVCIRepository<>), typeof(UVCIRepository<>));
            builder.Services.AddSingleton<IDomesticUVCIGenerator, DomesticUVCIGenerator>();
            builder.Services.AddSingleton<IRegionUVCIGenerator, RegionUVCIGenerator>();
            builder.Services.AddSingleton<IUVCIGeneratorService, UVCIGeneratorService>();
            builder.Services.AddSingleton<ICovidCertificateService, CovidCertificateService>();
            builder.Services.AddSingleton<ICovidCertificateBuilder, CovidCertificateBuilder>();
            builder.Services.AddSingleton<IPublicKeyService, PublicKeyService>();
            builder.Services.AddSingleton<IEmailLimiter, EmailLimiter>();
            builder.Services.AddSingleton<IPdfContentGenerator, PdfContentGenerator>();
            builder.Services.AddQRCodeSigningServices();
            builder.Services.AddSingleton<IEligibilityConfigurationService, EligibilityConfigurationService>();
            builder.Services.AddSingleton<IInternationalPdfLimiter, InternationalPdfLimiter>();
            builder.Services.AddSingleton<ICovidResultsService, CovidResultsService>();
            builder.Services.AddSingleton<IProofingLevelValidatorService, ProofingLevelValidatorService>();

            builder.Services.AddSingleton<IBoosterValidityService, BoosterValidityService>();
            builder.Services.AddSingleton<IPostEndpointValidationService, PostEndpointValidationService>();
            builder.Services.AddSingleton<IUnattendedSecurityService, UnattendedSecurityService>();
            builder.Services.AddSingleton<IInternationalCertificateWrapper, InternationalCertificateWrapper>();
        }
    }
}
