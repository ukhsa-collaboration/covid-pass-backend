using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.ResponseDtos;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IGeneratePassData
    {
        public Task<(QRcodeResponse qr, Certificate cert)> GetPassDataAsync(CovidPassportUser user, string idToken, QRType type, string apiKey, string languageCode = "en");
    }
}
