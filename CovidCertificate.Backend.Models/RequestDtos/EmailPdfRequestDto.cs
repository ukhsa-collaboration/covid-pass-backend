using CovidCertificate.Backend.Models.Enums;
using System;
using System.Text;

namespace CovidCertificate.Backend.Models.RequestDtos
{
    public class EmailPdfRequestDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public byte[] PdfData { get; set; }
        public string LanguageCode { get; set; }
        public CertificateScenario CertificateScenario { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Name:").Append(this.Name ?? "").AppendLine();
            sb.Append("Email:").Append(this.Email ?? "").AppendLine();
            sb.Append("PdfData:").Append(Convert.ToBase64String(this.PdfData) ?? "").AppendLine();
            sb.Append("LanguageCode:").Append(this.LanguageCode ?? "").AppendLine();
            sb.Append("CertificateScenario:").Append(this.CertificateScenario).AppendLine();
            return sb.ToString();
        }
    }
}
