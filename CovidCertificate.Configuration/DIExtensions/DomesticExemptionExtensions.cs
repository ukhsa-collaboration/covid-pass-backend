using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Interfaces.DomesticExemptions;
using CovidCertificate.Backend.Services.Certificates;
using CovidCertificate.Backend.Services.DomesticExemptions;
using Microsoft.Extensions.DependencyInjection;

namespace CovidCertificate.Backend.Configuration.DIExtensions
{
    public static class DomesticExemptionExtensions
    {
        public static void AddDomesticExemptionServices(this IServiceCollection services)
        {
            services.AddSingleton<IDomesticExemptionService, DomesticExemptionService>();
            services.AddSingleton<IDomesticExemptionCertificateGenerator, DomesticExemptionCertificateGenerator>();
            services.AddSingleton<IDomesticExemptionCache, DomesticExemptionCache>();
            services.AddSingleton<IDomesticExemptionsParsingService, DomesticExemptionsParsingService>();
            services.AddSingleton<IDomesticExemptionsValidationService, DomesticExemptionsValidationService>();
            services.AddSingleton<ICsvToDomesticExemptionsParsingService, CsvToDomesticExemptionsParsingService>();
            services.AddSingleton<IClinicalTrialExemptionService, ClinicalTrialExemptionService>();
            services.AddSingleton<IMedicalExemptionService, MedicalExemptionServiceMock>();
            services.AddSingleton<IDomesticExemptionRecordsService, DomesticExemptionCosmosService>();
            services.AddSingleton<IMiscExemptionService, MiscExemptionService>();
        }
    }
}
