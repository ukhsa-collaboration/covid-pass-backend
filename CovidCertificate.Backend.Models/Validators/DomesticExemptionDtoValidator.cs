using CovidCertificate.Backend.Models.RequestDtos;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;

namespace CovidCertificate.Backend.Models.Validators
{
    public class DomesticExemptionDtoValidator : AbstractValidator<DomesticExemptionDto>
    {
        public DomesticExemptionDtoValidator()
        {
            RuleFor(x => x.DateOfBirth).NotEmpty();
            RuleFor(x => x.NhsNumber).NotEmpty().Matches(StringUtils.NhsNumberRegex);
        }
    }
}
