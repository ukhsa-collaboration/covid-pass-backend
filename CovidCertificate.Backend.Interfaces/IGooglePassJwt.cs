using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IGooglePassJwt
    {
        Task<string> GenerateJwtAsync(CovidPassportUser user, QRType qrType, string languageCode, int doseNumber, string apiKey, string idToken = "");
    }
}
