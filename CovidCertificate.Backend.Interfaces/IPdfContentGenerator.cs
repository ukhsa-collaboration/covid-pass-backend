using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.RequestDtos;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IPdfContentGenerator
    {
        Task<PdfContent> GenerateInternationalAsync(CovidPassportUser covidPassportUser, Certificate vaccinationCertificate, Certificate recoveryCertificate, string languageCode, PDFType type, int doseNumber);
    }
}
