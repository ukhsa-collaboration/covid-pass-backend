namespace CovidCertificate.Backend.Models.DataModels.PdfGeneration
{
    public class HandlebarsRecoveryDto
    {
        public string Name { get; set; }
        public string DateOfBirth { get; set; }
        public string QrCode { get; set; }
        public string DateOfFirstPositiveTestResult { get; set; }
        public string CertificateType { get; set; }
        public string DiseaseTargeted { get; set; }
        public string CountryOfTest { get; set; }
        public string CertificateIssuer { get; set; }
        public string CertificateValidFrom { get; set; }
        public string CertificateValidUntil { get; set; }
        public string PageNumber { get; set; }
        public string TotalNumberOfPages { get; set; }
    }
}
