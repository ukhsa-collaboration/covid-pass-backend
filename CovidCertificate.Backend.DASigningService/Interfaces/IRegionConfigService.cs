using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.DASigningService.Models;

namespace CovidCertificate.Backend.DASigningService.Interfaces
{
    public interface IRegionConfigService
    {
        RegionConfig GetRegionConfig(string regionSubscriptionHeader, ErrorHandler errorHandler);
    }
}
