using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands;

namespace CovidCertificate.Backend.Interfaces.Certificates.UVCI
{
    public interface IRegionUVCIGenerator
    {
        Task<string> GenerateAndInsertRegionUvciAsync(GenerateAndInsertUvciCommand command);
    }
}
