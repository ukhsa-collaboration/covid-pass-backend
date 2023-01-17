namespace CovidCertificate.Backend.Models.Settings
{
    public class NhsLoginSettings
    {
        public string ClientId { get; set; }
        public string ClientAssertionType { get; set; }
        public string BaseUrl { get; set; }
        public string TokenRelativePath { get; set; }
        public string TokenUri => BaseUrl + TokenRelativePath;
        public int TokenLifeTime { get; set; }
        public string UserInfoRelativePath { get; set; }
        public string UserInfoEndpoint => BaseUrl + UserInfoRelativePath;
        public string AcceptedIdentityProofingLevel { get; set; }
        public string Issuer => BaseUrl;
        public string Audience { get; set; }
        public string WellKnownUrlRelativePath { get; set; }
        public string PublicKeyUrlRelativePath { get; set; }
        public string WellKnownUrl => BaseUrl + WellKnownUrlRelativePath;
        public string PublicKeyUrl => BaseUrl + PublicKeyUrlRelativePath;
    }
}