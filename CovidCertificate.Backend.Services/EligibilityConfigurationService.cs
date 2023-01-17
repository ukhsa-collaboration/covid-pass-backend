using CovidCertificate.Backend.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Helpers;

namespace CovidCertificate.Backend.Services
{
    public class EligibilityConfigurationService : IEligibilityConfigurationService
    {
        private readonly IConfiguration configuration;
        private readonly IFeatureManager featureManager;

        public EligibilityConfigurationService(
            IConfiguration configuration,
            IFeatureManager featureManager)
        {
            this.configuration = configuration;
            this.featureManager = featureManager;
        }

        public async Task<(string, string)> GetEligibilityConfigurationBlobContainerAndFilenameAsync()
        {
            var allowMandatoryCerts = (await featureManager.IsEnabledAsync(FeatureFlags.MandatoryCerts));
            var enableDomesticBoosters = await featureManager.IsEnabledAsync(FeatureFlags.DomesticBoosters);

            var containerName = configuration[$"BlobContainerNameEligibilityConfiguration"];

            var filename = enableDomesticBoosters ?
                configuration[$"BlobFileNameEligibilityConfigurationMandatoryWithBoosters"]
                : allowMandatoryCerts ? configuration["BlobFileNameEligibilityConfigurationMandatory"]
                : configuration["BlobFileNameEligibilityConfiguration"];

            return (containerName, filename);
        }
    }
}
