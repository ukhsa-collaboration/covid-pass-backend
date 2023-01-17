using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using CovidCertificate.Backend.Configuration.Bases;
using Microsoft.Extensions.DependencyInjection;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.PKINationalBackend.Services;
using CovidCertificate.Backend.Services;
using System.Runtime.InteropServices;
using CovidCertificate.Backend.PKINationalBackend;
using System;
using CovidCertificate.Backend.PKINationalBackend.Models;
using CovidCertificate.Backend.Configuration.Extensions;
using CovidCertificate.Backend.Interfaces.PKINationaBackend;
using CovidCertificate.Backend.Services.Mocks;
using CovidCertificate.Backend.Services.PKINationaBackend;

[assembly: FunctionsStartup(typeof(Startup))]
namespace CovidCertificate.Backend.PKINationalBackend
{
    [ExcludeFromCodeCoverage]
    public class Startup : StartupBase
    {
        public override void SetupFunctionSpecificDependencyInjection(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<ITrustListService, TrustListService>();
            builder.Services.AddSingleton<IValueSetService, ValueSetService>();
            builder.Services.AddSingleton<INationalBackendService, NationalBackendService>();
            builder.Services.AddSingleton<ICertificateCache, CertificateInMemoryCache>();
            builder.Services.AddSingleton<IDomesticPolicyInformationService, DomesticPolicyInformationService>();
        }

        public override void SetupFunctionSpecificSettings(IFunctionsHostBuilder builder)
        {
            builder.AddSetting<DGCGSettings>(Configuration, "DGCGConfig");
        }
    }
}
