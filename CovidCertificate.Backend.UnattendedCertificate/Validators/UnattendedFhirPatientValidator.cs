using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CovidCertificate.Backend.UnattendedCertificate.ErrorHandling;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;
using FluentValidation.Results;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.UnattendedCertificate.Validators
{
    public class UnattendedFhirPatientValidator : AbstractValidator<Patient>
    {
        private static readonly Regex regex = new Regex(StringUtils.NhsNumberRegex);

        public UnattendedFhirPatientValidator()
        {
            CascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Identifier)
                .NotEmpty()
                .WithErrorCode(ErrorCode.FHIR_PATIENT_NHS_NUMBER_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Identifier.FirstOrDefault().Value)
                .NotEmpty()
                .WithErrorCode(ErrorCode.FHIR_PATIENT_NHS_NUMBER_MISSING.ToString(StringUtils.NumberFormattedEnumFormat))
                .When(x => x.Identifier.FirstOrDefault() != null)
                .Must(CheckNhsNumRegex)
                .WithErrorCode(ErrorCode.FHIR_PATIENT_NHS_NUMBER_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorCode(ErrorCode.FHIR_PATIENT_NAME_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Name.FirstOrDefault().Given)
                .NotEmpty()
                .When(x => x.Name?.FirstOrDefault() != null)
                .WithErrorCode(ErrorCode.FHIR_PATIENT_NAME_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Name.FirstOrDefault().Family)
                .NotEmpty()
                .When(x => x.Name?.FirstOrDefault() != null)
                .WithErrorCode(ErrorCode.FHIR_PATIENT_NAME_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.BirthDate)
                .NotEmpty()
                .WithErrorCode(ErrorCode.FHIR_PATIENT_BIRTHDATE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));
            
            RuleFor(x=>x.BirthDate)
                .NotEmpty()
                .WithErrorCode(ErrorCode.FHIR_PATIENT_BIRTHDATE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat))
                .When(x=>x.BirthDate!= null)
                .Must(CheckDateFormatRegex)
                .WithErrorCode(ErrorCode.FHIR_PATIENT_BIRTHDATE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));
        }

        private static bool CheckNhsNumRegex(string nhsNumber)
        {
            return regex.IsMatch(nhsNumber);
        }

        private static bool CheckDateFormatRegex(string arg)
        {
            return System.DateTime.TryParseExact(arg, DateUtils.FHIRDateFormat, null, DateTimeStyles.None, out var _);
        }

        protected override bool PreValidate(ValidationContext<Patient> context, ValidationResult result)
        {
            if (context.InstanceToValidate != null)
            {
                return base.PreValidate(context, result);
            }

            var validationFailureObj = new ValidationFailure("", "Patient missing.");
            validationFailureObj.ErrorCode = ErrorCode.FHIR_PATIENT_INVALID.ToString(StringUtils.NumberFormattedEnumFormat);
            result.Errors.Add(validationFailureObj);

            return false;
        }
    }
}
