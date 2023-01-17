using CovidCertificate.Backend.Models.DataModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces.Certificates
{
    public interface IQRCodeGenerator
    {
        /// <summary>
        /// Generates a new QR code based off a certificate
        /// </summary>
        /// <param name="certificate">The certificate we want to generate a qr code for</param>
        /// <returns>The qr code</returns>
        Task<List<string>> GenerateQRCodesAsync(Certificate certificate,CovidPassportUser user, string barcodeIssuerCountry = "");
    }
}
