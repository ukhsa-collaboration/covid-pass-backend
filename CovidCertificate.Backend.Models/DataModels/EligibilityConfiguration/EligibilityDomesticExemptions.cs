namespace CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration
{
    public class EligibilityDomesticExemptions
    {
        public int CertificateExpiresInHours { get; private set; }

        public EligibilityDomesticExemptions(int certificateExpiresInHours)
        {
            this.CertificateExpiresInHours = certificateExpiresInHours;
        }
    }
}
