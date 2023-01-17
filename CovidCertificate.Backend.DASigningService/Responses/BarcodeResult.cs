using CovidCertificate.Backend.DASigningService.ErrorHandling;

namespace CovidCertificate.Backend.DASigningService.Responses
{
    public class BarcodeResult
    {
        public string Id { get; set; }
        public string CertificateType { get; set; }
        public bool CanProvide { get; set; }
        public string Barcode { get; set; }
        public Error Error { get; set; }
    }
}
