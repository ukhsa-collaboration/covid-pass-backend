using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using System.IO;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IGenerateApplePass
    {
        Task<MemoryStream> GeneratePassAsync(CovidPassportUser covidPassportUser, QRType qrType, string languageCode, string idToken = "", int doseNumber = 0);
    }
}