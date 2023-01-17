using CovidCertificate.Backend.Models.RequestDtos;
using FluentValidation;

namespace CovidCertificate.Backend.Models.Validators
{
    public class EmailPdfRequestDtoValidator : AbstractValidator<EmailPdfRequestDto>
    {
        public EmailPdfRequestDtoValidator()
        {
            RuleFor(x => x.PdfData).NotEmpty();
            RuleFor(x => x.Email).EmailAddress();
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.LanguageCode).NotEmpty();
        }
    }
}
