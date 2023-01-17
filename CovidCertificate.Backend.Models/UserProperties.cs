using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.ResponseDtos;

namespace CovidCertificate.Backend.Models
{
    public class UserProperties
    {
        public GracePeriodResponse GracePeriod { get; set; }
        public IdentityProofingLevel IdentityProofingLevel { get; set; }
        public DomesticAccessLevel DomesticAccessLevel { get; set; }
        public string Country { get; set; } = "UNKNOWN-COUNTRY";
    }
}
