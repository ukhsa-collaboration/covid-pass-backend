using CovidCertificate.Backend.Models.Enums;
using FluentValidation;
using System;
using System.Text;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Interfaces.UserInterfaces;

namespace CovidCertificate.Backend.Models.RequestDtos
{
    public class AddPdfCertificateRequestDto : IUserBaseInformation
    {
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime Expiry { get; set; }
        public DateTime EligibilityPeriod { get; set; }
        public string QrCodeToken { get; set; }
        public string TemplateName { get; set; }
        public string Email { get; set; }
        public CertificateType CertificateType { get; set; }
        public string UniqueCertificateIdentifier { get; set; }
        public string LanguageCode { get; set; }
        public GetHtmlRequestDto GetHtmlDto()
        {

            return new GetHtmlRequestDto
            {
                Expiry = Expiry,
                EligibilityPeriod = EligibilityPeriod,
                TemplateName = TemplateName,
                Name = Name,
                DateOfBirth = DateOfBirth,
                QrCodeToken = QrCodeToken,
                CertificateType = CertificateType,
                UniqueCertificateIdentifier = UniqueCertificateIdentifier,
                LanguageCode = LanguageCode
            };
        }
        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Email:").Append(this.Email??"").AppendLine();
            sb.Append("Expiry:").Append(this.Expiry).AppendLine();
            sb.Append("Name:").Append(this.Name??"").AppendLine();
            sb.Append("CertificateType:").Append(this.CertificateType).AppendLine();
            sb.Append("EligibilityPeriod:").Append(this.EligibilityPeriod).AppendLine();
            sb.Append("TemplateName:").Append(this.TemplateName??"").AppendLine();
            sb.Append("DOB:").Append(this.DateOfBirth).AppendLine();
            sb.Append("QrCodeToken:").Append(this.QrCodeToken??"").AppendLine();
            sb.Append("Language:").Append(this.LanguageCode ?? "").AppendLine();
            return sb.ToString();
        }
    }
}
