namespace CovidCertificate.Backend.Interfaces
{
    public interface IQrImageGenerator
    {
        /// <summary>
        /// Gets a raw byte data representing a QR code image
        /// </summary>
        /// <param name="content">The data to encode in the QR code</param>
        /// <param name="ppm">Pixels Per Module</param>
        /// <returns></returns>
        byte[] GenerateQrCodeRaw(string content, int ppm = 20);

        /// <summary>
        /// Gets a base64 image string got the current QR code
        /// </summary>
        /// <param name="content">The data to encode in the QR code</param>
        /// <param name="ppm">Pixels Per Module</param>
        /// <returns></returns>
        string GenerateQrCodeString(string content, int ppm = 20);
    }
}
