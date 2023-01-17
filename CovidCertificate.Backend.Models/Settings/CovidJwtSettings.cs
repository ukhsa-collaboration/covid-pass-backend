namespace CovidCertificate.Backend.Models.Settings
{
    public class CovidJwtSettings
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpiryInDays { get; set; }
    }
}
