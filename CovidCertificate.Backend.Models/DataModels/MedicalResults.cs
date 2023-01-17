using System.Collections.Generic;
using CovidCertificate.Backend.Models.Interfaces;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class MedicalResults
    {
        public MedicalResults(List<Vaccine> vaccines = default, List<TestResultNhs> testResults = default)
        {
            this.Vaccines = vaccines;
            this.DiagnosticTestResults = testResults;
        }
        public List<Vaccine> Vaccines { get; set; }
        public List<TestResultNhs> DiagnosticTestResults { get; set; }
        public List<IGenericResult> GetAllMedicalResults()
        {
            var allMedicalResults = new List<IGenericResult>();
            allMedicalResults.AddRange(Vaccines);
            allMedicalResults.AddRange(DiagnosticTestResults);

            return allMedicalResults;
        }
    }
}
