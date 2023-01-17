namespace CovidCertificate.Backend.Models.Settings
{
    public class PassSettings
    {
        public string PassProvider { get; set; }
        public string PassName { get; set; }
        public string PassOrigins { get; set; }
        public string GoogleImageUrl { get; set; }
        public string BackgroundColourDomestic { get; set; }
        public string BackgroundColourInternational { get; set; }
        public string BackgroundColourMandatory { get; set; }
        public string BackgroundColourVoluntary { get; set; }
        public string IssuerId { get; set; }
        public string Audience { get; set; }
        public string JwtType { get; set; }
        public string Iss { get; set; }
        public string UniqueId { get; set; }


    }
}
