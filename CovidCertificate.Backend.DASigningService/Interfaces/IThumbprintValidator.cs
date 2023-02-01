using CovidCertificate.Backend.DASigningService.ErrorHandling;
using Microsoft.AspNetCore.Http;

namespace CovidCertificate.Backend.DASigningService.Interfaces
{
    public interface IThumbprintValidator
    {
        void ValidateThumbprint(HttpRequest request, ErrorHandler errorHandler);
    }
}
