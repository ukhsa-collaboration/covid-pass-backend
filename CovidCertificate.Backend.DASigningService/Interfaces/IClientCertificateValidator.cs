using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.DASigningService.Models;
using Microsoft.AspNetCore.Http;

namespace CovidCertificate.Backend.DASigningService.Interfaces
{
    public interface IClientCertificateValidator
    {
        void ValidateCertificate(HttpRequest request, ErrorHandler errorHandler, RegionConfig regionConfig);
    }
}
