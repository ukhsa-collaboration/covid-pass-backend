using CovidCertificate.Backend.Models.RequestDtos;
using FluentValidation;

namespace CovidCertificate.Backend.Models.Validators
{
    public class InternationalEmailServiceBusRequestDtoValidator : AbstractValidator<InternationalEmailServiceBusRequestDto>
    {
        public InternationalEmailServiceBusRequestDtoValidator()
        {
            RuleFor(x => x.EmailToSendTo).EmailAddress();
            RuleFor(x => x.CovidPassportUser).NotEmpty();
            RuleFor(x => x.LanguageCode).NotEmpty();
        }
    }
}
