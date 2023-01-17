using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.Responses;
using CovidCertificate.Backend.DASigningService.Services.Commands;

namespace CovidCertificate.Backend.DASigningService.Interfaces
{
    public interface IDomesticBarcodeGenerator
    {
        Task<BarcodeResults> GenerateDomesticBarcodeAsync(GenerateDomesticBarcodeCommand command);
    }
}
