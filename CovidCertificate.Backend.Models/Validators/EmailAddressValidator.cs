using FluentValidation;

namespace CovidCertificate.Backend.Models.Validators
{
    public class EmailAddressValidator : AbstractValidator<string>
    {
        public EmailAddressValidator()
        {
            RuleFor(x => x).NotEmpty().EmailAddress().Must(x => !x.Contains(' '));
        }
    }
}
