namespace CovidCertificate.Backend.Models.Settings
{
    public class MongoDbSettings
    {
        public string DatabaseName { get; set; }
        public uint MaxDeleteExecutionTimeWarning { get; set; }
        public uint MaxReadExecutionTimeWarning { get; set; }
        public uint MaxWriteExecutionTimeWarning { get; set; }
        public uint MaxUpdateExecutionTimeWarning { get; set; }
        public int RetryCount { get; set; }
        public int RetrySleepDuration { get; set; }
    }
}
