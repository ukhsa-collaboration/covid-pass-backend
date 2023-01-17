namespace CovidCertificate.Backend.Models.Settings
{
    public class BlobServiceSettings
    {
        public int GetImageRetryCount { get; set; }
        public int GetImageRetrySleepDurationInMilliseconds { get; set; }
    }
}
