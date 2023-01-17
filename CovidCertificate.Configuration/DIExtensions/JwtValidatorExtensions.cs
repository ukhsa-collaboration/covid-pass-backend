using CovidCertificate.Backend.Interfaces.JwtServices;
using CovidCertificate.Backend.Services.SecurityServices;
using Microsoft.Extensions.DependencyInjection;

namespace CovidCertificate.Backend.Configuration.DIExtensions
{
    public static class JwtValidatorExtensions
    {
        public static void AddJwtValidatorServices(this IServiceCollection services)
        {
            services.AddSingleton<IJwtValidationParameterFetcher, JwtValidationParametersFetcher>();
            services.AddSingleton<IJwtValidator, JwtValidator>();
        }
    }
}
