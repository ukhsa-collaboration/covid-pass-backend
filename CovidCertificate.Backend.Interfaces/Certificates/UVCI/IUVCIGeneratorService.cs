using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;

namespace CovidCertificate.Backend.Interfaces.Certificates.UVCI
{
    public interface IUVCIGeneratorService
    {
        Task<bool> IfUvciExistsForUserAsync(CovidPassportUser user, CertificateScenario scenario);
        Task<string> GenerateAndInsertUvciAsync(GenerateAndInsertUvciCommand command);
    }
}
