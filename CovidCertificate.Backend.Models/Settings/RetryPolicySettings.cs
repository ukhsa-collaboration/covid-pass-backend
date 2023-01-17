namespace CovidCertificate.Backend.Models.Settings
{
    public class RetryPolicySettings
    {
        public int RetryCount { get; set; }
        public int RetrySleepDurationInMilliseconds { get; set; }
        public int TimeoutInMilliseconds { get; set; }
    }
}
