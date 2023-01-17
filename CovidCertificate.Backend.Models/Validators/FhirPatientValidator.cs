using FluentValidation;
using Hl7.Fhir.Model;
using System.Linq;
using CovidCertificate.Backend.Utils.Extensions;

namespace CovidCertificate.Backend.Models.Validators
{

    public class FhirPatientBundleValidator : AbstractValidator<Bundle>
    {
        public class FhirPatientValidator : AbstractValidator<Patient>
        {
            public FhirPatientValidator()
            {
                RuleFor(x => x.Identifier).Cascade(CascadeMode.Stop)
                    .NotEmpty()
                    .NotNull();     

                RuleFor(x => x.Identifier.First().Value).Cascade(CascadeMode.Stop)
                    .NotEmpty()
                    .Matches(StringUtils.NhsNumberRegex)
                    .When(x => x.Identifier?.FirstOrDefault() != null);              

                RuleFor(x => x.BirthDate).Cascade(CascadeMode.Stop)
                    .NotEmpty()
                    .Matches(Date.PATTERN);
            }
        }

        public FhirPatientBundleValidator()
        {
            RuleFor(x => x.Entry).Cascade(CascadeMode.Stop)
                .NotEmpty();

            RuleFor(x => x.Entry.Count).Cascade(CascadeMode.Stop)
                .Equal(1)
                .When(x => x.Entry != null);

            RuleFor(x => x.Entry.First().Resource as Patient).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .SetValidator(new FhirPatientValidator())
                .When(x => x.Entry?.First() != null);
        }
    }
}
