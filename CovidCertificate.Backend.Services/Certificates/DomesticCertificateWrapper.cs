using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Models.ResponseDtos;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CovidCertificate.Backend.Services.Certificates
{
    public class DomesticCertificateWrapper : IDomesticCertificateWrapper
    {
        private readonly ILogger<DomesticCertificateWrapper> logger;
        private readonly IFeatureManager featureManager;

        public DomesticCertificateWrapper(ILogger<DomesticCertificateWrapper> logger, IFeatureManager featureManager)
        {
            this.logger = logger;
            this.featureManager = featureManager;
        }
        public async Task<DomesticCertificateResponse> WrapAsync(CertificatesContainer certificateContainer, bool expiredCertificateExists)
        {
            var certificate = certificateContainer.GetSingleCertificateOrNull();
            var errorCode = certificateContainer.ErrorCode;
            var waitPeriod = certificateContainer.WaitPeriod;
            var twoPassEnabled = await GetMandatoryToggleAsync(featureManager);

            logger.LogTraceAndDebug($"certificate: {certificate}");
            if (await featureManager.IsEnabledAsync(FeatureFlags.ErrorScenarios))
            {
                var certificateResponse = new DomesticCertificateResponse(certificate, expiredCertificateExists, twoPassEnabled, errorCode, waitPeriod);
                if (errorCode != null)
                    logger.LogInformation($"business rules evaluation completed with outcome code: {errorCode}");
                if (certificate != null)
                    logger.LogInformation($"business rules evaluation completed with outcome code: 0");

                return certificateResponse;
            }

            return new DomesticCertificateResponse(certificate, expiredCertificateExists, twoPassEnabled);
        }

        private async Task<MandatoryToggle> GetMandatoryToggleAsync(IFeatureManager featureManager)
        {
            if (!await featureManager.IsEnabledAsync(FeatureFlags.MandatoryCerts))
            {
                return MandatoryToggle.MandatoryVoluntaryOff;
            }

            return await featureManager.IsEnabledAsync(FeatureFlags.VoluntaryDomestic) ? MandatoryToggle.MandatoryAndVoluntary
                                                                                       : MandatoryToggle.MandatoryOnly;
        }
    }
}
