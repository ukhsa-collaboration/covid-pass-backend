using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.Certificates.UVCI
{
    public class UVCIGeneratorService : IUVCIGeneratorService
    {
        private readonly IDomesticUVCIGenerator domesticUvciGenerator;
        private readonly IRegionUVCIGenerator regionUvciGenerator;
        private readonly ILogger<UVCIGeneratorService> logger;

        public UVCIGeneratorService(IDomesticUVCIGenerator domesticUvciGenerator,
            IRegionUVCIGenerator regionUvciGenerator,
            ILogger<UVCIGeneratorService> logger)
        {
            this.domesticUvciGenerator = domesticUvciGenerator;
            this.regionUvciGenerator = regionUvciGenerator;
            this.logger = logger;
        }

        public async Task<bool> IfUvciExistsForUserAsync(CovidPassportUser user, CertificateScenario scenario)
        {
            logger.LogTraceAndDebug("IfUvciExistsForUserAsync was invoked");

            var result = await domesticUvciGenerator.DomesticUvciExistsForUserAsync(user, scenario);

            logger.LogTraceAndDebug("IfUvciExistsForUserAsync has finished");

            return result;
        }

        public async Task<string> GenerateAndInsertUvciAsync(GenerateAndInsertUvciCommand command)
        {
            if (command.UVCICountryCode == "GB")
            {
                return await domesticUvciGenerator.GenerateAndInsertDomesticUvciAsync(command);
            }

            return await regionUvciGenerator.GenerateAndInsertRegionUvciAsync(command);
        }
    }
}
