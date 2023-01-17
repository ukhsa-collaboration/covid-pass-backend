using System;
using CovidCertificate.Backend.Utils.Extensions;

namespace CovidCertificate.Backend.Models.DataModels.PassData
{
    public class RecoveryPassData
    {
        public string DateOfFirstPositiveTestResult { get; set; }
        public string CertificateType { get; set; }
        public string DiseaseTargeted { get; set; }
        public string CountryOfTest { get; set; }
        public string CertificateIssuer { get; set; }
        public string CertificateValidFrom { get; set; }
        public string CertificateValidUntil { get; set; }
        public string Uvci { get; set; }
        public RecoveryPassData(TestResultNhs testResultNhs, string languageCode)
        {
            DateOfFirstPositiveTestResult = StringUtils.GetTranslatedAndFormattedDate(testResultNhs.DateTimeOfTest, languageCode);
            CertificateType = testResultNhs.ValidityType;
            DiseaseTargeted = testResultNhs.DiseaseTargeted.Item2;
            CountryOfTest = testResultNhs.CountryOfAuthority;
            CertificateIssuer = testResultNhs.Authority;
            CertificateValidFrom = StringUtils.GetTranslatedAndFormattedDate(DateTime.UtcNow, languageCode);
        }
    }
}
