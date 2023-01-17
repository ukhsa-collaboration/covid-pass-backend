using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.Certificates.UVCI
{
    public class RegionUVCIGenerator : UVCIGenerator<RegionUVCIGeneratorModel>, IRegionUVCIGenerator
    {
        public RegionUVCIGenerator(IConfiguration configuration,
            ILogger<RegionUVCIGenerator> logger,
            IUVCIRepository<RegionUVCIGeneratorModel> regionUvciRepository) : base(
            configuration, logger, regionUvciRepository)
        {
        }

        public async Task<string> GenerateAndInsertRegionUvciAsync(GenerateAndInsertUvciCommand command)
        {
            logger.LogTraceAndDebug("GenerateAndInsertRegionUvciAsync was invoked.");

            var uvci = GenerateUvciString(command.UVCICountryCode, out DateTime generationDateTime);
            var uvciString = await InsertUvciAsync(command, uvci, generationDateTime);

            logger.LogTraceAndDebug("GenerateAndInsertRegionUvciAsync finished.");

            return uvciString;
        }
    }
}
