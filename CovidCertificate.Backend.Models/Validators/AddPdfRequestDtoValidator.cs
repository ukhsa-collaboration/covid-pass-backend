using CovidCertificate.Backend.Models.RequestDtos;
using FluentValidation;
using System;

namespace CovidCertificate.Backend.Models.Validators
{
    public class AddPdfRequestDtoValidator : AbstractValidator<AddPdfCertificateRequestDto>
    {
        public AddPdfRequestDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Email).EmailAddress();
            RuleFor(x => x.QrCodeToken).NotEmpty();
            RuleFor(x => x.TemplateName).NotEmpty();
            RuleFor(x => x.Expiry).NotEqual(default(DateTime));
            RuleFor(x => x.CertificateType).IsInEnum();
        }
    }
}
