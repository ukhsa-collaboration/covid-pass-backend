using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.Certificates.UVCI
{
    public class DomesticUVCIGenerator : UVCIGenerator<DomesticUVCIGeneratorModel>, IDomesticUVCIGenerator
    {
        public DomesticUVCIGenerator(IConfiguration configuration,
            ILogger<DomesticUVCIGenerator> logger,
            IUVCIRepository<DomesticUVCIGeneratorModel> domesticUvciRepository) : base(
            configuration, logger, domesticUvciRepository)
        {
        }

        public async Task<bool> DomesticUvciExistsForUserAsync(CovidPassportUser user, CertificateScenario scenario)
        {
            logger.LogTraceAndDebug("DomesticUvciExistsForUserAsync was invoked");

            var result = await uvciRepository.UvciExistsAsync(user, scenario);

            logger.LogTraceAndDebug("DomesticUvciExistsForUserAsync finished");

            return result;
        }

        public async Task<string> GenerateAndInsertDomesticUvciAsync(GenerateAndInsertUvciCommand command)
        {
            logger.LogTraceAndDebug("GenerateAndInsertDomesticUvciAsync was invoked.");

            var uvci = GenerateUvciString(command.UVCICountryCode, out DateTime generationDateTime);
            var uvciString = await InsertUvciAsync(command, uvci, generationDateTime);

            logger.LogTraceAndDebug("GenerateAndInsertDomesticUvciAsync finished.");

            return uvciString;
        }
    }
}
