using CovidCertificate.Backend.Models.Enums;

namespace CovidCertificate.Backend.Interfaces.Certificates
{
    public interface IProofingLevelValidatorService
    {
        bool ValidateProofingLevel(string idToken);
        IdentityProofingLevel GetProofingLevel(string idToken);
        void VerifyProofingLevel(string identityToken);
    }
}
