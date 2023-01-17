using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration;

namespace CovidCertificate.Backend.Services.Certificates
{
    public class IneligibilityService : IIneligibilityService
    {
        private int lockoutDays;
        private int negationTestPeriodDays;
        private int stackingPeriodDays;
        private readonly IBlobFilesInMemoryCache<EligibilityConfiguration> mappings;
        private readonly ILogger<IneligibilityService> logger;
        private readonly string blobContainer;
        private readonly string blobFilename;
        private readonly IConfiguration configuration;

        public IneligibilityService(IBlobFilesInMemoryCache<EligibilityConfiguration> mappings, IConfiguration configuration, ILogger<IneligibilityService> logger)
        {
            this.mappings = mappings;
            this.configuration = configuration;
            this.logger = logger;
            blobContainer = this.configuration["BlobContainerNameEligibilityConfiguration"];
            blobFilename = this.configuration["BlobFileNameEligibilityConfiguration"];
        }

        public async Task<IneligiblityResult> GetUserIneligibilityAsync(IEnumerable<TestResultNhs> tests)
        {
            var configuration = (await mappings.GetFileAsync(blobContainer, blobFilename));
            lockoutDays = configuration.Ineligibility.LockoutPeriodDays;
            negationTestPeriodDays = configuration.Ineligibility.NegationTestPeriodDays;
            stackingPeriodDays = configuration.Ineligibility.StackingPeriodDays;

            var positiveTests = tests.Where(p => p.IsPositive()).OrderBy(p => p.DateTimeOfTest);

            var validPositiveTests = positiveTests.Where(p => p.IsNAAT || TestResultNotFollowedByNegativeNAAT(p, tests));

            var lockoutResetTests = RemoveTestsWithinStackingPeriod(validPositiveTests);

            if (lockoutResetTests.Where(t => IsDateInLockoutPeriod(t.DateTimeOfTest)).Any())
            {
                var recentTest = lockoutResetTests.OrderBy(t => t.DateTimeOfTest).Last();
                var errorCode = GetErrorCode(recentTest);
                logger.LogInformation($"User was Ineligible for certificate with error code: {errorCode}");
                return new IneligiblityResult(errorCode, recentTest.DateTimeOfTest.AddDays(lockoutDays));
            }

            return null;
        }

        private bool TestResultNotFollowedByNegativeNAAT(TestResultNhs test, IEnumerable<TestResultNhs> allTests)
        {
            var negativeNAATTests = allTests.Where(t => t.IsNegative() && t.IsNAAT);

            return !negativeNAATTests.Where(n => n.DateTimeOfTest > test.DateTimeOfTest && 
                                            n.DateTimeOfTest < test.DateTimeOfTest.AddDays(negationTestPeriodDays))
                                     .Any();
        }

        private bool IsDateInLockoutPeriod(DateTime date)
        {
            return date.AddDays(lockoutDays) >= DateTime.UtcNow;
        }

        private int? GetErrorCode(TestResultNhs test)
        {
            switch (test.ValidityType)
            {
                case "PCR":
                    return 2;
                case "LFT":
                    return 3;
                default:
                    return null;
            }
        }

        private IEnumerable<TestResultNhs> RemoveTestsWithinStackingPeriod(IEnumerable<TestResultNhs> tests)
        {
            var nonStackedTests = new List<TestResultNhs>();

            foreach(var test in tests.OrderBy(t => t.DateTimeOfTest))
            {
                if ((nonStackedTests.LastOrDefault()?.DateTimeOfTest.AddDays(stackingPeriodDays) ?? DateTime.MinValue)
                                <= test.DateTimeOfTest)
                {
                    nonStackedTests.Add(test);
                }
            }

            return nonStackedTests;
        }
    }
}
