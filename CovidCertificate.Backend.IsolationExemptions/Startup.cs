using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Configuration.Bases;
using CovidCertificate.Backend.Configuration.Bases.ValidationService;
using CovidCertificate.Backend.Configuration.Extensions;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.BlobService;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.IsolationExemptions;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.NhsApiIntegration.Interfaces;
using CovidCertificate.Backend.NhsApiIntegration.Models;
using CovidCertificate.Backend.NhsApiIntegration.Services;
using CovidCertificate.Backend.Services;
using CovidCertificate.Backend.Services.AzureServices;
using CovidCertificate.Backend.Services.Certificates;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using CovidCertificate.Backend.Services.DomesticExemptions;
using CovidCertificate.Backend.Services.Mappers;
using Hl7.Fhir.Model;

[assembly: FunctionsStartup(typeof(Startup))]
namespace CovidCertificate.Backend.IsolationExemptions
{
    public class Startup : StartupBase
    {
        public override void SetupFunctionSpecificSettings(IFunctionsHostBuilder builder)
        {
            builder.AddSetting<NhsTestResultsHistoryApiSettings>(Configuration, "NhsTestResultsHistoryApiSettings");
            builder.AddSetting<DomesticExemptionSettings>(Configuration, "DomesticExemptionSettings");
            builder.AddSetting<BlobServiceSettings>(Configuration, "BlobServiceSettings");
            builder.AddSetting<MongoDbSettings>(Configuration, "MongoDbSettings");
            builder.AddSetting<MedicalExemptionApiSettings>(Configuration, "MedicalExemptionApiSettings");
            builder.AddSetting<RetryPolicySettings>(Configuration, "RetryPolicySettings");
        }

        public override void SetupFunctionSpecificDependencyInjection(IFunctionsHostBuilder builder)
        {
            builder.Services.AddScoped<IMedicalExemptionService, UnattendedMedicalExemptionService>();
            builder.Services.AddSingleton<IDomesticExemptionCache, DomesticExemptionCache>();
            builder.Services.AddScoped<IClinicalTrialExemptionService, ClinicalTrialExemptionService>();
            builder.Services.AddScoped<IDomesticExemptionRecordsService, DomesticExemptionCosmosService>();
            builder.Services.AddScoped<IMedicalExemptionApiService, MedicalExemptionApiService>();
            builder.Services.AddScoped<IMedicalExemptionDataParser, MedicalExemptionParser>();
            builder.Services.AddSingleton<INhsTestResultsHistoryApiAccessTokenService, NhsTestResultsHistoryApiAccessTokenService>();
            builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
            builder.Services.AddSingleton<INhsdFhirApiService, NhsdFhirApiService>();
            builder.Services.AddScoped<IIsolationExemptionStatusService, IsolationExemptionStatusService>();
            builder.Services.AddSingleton<IConfigurationValidityCalculator, ConfigurationValidityCalculator>();
            builder.Services.AddSingleton<IMapper<Bundle, List<Immunization>>, BundleToImmunizationsMapper>();
            builder.Services.AddSingleton<IVaccinationMapper, VaccinationMapper>();
            builder.Services.AddSingleton<IMapper<Bundle, Task<List<Vaccine>>>, BundleToVaccinesMapper>();
            builder.Services.AddSingleton<IBlobService, BlobService>();
            builder.Services.AddSingleton(typeof(IBlobFilesInMemoryCache<>), typeof(BlobFilesInMemoryCache<>));
            builder.Services.AddSingleton(typeof(IMongoRepository<>), typeof(MongoRepository<>));
            builder.Services.AddSingleton<IVaccineService, VaccineService>();
            builder.Services.AddSingleton<IVaccineFilterService, VaccineFilterService>();
            builder.Services.AddSingleton<IEligibilityConfigurationService, EligibilityConfigurationService>();
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IBoosterValidityService, BoosterValidityService>();
            builder.Services.AddSingleton<IPostEndpointValidationService, PostEndpointValidationService>();
            builder.Services.AddSingleton<IUnattendedSecurityService, UnattendedSecurityService>();
        }
    }
}
