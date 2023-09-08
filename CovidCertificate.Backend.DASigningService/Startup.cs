using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Services;
using CovidCertificate.Backend.Configuration.Bases;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using CovidCertificate.Backend.DASigningService;
using CovidCertificate.Backend.Services.Certificates;
using CovidCertificate.Backend.Services.Mappers;
using CovidCertificate.Backend.DASigningService.Interfaces;
using CovidCertificate.Backend.DASigningService.Services;
using CovidCertificate.Backend.DASigningService.Services.Helpers;
using CovidCertificate.Backend.Configuration.DIExtensions;
using CovidCertificate.Backend.Configuration.Extensions;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Interfaces.International;
using CovidCertificate.Backend.Interfaces.PKINationaBackend;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Services.Certificates.UVCI;
using CovidCertificate.Backend.Services.International;
using CovidCertificate.Backend.Services.PKINationaBackend;

[assembly: FunctionsStartup(typeof(Startup))]
namespace CovidCertificate.Backend.DASigningService
{
    [ExcludeFromCodeCoverage]
    public class Startup : StartupBase
    {
        public override void SetupFunctionSpecificDependencyInjection(IFunctionsHostBuilder builder)
        {
            // Add settings (loaded once during startup of application)
            builder.AddSetting<DomesticQRValues>(Configuration, "DomesticQRValues");
            builder.AddSetting<DomesticPolicy>(Configuration, "DomesticPolicy");

            builder.Services.AddSingleton<IVaccinationMapper, VaccinationMapper>();
            builder.Services.AddScoped<DiagnosticTestFhirBundleMapper, DiagnosticTestFhirBundleMapper>();
            builder.Services.AddSingleton<IGetTimeZones, GetTimeZones>();
            builder.Services.AddSingleton(typeof(IUVCIRepository<>), typeof(UVCIRepository<>));
            builder.Services.AddSingleton<IDomesticUVCIGenerator, DomesticUVCIGenerator>();
            builder.Services.AddSingleton<IRegionUVCIGenerator, RegionUVCIGenerator>();
            builder.Services.AddSingleton<IUVCIGeneratorService, UVCIGeneratorService>();
            builder.Services.AddSingleton<IEncoderService, EncoderService>();
            builder.Services.AddSingleton<ICondensorService, CondensorService>();
            builder.Services.AddSingleton<ICBORFlow, CBORFlow>();
            builder.Services.AddSingleton<IThumbprintValidator, ThumbprintValidator>();
            builder.Services.AddScoped<IVaccinationBarcodeGenerator, VaccinationBarcodeGenerator>();
            builder.Services.AddScoped<IRecoveryBarcodeGenerator, RecoveryBarcodeGenerator>();
            builder.Services.AddSingleton<IDomesticBarcodeGenerator, DomesticBarcodeGenerator>();
            builder.Services.AddScoped<ITestResultBarcodeGenerator, TestResultBarcodeGenerator>();
            builder.Services.AddScoped<IBarcodeGenerator, BarcodeGenerator>();
            builder.Services.AddSingleton<IRegionConfigService, RegionConfigService>();
            builder.Services.AddScoped<ILogService, LogService>();
            builder.Services.AddQRCodeSigningServices();
            builder.Services.AddSingleton(typeof(IBlobFilesInMemoryCache<>), typeof(BlobFilesInMemoryCache<>));            
            builder.Services.AddSingleton<IQRCodeGenerator, QRCodeGenerator>();
            builder.Services.AddSingleton<INationalBackendService, NationalBackendService>();
        }
    }
}
