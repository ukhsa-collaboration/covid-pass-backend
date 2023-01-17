using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration;
using CovidCertificate.Backend.Models.Enums;
using Microsoft.Extensions.Configuration;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.FeatureManagement;

namespace CovidCertificate.Backend.Services
{
    public class IsolationExemptionStatusService : IIsolationExemptionStatusService
    {
        private readonly ILogger<IsolationExemptionStatusService> logger;
        private readonly IConfigurationValidityCalculator configurationValidityCalculator;
        private readonly IBlobFilesInMemoryCache<EligibilityConfiguration> eligibilityConfigurationBlobCache;
        private readonly IVaccineService vaccinesService;
        private readonly IMedicalExemptionService medicalExemptionService;
        private readonly IClinicalTrialExemptionService clinicalTrialExemptionService;
        private readonly IEligibilityConfigurationService eligibilityConfigurationService;

        public IsolationExemptionStatusService(ILogger<IsolationExemptionStatusService> logger,
                                               IConfigurationValidityCalculator configurationValidityCalculator,
                                               IBlobFilesInMemoryCache<EligibilityConfiguration> eligibilityConfigurationBlobCache,
                                               IVaccineService vaccinesService,
                                               IMedicalExemptionService medicalExemptionService,
                                               IClinicalTrialExemptionService clinicalTrialExemptionService,
                                               IEligibilityConfigurationService eligibilityConfigurationService)
        {
            this.logger = logger;
            this.configurationValidityCalculator = configurationValidityCalculator;
            this.eligibilityConfigurationBlobCache = eligibilityConfigurationBlobCache;
            this.vaccinesService = vaccinesService;
            this.medicalExemptionService = medicalExemptionService;
            this.clinicalTrialExemptionService = clinicalTrialExemptionService;
            this.eligibilityConfigurationService = eligibilityConfigurationService;
        }

        public async Task<IsolationExemptionStatus> GetIsolationExemptionStatusAsync(CovidPassportUser patient, DateTime effectiveDateTime, string apiKey)
        {
            var status = await GetVaccinationExemptionStatusAsync(patient, effectiveDateTime, apiKey);

            if (status != IsolationExemptionStatus.INSUFFICIENT_RECORDS_FOUND)
            {
                return status;
            }

            if (await medicalExemptionService.IsUserMedicallyExemptAsync(patient, null))
            {
                return IsolationExemptionStatus.MEDICAL_EXEMPTION;
            }

            if (await clinicalTrialExemptionService.IsUserClinicalTrialExemptAsync(patient.NhsNumber, patient.DateOfBirth))
            {
                return IsolationExemptionStatus.CLINICAL_TRIAL;
            }

            return IsolationExemptionStatus.INSUFFICIENT_RECORDS_FOUND;
        }

        private async Task<IsolationExemptionStatus> GetVaccinationExemptionStatusAsync(CovidPassportUser user, DateTime effectiveDateTime, string apiKey)
        {
            var vaccinations = await vaccinesService.GetUnattendedVaccinesAsync(user, apiKey, shouldFilterFirstAndLast: true, checkBundleBirthdate: true);
            if (vaccinations.NullOrEmpty())
            {
                return IsolationExemptionStatus.INSUFFICIENT_RECORDS_FOUND;
            }

            var configurationFile = await GetIsolationExemptionConfigurationFileAsync();
            (var fullyVaccinatedRules, var insufficientHoursRules) = GetEligibilityRules(configurationFile);

            if (UserMeetsRules(vaccinations, fullyVaccinatedRules, user, effectiveDateTime))
            {
                return IsolationExemptionStatus.FULLY_VACCINATED;
            }

            return UserMeetsRules(vaccinations, insufficientHoursRules, user, effectiveDateTime)
                                    ? IsolationExemptionStatus.INSUFFICIENT_TIME_SINCE_LAST_VACCINATION
                                    : IsolationExemptionStatus.INSUFFICIENT_RECORDS_FOUND;
        }

        private async Task<EligibilityConfiguration> GetIsolationExemptionConfigurationFileAsync()
        {
            (var container, var filename) = await eligibilityConfigurationService.GetEligibilityConfigurationBlobContainerAndFilenameAsync();

            var configurationFile = await eligibilityConfigurationBlobCache.GetFileAsync(container, filename);

            if (configurationFile.Rules == null)
            {
                throw new Exception($"IsolationExemptionStatusService: {filename} does not contain any rules.");
            }

            return configurationFile;
        }

        private (IEnumerable<EligibilityRules>, IEnumerable<EligibilityRules>) GetEligibilityRules(EligibilityConfiguration configurationFile)
        {
            var fullyVaccinatedRules = configurationFile.Rules.Where(x => x.Conditions.Any() && x.Conditions.All(condition => condition.ProductType == (Models.Enums.DataType.Vaccination) && x.Scenario == CertificateScenario.Domestic));
            if (fullyVaccinatedRules.NullOrEmpty())
            {
                throw new Exception("IsolationExemptionStatusService: There should always be rules to generate an isolation exemption.");
            }

            var insufficientHoursRules = new List<EligibilityRules>();


            foreach (EligibilityRules fullyVaccinatedRule in fullyVaccinatedRules)
            {
                insufficientHoursRules.Add(EligibilityRules.Copy(fullyVaccinatedRule));
            }

            foreach (EligibilityRules insufficientHoursRule in insufficientHoursRules)
            {
                foreach (EligibilityCondition eligibilityCondition in insufficientHoursRule.Conditions)
                {
                    eligibilityCondition.ResultValidAfterHoursFromLastResult = 0;
                }

            }
            return (fullyVaccinatedRules, insufficientHoursRules);
        }

        private bool UserMeetsRules(IEnumerable<Vaccine> vaccinations, IEnumerable<EligibilityRules> rules, CovidPassportUser user, DateTime dateTime)
        {
            var fullyExemptCertificates = configurationValidityCalculator.GenerateCertificatesUsingRules(vaccinations, rules, user, dateTime);
            return fullyExemptCertificates.Any();
        }
    }
}
