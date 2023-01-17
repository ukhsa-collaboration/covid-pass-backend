namespace CovidCertificate.Backend.Models.Settings
{
    public class OdsApiSettings
    {
        public string OdsApiBaseUrl { get; set; }
        public int RetryCount { get; set; }
        public int RetrySleepDurationInMilliseconds { get; set; }
        public int TimeoutInMilliseconds { get; set; }
        public int OdsApiResponseLimit { get; set; }
    }
}
