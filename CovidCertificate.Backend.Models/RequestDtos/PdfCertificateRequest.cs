using CovidCertificate.Backend.Models.Enums;
using System;
using System.Text;

namespace CovidCertificate.Backend.Models.RequestDtos
{
    public class PdfCertificateRequest
    {
        public DateTime Expiry { get; set; }
        public string QrCodeToken { get; set; }
        public string Email { get; set; }
        public string TemplateName { get; set; }
        public CertificateType CertificateType { get; set; }
        public DateTime ValidityEndDate { get; set; }
        public DateTime EligibilityEndDate { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Email:").Append(this.Email??"").AppendLine();
            sb.Append("Expiry:").Append(this.Expiry).AppendLine();
            sb.Append("CertificateType:").Append(this.CertificateType).AppendLine();
            sb.Append("LangCode:").Append(this.TemplateName??"").AppendLine();
            sb.Append("QrCodeToken:").Append(this.QrCodeToken??"").AppendLine();
            sb.Append("ValidityEndDate:").Append(this.ValidityEndDate).AppendLine();
            sb.Append("EligibilityEndDate:").Append(this.EligibilityEndDate).AppendLine();

            return sb.ToString();
        }
    }
}
