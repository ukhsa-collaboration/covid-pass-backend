using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using System.Linq;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration;
using CovidCertificate.Backend.Models.Helpers;

namespace CovidCertificate.Backend.Services.Certificates
{
    public class DomesticExemptionCertificateGenerator : IDomesticExemptionCertificateGenerator
    {
        private readonly IQRCodeGenerator qRCodeGenerator;
        private readonly IBlobFilesInMemoryCache<EligibilityConfiguration> blobCache;
        private readonly ILogger<DomesticExemptionCertificateGenerator> logger;
        private readonly IUVCIGeneratorService uvciGeneratorService;
        private readonly IConfiguration configuration;
        private readonly IFeatureManager featureManager;
        private readonly IEligibilityConfigurationService eligibilityConfigurationService;

        public DomesticExemptionCertificateGenerator(
            ILogger<DomesticExemptionCertificateGenerator> logger,
            IQRCodeGenerator qRCodeGenerator,
            IBlobFilesInMemoryCache<EligibilityConfiguration> blobCache,
            IUVCIGeneratorService uvciGeneratorService,
            IConfiguration configuration,
            IFeatureManager featureManager,
            IEligibilityConfigurationService eligibilityConfigurationService)
        {
            this.qRCodeGenerator = qRCodeGenerator;
            this.blobCache = blobCache;
            this.logger = logger;
            this.uvciGeneratorService = uvciGeneratorService;
            this.configuration = configuration;
            this.featureManager = featureManager;
            this.eligibilityConfigurationService = eligibilityConfigurationService;
        }

        public async Task<Certificate> GenerateDomesticExemptionCertificateAsync(CovidPassportUser user, DomesticExemption exemption)
        {
            logger.LogTraceAndDebug("GenerateDomesticExemptionCertificate was invoked");

            (var expiryDate, var eligibilityDate) = await CalculateCertificateExpiryAndEligibilityAsync(exemption);
            
            CertificateType certificateType = await featureManager.IsEnabledAsync(FeatureFlags.MandatoryCerts) ? CertificateType.DomesticMandatory : CertificateType.Exemption;

            var certificate = new Certificate(user.Name, user.DateOfBirth, expiryDate, eligibilityDate, certificateType, CertificateScenario.Domestic);

            if (await featureManager.IsEnabledAsync(FeatureFlags.DomesticBoosters))
            {
                certificate.Policy = new[] { "GB-ENG:4" };
                certificate.PolicyMask = 123;
            }            

            var qrCode = await qRCodeGenerator.GenerateQRCodesAsync(certificate,user);
            certificate.QrCodeTokens.Add(qrCode.FirstOrDefault());
            certificate.UniqueCertificateIdentifier = await uvciGeneratorService.GenerateAndInsertUvciAsync(
                new GenerateAndInsertUvciCommand(
                    this.configuration["DomesticAuthority"],
                    this.configuration["CountryOfIssuer"],
                    StringUtils.GetHashValue(certificate.Name, certificate.DateOfBirth),
                    certificate.CertificateType,
                    certificate.CertificateScenario,
                    expiryDate));

            logger.LogTraceAndDebug($"certificate: {certificate?.ToString()}");
            logger.LogTraceAndDebug("GenerateDomesticExemptionCertificate has finished");

            return certificate;
        }

        private async Task<(DateTime, DateTime)> CalculateCertificateExpiryAndEligibilityAsync(DomesticExemption exemption)
        {
            (var container, var filename) = await eligibilityConfigurationService.GetEligibilityConfigurationBlobContainerAndFilenameAsync();
            var configuration = await blobCache.GetFileAsync(container, filename);
            var certificateExpiresInHours = configuration.DomesticExemptions.CertificateExpiresInHours;

            var eligibilityDate = exemption.DateExemptionExpires ?? DateTime.UtcNow.AddDays(10000);
            var certificateExpiryDate = DateTime.UtcNow.AddHours(certificateExpiresInHours);

            var expiry = eligibilityDate > certificateExpiryDate ? certificateExpiryDate : eligibilityDate;

            return (expiry, eligibilityDate);
        }
    }
}
