using CovidCertificate.Backend.Models.RequestDtos;
using FluentValidation;

namespace CovidCertificate.Backend.Models.Validators
{
    public class SendInternationalEmailDtoValidator : AbstractValidator<SendInternationalEmailDto>
    {
        public SendInternationalEmailDtoValidator()
        {
            RuleFor(x => x.Email).NotEmpty();
            RuleFor(x => x.Email).EmailAddress();
        }
    }
}
