namespace CovidCertificate.Backend.Models.Settings
{
    public class NotificationTemplates
    {
        public NotificationTemplate TwoFactor { get; set; }
        public NotificationTemplate PdfGeneration { get; set; }
        public NotificationTemplate WelshPdfGeneration { get; set; }
        public NotificationTemplate DomesticPdf { get; set; }
        public NotificationTemplate InternationalPdf { get; set; }
        public NotificationTemplate EmailPdfFailureSizeLimitExceeded { get; set; }
    }
}
