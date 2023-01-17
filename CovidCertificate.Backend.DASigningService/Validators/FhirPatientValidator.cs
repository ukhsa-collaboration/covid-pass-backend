using System.Linq;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;
using FluentValidation.Results;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.DASigningService.Validators
{
    public class FhirPatientValidator : AbstractValidator<Patient>
    {
        public FhirPatientValidator()
        {    
            RuleFor(x => x.Name).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Patient name missing.")
                .WithErrorCode(ErrorCode.FHIR_PATIENT_NAME_MISSING.ToString(StringUtils.NumberFormattedEnumFormat)).Must(y => y.Any()).WithMessage("Patient name missing.")
                .WithErrorCode(ErrorCode.FHIR_PATIENT_NAME_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));
            
            RuleFor(x => x.Name.FirstOrDefault().Given).Cascade(CascadeMode.Stop).NotEmpty()
                .When(x => x.Name?.FirstOrDefault() != null)
                .WithMessage("Patient.Name[0].Given missing.")
                .WithErrorCode(ErrorCode.FHIR_PATIENT_GIVEN_NAME_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));
            
            RuleFor(x => x.Name.FirstOrDefault().Family).Cascade(CascadeMode.Stop).NotEmpty()
                .When(x => x.Name?.FirstOrDefault() != null)
                .WithMessage("Patient.Name[0].Family missing.")
                .WithErrorCode(ErrorCode.FHIR_PATIENT_FAMILY_NAME_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.BirthDate).Cascade(CascadeMode.Stop).NotEmpty()
                .WithMessage("Patient.BirthDate missing.")
                .WithErrorCode(ErrorCode.FHIR_PATIENT_BIRTHDATE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

        }

        protected override bool PreValidate(ValidationContext<Patient> context, ValidationResult result)
        {
            if (context.InstanceToValidate == null)
            {
                var validationFailureObj = new ValidationFailure("", "Patient missing.");
                validationFailureObj.ErrorCode = ErrorCode.FHIR_PATIENT_MISSING.ToString(StringUtils.NumberFormattedEnumFormat);
                result.Errors.Add(validationFailureObj);

                return false;
            }
            return base.PreValidate(context, result);
        }
    }
}
