namespace CovidCertificate.Backend.Models.RequestDtos
{
    public class PdfGenerationRequestInternationalDto
    {
        public string Email { get; set; }
        public PdfContent PdfContent { get; set; }
        public string Name { get; set; }
        public string LanguageCode { get; set; }
    }

    public class PdfContent
    {
        public string Body { get; set; }
        public string LanguageCode { get; set; }
    }
}
