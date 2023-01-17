using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.DataModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.RequestDtos;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using CovidCertificate.Backend.Utils.Extensions;
using System.Linq;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.ResponseDtos;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Helpers;

namespace CovidCertificate.Backend.Services.Certificates
{
    public class CovidCertificateService : ICovidCertificateService
    {
        private readonly IQueueService queueService;
        private readonly ILogger<CovidCertificateService> logger;
        private readonly ICovidCertificateBuilder certificateBuilder;
        private readonly IUVCIGeneratorService uvciGenerator;
        private readonly IDomesticExemptionCertificateGenerator domesticExemptionCertificateGenerator;
        private readonly IIneligibilityService ineligibilityService;
        private readonly IDomesticExemptionService domesticExemptionService;
        private readonly IFeatureManager featureManager;

        public CovidCertificateService(
            IQueueService queueService,
            ILogger<CovidCertificateService> logger,
            ICovidCertificateBuilder certificateBuilder,
            IUVCIGeneratorService uvciGenerator,
            IDomesticExemptionCertificateGenerator domesticExemptionCertificateGenerator,
            IIneligibilityService ineligibilityService,
            IDomesticExemptionService domesticExemptionService,
            IFeatureManager featureManager)
        {
            this.queueService = queueService;
            this.logger = logger;
            this.certificateBuilder = certificateBuilder;
            this.uvciGenerator = uvciGenerator;
            this.domesticExemptionCertificateGenerator = domesticExemptionCertificateGenerator;
            this.ineligibilityService = ineligibilityService;
            this.domesticExemptionService = domesticExemptionService;
            this.featureManager = featureManager;
        }

        public async Task<CertificatesContainer> GetDomesticCertificateAsync(CovidPassportUser user, string idToken, MedicalResults medicalResults)
        {
            logger.LogTraceAndDebug($"{nameof(GetDomesticCertificateAsync)} was invoked");

            if (await MandatoryEnabledAndVoluntaryNotAllowedAsync())
            {
                return await GetAllCertificatesAsync(user, idToken, CertificateScenario.Domestic, medicalResults, CertificateType.DomesticMandatory);
            }

            return await GetAllCertificatesAsync(user, idToken, CertificateScenario.Domestic, medicalResults);
        }

        public async Task<CertificatesContainer> GetInternationalCertificateAsync(CovidPassportUser user, string idToken, CertificateType? type, MedicalResults medicalResults)
        {
            logger.LogTraceAndDebug($"{nameof(GetInternationalCertificateAsync)} was invoked");

            return await GetAllCertificatesAsync(user, idToken, CertificateScenario.International, medicalResults, type);
        }

        public async Task<CertificatesContainer> GetDomesticUnattendedCertificateAsync(CovidPassportUser user, MedicalResults medicalResults)
        {
            return await GetDomesticCertificateAsync(user, "", medicalResults);
        }

        public async Task<CertificatesContainer> GetInternationalUnattendedCertificateAsync(CovidPassportUser user, CertificateType? type, MedicalResults medicalResults)
        {
            return await GetInternationalCertificateAsync(user, "", type, medicalResults);
        }

