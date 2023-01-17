using CovidCertificate.Backend.Models.RequestDtos;
using FluentValidation;
using System;

namespace CovidCertificate.Backend.Models.Validators
{
    public class PdfCertificateRequestValidator : AbstractValidator<PdfCertificateRequest>
    {
        public PdfCertificateRequestValidator()
        {
            RuleFor(x => x.Expiry).GreaterThan(DateTime.UtcNow);
            RuleFor(x => x.Email).EmailAddress();
            RuleFor(x => x.TemplateName).NotEmpty();
            RuleFor(x => x.QrCodeToken).NotEmpty().Custom((x,cc) =>
            {
                var sections = x.Split('.');
                if(sections.Length != 3)
                    cc.AddFailure("QrCodeToken", "Qr Code is not in the right format");
            });
            RuleFor(x => x.CertificateType).IsInEnum();
        }
    }
}
