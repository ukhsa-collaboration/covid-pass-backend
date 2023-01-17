using System.Text;

namespace CovidCertificate.Backend.Models.RequestDtos
{
    public class PdfGenerationRequestDomesticDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string EmailContent { get; set; }
        public string LanguageCode { get; set; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Email:").Append(this.Email??"").AppendLine();
            sb.Append("Name:").Append(this.Name??"").AppendLine();
            sb.Append("EmailContent:").Append(this.EmailContent).AppendLine();
            sb.Append("LanguageCode:").Append(this.LanguageCode).AppendLine();

            return sb.ToString();
        }
    }
}