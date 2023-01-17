using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.BlobService;
using CovidCertificate.Backend.Interfaces.PKINationaBackend;
using CovidCertificate.Backend.Models.PKINationalBackend.DomesticPolicy;
using CovidCertificate.Backend.PKINationalBackend.Models;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.PKINationalBackend.Services
{
    public class DomesticPolicyInformationService : IDomesticPolicyInformationService
    {
        private readonly ILogger<DomesticPolicyInformationService> logger;
        private readonly IBlobService blobService;
        private readonly DGCGSettings settings;

        public DomesticPolicyInformationService(ILogger<DomesticPolicyInformationService> logger, IBlobService blobService, DGCGSettings settings)
        {
            this.logger = logger;
            this.blobService = blobService;
            this.settings = settings;
        }

        public async Task<DomesticPolicyInformation> GetDomesticPolicyInformationAsync()
        {
            logger.LogTraceAndDebug($"{nameof(GetDomesticPolicyInformationAsync)} was invoked.");

            var policyInformation = await blobService.GetObjectFromBlobAsync<DomesticPolicyInformation>(settings.PolicyInformationBlobContainerName, settings.PolicyInformationBlobFileName);
            var blobProperties = await blobService.GetBlobPropertiesAsync(settings.PolicyInformationBlobContainerName, settings.PolicyInformationBlobFileName);
            policyInformation.LastUpdated = blobProperties.LastModified.UtcDateTime;

            logger.LogTraceAndDebug($"{nameof(GetDomesticPolicyInformationAsync)} has finished.");

            return policyInformation;
        }
    }
}
