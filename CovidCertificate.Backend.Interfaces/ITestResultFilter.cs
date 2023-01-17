using System.Collections.Generic;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Interfaces
{
    public interface ITestResultFilter
    {
        IEnumerable<TestResultNhs> FilterOutHomeTest(IEnumerable<TestResultNhs> allResults, string validityType);
    }
}
