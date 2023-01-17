namespace CovidCertificate.Backend.Models.Helpers
{
    public static class FeatureFlags
    {
        public const string MandatoryCerts = "MandatoryCerts";
        public const string VoluntaryDomestic = "VoluntaryDomestic";
        public const string Notify = "Notify";
        public const string P5TestResults = "P5TestResults";
        public const string RedisForTests = "RedisForTests";
        public const string FilterFirstAndLastVaccines = "FilterFirstAndLastVaccines";
        public const string DiagnosticTestResults = "DiagnoticTestResults";
        public const string LFTSelfTests = "LFTSelfTests";
        public const string PCRSelfTests = "PCRSelfTests";
        public const string RemoveBoosters = "RemoveBoosters";
        public const string ErrorScenarios = "ErrorScenarios";
        public const string DomesticBoosters = "DomesticBoosters";
        public const string RedisEnabled = "RedisEnabled";
        public static string IneligibilityDomestic = "IneligibilityDomestic";
        public static string IneligibilityInternational= "IneligibilityInternational";
        public static string UseMedicalExemptionsApi = "UseMedicalExemptionsApi";
        public const string U12TravelPass = "U12TravelPass";
        public const string DomesticPassAgeLimit = "AgeBasedDomesticAccess";
        public const string EnableDomestic = "EnableDomesticEndpoints";
        public const string EnableOtpTesting = "EnableOtpTesting";
    }
}
