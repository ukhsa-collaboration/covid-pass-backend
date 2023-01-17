using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;

namespace CovidCertificate.Backend.Interfaces.Certificates.UVCI
{
    public interface IUVCIRepository<T>
    {
        Task<bool> UvciExistsAsync(CovidPassportUser user, CertificateScenario scenario);

        Task<bool> InsertUvciAsync(string uvci, DateTime certificateGenerationDateTime,
            GenerateAndInsertUvciCommand command);
    }
}
