using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Services;
using CovidCertificate.Backend.Services.KeyServices;
using Microsoft.Extensions.DependencyInjection;

namespace CovidCertificate.Backend.Configuration.DIExtensions
{
    public static class QRCodeSigningServicesExtensions
    {
        public static void AddQRCodeSigningServices(this IServiceCollection services)
        {
            services.AddSingleton<IKeyRing, CertificateKeyRing>();
            services.AddSingleton<ICertificateCache, CertificateInMemoryCache>();
        }
    }
}
