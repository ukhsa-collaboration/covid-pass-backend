using CovidCertificate.Backend.Configuration.Bases;
using CovidCertificate.Backend.Configuration.DIExtensions;
using CovidCertificate.Backend.Configuration.Extensions;
using CovidCertificate.Backend.IngestionPipelines;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Interfaces.DomesticExemptions;
using CovidCertificate.Backend.Interfaces.TwoFactor;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Services;
using CovidCertificate.Backend.Services.Certificates;
using CovidCertificate.Backend.Services.DomesticExemptions;
using CovidCertificate.Backend.Services.Notifications;
using CovidCertificate.Backend.Services.PdfGeneration;
using CovidCertificate.Backend.Services.QrCodes;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(IngestionStartup))]

namespace CovidCertificate.Backend.IngestionPipelines
{
    public class IngestionStartup : StartupBase
    {
        public override void SetupFunctionSpecificSettings(IFunctionsHostBuilder builder)
        {
            builder.AddSetting<HtmlGeneratorSettings>(Configuration, "HtmlGeneratorSettings");
            builder.AddSetting<NotificationTemplates>(Configuration, "NotificationTemplates");
            builder.AddSetting<DomesticExemptionSettings>(Configuration, "DomesticExemptionSettings");
            builder.AddSetting<OdsApiSettings>(Configuration, "OdsApiSettings");
        }

        public override void SetupFunctionSpecificDependencyInjection(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IEmailService, NHSEmailService>();
            builder.Services.AddSingleton<ITestResultFilter, TestResultFilter>();
            builder.Services.AddScoped<IHtmlGeneratorService, HtmlGeneratorService>();
            builder.Services.AddScoped<IPdfGeneratorService, PdfHtmlGeneratorService>();
            builder.Services.AddScoped<IQRCodeGenerator, QRCodeGenerator>();
            builder.Services.AddScoped<IQrImageGenerator, QrImageGenerator>();
            builder.Services.AddScoped<IGetTimeZones, GetTimeZones>();
            builder.Services.AddScoped<IDomesticExemptionsParsingService, DomesticExemptionsParsingService>();
            builder.Services.AddScoped<IDomesticExemptionsValidationService, DomesticExemptionsValidationService>();
            builder.Services.AddScoped<ICsvToDomesticExemptionsParsingService, CsvToDomesticExemptionsParsingService>();
            builder.Services.AddScoped<IDomesticExemptionRecordsService, DomesticExemptionCosmosService>();
            builder.Services.AddScoped<IPdfContentGenerator, PdfContentGenerator>();
            builder.Services.AddScoped<IOdsApiService, OdsApiService>();
            builder.Services.AddScoped<IUpdateOrganisationsService, UpdateOrganisationsService>();
            builder.Services.AddQRCodeSigningServices();
        }
    }
}