        private async Task<CertificatesContainer> GetAllCertificatesAsync(CovidPassportUser user, string idToken, CertificateScenario scenario, MedicalResults medicalResults, CertificateType? type = null)
        {
            logger.LogTraceAndDebug($"{nameof(GetAllCertificatesAsync)} was invoked");

            if (user == default)
            {
                logger.LogTraceAndDebug($"covidTestUser == default");
                throw new ArgumentNullException(nameof(user), "No user to get certificate for");
            }

            var ineligibiltyStatus = await CheckIneligibilityStatusAsync(scenario, medicalResults);

            if (ineligibiltyStatus != null)
            {
                return ineligibiltyStatus;
            }

            var certificatesFromResults = await certificateBuilder.BuildCertificatesFromResultsAsync(
                medicalResults.GetAllMedicalResults(),
                user,
                scenario,
                type);

            if (!certificatesFromResults.Certificates.Any() && scenario.Equals(CertificateScenario.Domestic))
            {
                var userExemptions = await domesticExemptionService.GetAllExemptionsAsync(user, idToken);

                if (userExemptions.Any())
                {
                    var longestExemption = ChooseExemptionWithLongestExpiry(userExemptions);
                    var certificateFromExemption = await domesticExemptionCertificateGenerator.GenerateDomesticExemptionCertificateAsync(user, longestExemption);

                    logger.LogTraceAndDebug($"{nameof(GetAllCertificatesAsync)} has finished");
                    return new CertificatesContainer(certificateFromExemption);
                }
            }

            var isUnattendedUser = string.IsNullOrEmpty(idToken);
            if (isUnattendedUser && type == CertificateType.Vaccination)
            {
                CheckIfVaccinesArePresentInUnattended(medicalResults);
            }

            logger.LogTraceAndDebug($"{nameof(GetAllCertificatesAsync)} has finished");

            return certificatesFromResults;
        }

        private static void CheckIfVaccinesArePresentInUnattended(MedicalResults allResults)
        {
            if (allResults.Vaccines.NullOrEmpty())
            {
                throw new NoUnattendedVaccinesFoundException("No vaccines were found");
            }
        }

        public async Task<bool> ExpiredCertificateExistsAsync(CovidPassportUser user, Certificate certificate)
        {
            logger.LogTraceAndDebug($"{nameof(ExpiredCertificateExistsAsync)} was invoked");

            var expiredCertificateExists = true;
            if (certificate == null)
            {
                expiredCertificateExists = await uvciGenerator.IfUvciExistsForUserAsync(user, CertificateScenario.Domestic);
            }

            logger.LogTraceAndDebug($"{nameof(ExpiredCertificateExistsAsync)} has finished");
            return expiredCertificateExists;
        }

        private async Task<CertificatesContainer> CheckIneligibilityStatusAsync(CertificateScenario scenario, MedicalResults allResults)
        {
            var ineligibilityDomestic = scenario.Equals(CertificateScenario.Domestic) && await featureManager.IsEnabledAsync(FeatureFlags.IneligibilityDomestic);
            var ineligibilityInternational = scenario.Equals(CertificateScenario.International) && await featureManager.IsEnabledAsync(FeatureFlags.IneligibilityInternational);

            if (ineligibilityDomestic || ineligibilityInternational)
            {
                var ineligibilityResult = await ineligibilityService.GetUserIneligibilityAsync(allResults.DiagnosticTestResults);

                if (ineligibilityResult != null)
                {
                    return new CertificatesContainer(ineligibilityResult.ErrorCode, ineligibilityResult.WaitPeriod);
                }
            }

            return null;
        }

        public async Task<bool> SendCertificateAsync(AddPdfCertificateRequestDto dto, string outputQueueName)
        {
            logger.LogTraceAndDebug("SendCertificate was invoked");

            var result = await queueService.SendMessageAsync(outputQueueName, dto);
            logger.LogTraceAndDebug($"result is {result}");

            return result;
        }

        private async Task<bool> MandatoryEnabledAndVoluntaryNotAllowedAsync()
        {
            var allowMandatoryCert = await featureManager.IsEnabledAsync(FeatureFlags.MandatoryCerts);
            var allowVoluntaryDomesticCert = await featureManager.IsEnabledAsync(FeatureFlags.VoluntaryDomestic);

            return allowMandatoryCert && !allowVoluntaryDomesticCert;
        }

        private DomesticExemption ChooseExemptionWithLongestExpiry(IEnumerable<DomesticExemption> exemptions)
        {
            var infiniteExpiryExemptions = exemptions.Where(ex => ex.DateExemptionExpires == null);

            return infiniteExpiryExemptions.Any() ? infiniteExpiryExemptions.First()
                : exemptions.OrderByDescending(x => x.DateExemptionExpires).First();
        }
    }
}
