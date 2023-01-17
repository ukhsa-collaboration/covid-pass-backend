using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Helpers;

namespace CovidCertificate.Backend.Services.Certificates
{
    public class CovidResultsService : ICovidResultsService
    {
        private readonly IVaccineService vaccinesService;
        private readonly IDiagnosticTestResultsService testResultsService;
        private readonly ILogger<CovidResultsService> logger;
        private readonly IProofingLevelValidatorService proofingLevelValidatorService;
        private readonly IFeatureManager featureManager;

        public CovidResultsService(IVaccineService vaccinesService,
                                    IDiagnosticTestResultsService testResultsService,
                                    ILogger<CovidResultsService> logger,
                                    IProofingLevelValidatorService proofingLevelValidatorService,
                                    IFeatureManager featureManager)
        {
            this.vaccinesService = vaccinesService;
            this.testResultsService = testResultsService;
            this.logger = logger;
            this.proofingLevelValidatorService = proofingLevelValidatorService;
            this.featureManager = featureManager;
        }

        public async Task<MedicalResults> GetMedicalResultsAsync(CovidPassportUser user, string idToken, CertificateScenario scenario, string apiKey, CertificateType? type = null)
        {
            var isUnattendedUser = string.IsNullOrEmpty(idToken);

            if (isUnattendedUser)
            {
                switch (type)
                {
                    case CertificateType.Vaccination:
                        return await GetUnattendedVaccinationResultsAsync(user, apiKey);
                    case CertificateType.Recovery:
                        return await GetUnattendedTestResultsAsync(user, apiKey);
                    default:
                        return await GetAllMedicalResultsUnattendedAsync(user, apiKey);
                }
            }

            return await GetAllMedicalResultsAsync(user, idToken, scenario, apiKey);
        }

        private async Task<MedicalResults> GetAllMedicalResultsAsync(CovidPassportUser user, string idToken, CertificateScenario scenario, string apiKey)
        {
            var adequateProofingLevel = proofingLevelValidatorService.ValidateProofingLevel(idToken);

            var enableDomesticBoosters = await featureManager.IsEnabledAsync(FeatureFlags.DomesticBoosters);
            var vaccinationResultsTask = vaccinesService.GetAttendedVaccinesAsync(idToken, user, apiKey,
                shouldFilterFirstAndLast: scenario != CertificateScenario.International);

            Task<IEnumerable<TestResultNhs>> diagnosticTestResultsTask;

            if (scenario == CertificateScenario.Domestic && !adequateProofingLevel)
            {
                diagnosticTestResultsTask = Task.FromResult(Enumerable.Empty<TestResultNhs>());
            }
            else
            {
                diagnosticTestResultsTask = testResultsService.GetDiagnosticTestResultsAsync(idToken, apiKey);
            }

            await Task.WhenAll(vaccinationResultsTask, diagnosticTestResultsTask);
            
            return new MedicalResults(await vaccinationResultsTask, (await diagnosticTestResultsTask).ToList());
        }

        private async Task<MedicalResults> GetAllMedicalResultsUnattendedAsync(CovidPassportUser user, string apiKey)
        {
            var vaccineResultsTask = GetUnattendedVaccinationResultsAsync(user, apiKey);
            var testResultTask = GetUnattendedTestResultsAsync(user, apiKey);

            await Task.WhenAll(vaccineResultsTask, testResultTask);

            return new MedicalResults((await vaccineResultsTask).Vaccines, (await testResultTask).DiagnosticTestResults.ToList());
        }

        private async Task<MedicalResults> GetUnattendedVaccinationResultsAsync(CovidPassportUser user, string apiKey)
        {
            var results = await vaccinesService.GetUnattendedVaccinesAsync(user, apiKey);

            return new MedicalResults(results, new List<TestResultNhs>());
        }

        private async Task<MedicalResults> GetUnattendedTestResultsAsync(CovidPassportUser user, string apiKey)
        {
            var results = await testResultsService.GetUnattendedDiagnosticTestResultsAsync(user, apiKey);

            return new MedicalResults(new List<Vaccine>(), results.ToList());
        }
    }
}
