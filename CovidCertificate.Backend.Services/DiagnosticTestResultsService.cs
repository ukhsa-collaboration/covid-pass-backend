using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.NhsApiIntegration.Interfaces;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils;
using Microsoft.FeatureManagement;

namespace CovidCertificate.Backend.Services
{
    public class DiagnosticTestResultsService : IDiagnosticTestResultsService
    {
        private readonly ILogger<DiagnosticTestResultsService> logger;
        private readonly INhsdFhirApiService nhsdFhirApiService;
        private readonly IFhirBundleMapper<TestResultNhs> fhirBundleMapper;
        private readonly IRedisCacheService redisCacheService;
        private readonly IFeatureManager featureManager;
        private readonly ITestResultFilter resultFilter;
        private readonly IUnattendedSecurityService unattendedSecurityService;

        public DiagnosticTestResultsService(
            ILogger<DiagnosticTestResultsService> logger,
            INhsdFhirApiService nhsdFhirApiService,
            IFhirBundleMapper<TestResultNhs> fhirBundleMapper,
            IRedisCacheService redisCacheService,
            IFeatureManager featureManager,
            ITestResultFilter resultFilter,
            IUnattendedSecurityService unattendedSecurityService)
        {
            this.logger = logger;
            this.nhsdFhirApiService = nhsdFhirApiService;
            this.fhirBundleMapper = fhirBundleMapper;
            this.redisCacheService = redisCacheService;
            this.featureManager = featureManager;
            this.resultFilter = resultFilter;
            this.unattendedSecurityService = unattendedSecurityService;
        }

        public async Task<IEnumerable<TestResultNhs>> GetDiagnosticTestResultsAsync(string idToken, string apiKey)
        {
            bool diagnosticTestResultsEnabled = await featureManager.IsEnabledAsync(FeatureFlags.DiagnosticTestResults);
            if (!diagnosticTestResultsEnabled)
            {
                logger.LogInformation($"{FeatureFlags.DiagnosticTestResults} is {diagnosticTestResultsEnabled} so returning empty list");
                return Enumerable.Empty<TestResultNhs>();
            }
            string key = $"GetDiagnosticTestResults:{JwtTokenUtils.CalculateHashFromIdToken(idToken)}";

            IEnumerable<TestResultNhs> cachedResults;
            bool cachedResponseExists;

            // If Redis is disabled for test results, do not get data from Redis
            if (!await featureManager.IsEnabledAsync(FeatureFlags.RedisForTests))
            {
                (cachedResults, cachedResponseExists) = (Enumerable.Empty<TestResultNhs>(), false);
            }
            else
            {
                (cachedResults, cachedResponseExists) =
                    await redisCacheService.GetKeyValueAsync<IEnumerable<TestResultNhs>>(key);
            };
            
            if (cachedResponseExists)
            {
                return cachedResults;
            }

            var bundle = await nhsdFhirApiService.GetDiagnosticTestResultsAsync(idToken, apiKey);
            var results = await fhirBundleMapper.ConvertBundleAsync(bundle);

            if (!await featureManager.IsEnabledAsync(FeatureFlags.PCRSelfTests))
            {
                results = resultFilter.FilterOutHomeTest(results, "PCR");
                logger.LogInformation("PCR HomeTests filtered out");
            }

            if (!await featureManager.IsEnabledAsync(FeatureFlags.LFTSelfTests))
            {
                results = resultFilter.FilterOutHomeTest(results, "LFT");
                logger.LogInformation("LFT HomeTests filtered out");
            }

            // If Redis is disabled for test results, do not save data to Redis
            if (await featureManager.IsEnabledAsync(FeatureFlags.RedisForTests))
            {
                await redisCacheService.AddKeyAsync(key, results, RedisLifeSpanLevel.ThirtyMinutes);
            }

            return results;
        }

        public async Task<IEnumerable<TestResultNhs>> GetUnattendedDiagnosticTestResultsAsync(CovidPassportUser user, string apiKey)
        {
            unattendedSecurityService.Authorize();

            bool diagnosticTestResultsEnabled = await featureManager.IsEnabledAsync(FeatureFlags.DiagnosticTestResults);
            if (!diagnosticTestResultsEnabled)
            {
                logger.LogInformation($"{FeatureFlags.DiagnosticTestResults} is {diagnosticTestResultsEnabled} so returning empty list");
                return Enumerable.Empty<TestResultNhs>();
            }

            var bundle = await nhsdFhirApiService.GetUnattendedDiagnosticTestResultsAsync(user, apiKey);
            var results = await fhirBundleMapper.ConvertBundleAsync(bundle);

            return results;
        }
    }
}
