using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;

namespace CovidCertificate.Backend.Interfaces.Certificates.UVCI
{
    public interface IDomesticUVCIGenerator
    {
        Task<bool> DomesticUvciExistsForUserAsync(CovidPassportUser user, CertificateScenario scenario);
        Task<string> GenerateAndInsertDomesticUvciAsync(GenerateAndInsertUvciCommand command);
    }
}
