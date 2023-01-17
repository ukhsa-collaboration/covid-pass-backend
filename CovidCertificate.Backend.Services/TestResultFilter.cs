using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace CovidCertificate.Backend.Services
{
    public class TestResultFilter:ITestResultFilter
    {
        private readonly ILogger<TestResultFilter> logger;
        private const string selfTest = "SELFTEST";
        public TestResultFilter(ILogger<TestResultFilter> logger)
        {
            this.logger = logger;
        }

        public IEnumerable<TestResultNhs> FilterOutHomeTest(IEnumerable<TestResultNhs> allResults, string validityType)
        {
            logger.LogTraceAndDebug("filterOutHomeTest was invoked");
            var validResults = new List<TestResultNhs>();
            foreach (var result in allResults)
            {
                if (result.ProcessingCode == null)
                {
                    validResults.Add(result);
                    continue;
                }
                if (result.ProcessingCode.ToUpper() != selfTest && result.ValidityType != validityType)
                {
                    validResults.Add(result);
                }
            }
            logger.LogTraceAndDebug("filterOutHomeTest has finished");
            return validResults;
        }
    }
}

