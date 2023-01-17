using CovidCertificate.Backend.Models.RequestDtos;
using FluentValidation;

namespace CovidCertificate.Backend.Models.Validators
{
    public class SendCertificateDtoValidator : AbstractValidator<SendCertificateDto>
    {
        public SendCertificateDtoValidator()
        {
            RuleFor(x => x.Email).NotEmpty();
            RuleFor(x => x.Email).EmailAddress();
        }
    }
}
