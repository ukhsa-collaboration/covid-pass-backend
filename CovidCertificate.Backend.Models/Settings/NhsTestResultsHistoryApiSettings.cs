namespace CovidCertificate.Backend.Models.Settings
{
    public class NhsTestResultsHistoryApiSettings
    {
        public string NhsTestResultsHistoryApiBaseUrl { get; set; }
        public string NhsTestResultsHistoryApiAccessTokenBaseUrl { get; set; }
        public string NhsTestResultsHistoryApiEndpoint { get; set; }
        public bool UseTestResultsHistoryMock { get; set; }
        public string TestResultsHistoryMockApiKey { get; set; }
        public string AuthMockApiKey { get; set; }
        public string NhsTestResultsHistoryApiAccessTokenAppKid { get; set; }
        public string NhsTestResultsHistoryApiAccessTokenAppKey { get; set; }
        public string AntigenTestSNOMEDCode { get; set; }
        public string VirusTestSNOMEDCode { get; set; }
        public int RetryCount { get; set; }
        public int RetrySleepDurationInMilliseconds { get; set; }
        public int TimeoutInMilliseconds { get; set; }
        public int AccessTokenRetryCount { get; set; }
        public int AccessTokenRetrySleepDurationInMilliseconds { get; set; }
        public int AccessTokenTimeoutInMilliseconds { get; set; }
        public bool DisableP5 { get; set; }
        public bool DisableP5Plus { get; set; }
        public bool DisableP9 { get; set; }
        public bool AllowAllOtherThanP5AndP5PlusAndP9 { get; set; }
    }
}
