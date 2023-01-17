using CovidCertificate.Backend.Interfaces;
using QRCoder;
using System;
using QRGenerator = QRCoder.QRCodeGenerator;

namespace CovidCertificate.Backend.Services.QrCodes
{
    public class QrImageGenerator : IQrImageGenerator
    {
        /// <summary>
        /// Gets a raw byte data representing a QR code image
        /// </summary>
        /// <param name="content">The data to encode in the QR code</param>
        /// <param name="ppm">Pixels Per Module</param>
        /// <returns></returns>
        public byte[] GenerateQrCodeRaw(string content, int ppm = 20)
        {
            var qrData = GenerateQrCodeData(content);
            var code = new BitmapByteQRCode(qrData);
            return code.GetGraphic(ppm);
        }

        /// <summary>
        /// Gets a base64 image string got the current QR code
        /// </summary>
        /// <param name="content">The data to encode in the QR code</param>
        /// <param name="ppm">Pixels Per Module</param>
        /// <returns></returns>
        public string GenerateQrCodeString(string content, int ppm = 20)
        {
            var qrData = GenerateQrCodeData(content);
            var code = new PngByteQRCode(qrData);
            var qrCodeBytes =  code.GetGraphic(ppm);
            return Convert.ToBase64String(qrCodeBytes);

        }

        private static QRCodeData GenerateQrCodeData(string inputText)
        {
            if (string.IsNullOrEmpty(inputText))
                throw new ArgumentNullException("QrCode cannot be empty");

            var generator = new QRGenerator();

            return generator.CreateQrCode(inputText, QRGenerator.ECCLevel.M, eciMode: QRGenerator.EciMode.Default);
        }

    }
}
