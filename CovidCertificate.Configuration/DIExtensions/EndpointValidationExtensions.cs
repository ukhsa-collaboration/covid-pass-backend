using CovidCertificate.Backend.Configuration.Bases.ValidationService;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.EndpointValidation;
using CovidCertificate.Backend.Interfaces.TokenValidation;
using CovidCertificate.Backend.Services;
using CovidCertificate.Backend.Services.TokenValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CovidCertificate.Backend.Configuration.DIExtensions
{
    public static class EndpointValidationExtensions
    {
        public static void AddEndpointValidationServices(this IServiceCollection services)
        {
            services.AddSingleton<IEndpointAuthorizationService, EndpointAuthorizationService>();
            services.AddSingleton<IIdTokenValidationService, IdTokenValidationService>();
            services.AddSingleton<IAuthTokenValidationService, AuthTokenValidationService>();
            services.AddSingleton<IOdsCodeService, OdsCodeService>();
            services.AddSingleton<IDomesticAccessService, DomesticAccessService>();
            services.AddSingleton<IEndpointProofingLevelService, EndpointProofingLevelService>();
        }
    }
}
