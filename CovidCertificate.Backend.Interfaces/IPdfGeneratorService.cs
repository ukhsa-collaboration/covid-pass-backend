using CovidCertificate.Backend.Models.RequestDtos;
using System.IO;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IPdfGeneratorService
    {
        Task<Stream> GeneratePdfContentStreamAsync(PdfContent pdfContent);
        Task<Stream> GeneratePdfDocumentStreamAsync(PdfContent emailContent);
        Task<PdfContent> GetDirectDownloadPdfRequestObjectAsync(CovidPassportUser covidUser,
            Certificate certificate, string templateLanguage, IHtmlGeneratorService htmlGeneratorServic);
    }
}
