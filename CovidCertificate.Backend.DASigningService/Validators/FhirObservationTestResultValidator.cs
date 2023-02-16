using System;
using System.Linq;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.Interfaces.DateTimeProvider;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;
using FluentValidation.Results;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Configuration;

namespace CovidCertificate.Backend.DASigningService.Validators
{
    public class FhirObservationTestResultValidator : AbstractValidator<Observation>
    {
        private readonly IDateTimeProviderService dateTimeProviderService;

        private IConfiguration configuration;

        public FhirObservationTestResultValidator(IConfiguration configuration,
            IDateTimeProviderService dateTimeProviderService)
        {
            this.configuration = configuration;
            this.dateTimeProviderService = dateTimeProviderService;

            RuleFor(x => x.Value).Cascade(CascadeMode.Stop).NotNull()
                .WithMessage("Observation.ValueCodableConcept.Coding[0] missing.")
                .WithErrorCode(
                        ErrorCode.FHIR_OBSERVATION_VALUE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Value).Cascade(CascadeMode.Stop).Must(IsCodableConcept)
                .When(x => x.Value != null)
                .WithMessage("Observation.ValueCodableConcept.Coding[0] missing.")
                .WithErrorCode(
                        ErrorCode.FHIR_OBSERVATION_VALUE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => ((CodeableConcept)x.Value).Coding.FirstOrDefault()).Cascade(CascadeMode.Stop).NotEmpty()
                .When(x => x.Value != null && ((CodeableConcept)x.Value).Coding != null)
                .WithMessage("Observation.ValueCodableConcept.Coding[0] missing.")
                .WithErrorCode(
                        ErrorCode.FHIR_OBSERVATION_VALUE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => ((CodeableConcept)x.Value).Coding.FirstOrDefault().Code).Cascade(CascadeMode.Stop).NotEmpty()
                .When(x => x.Value != null && ((CodeableConcept)x.Value).Coding != null && ((CodeableConcept)x.Value).Coding.FirstOrDefault() != null)
                .WithMessage("Observation.ValueCodableConcept.Coding[0].Code missing.")
                .WithErrorCode(
                        ErrorCode.FHIR_OBSERVATION_VALUE_CODE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => ((CodeableConcept)x.Value).Coding.FirstOrDefault().Code).Cascade(CascadeMode.Stop).Equal("1322791000000100")
                .When(x => x.Value != null && ((CodeableConcept)x.Value).Coding != null && ((CodeableConcept)x.Value).Coding.FirstOrDefault() != null && ((CodeableConcept)x.Value).Coding.FirstOrDefault().Code != null)
                .WithMessage("Observation.ValueCodableConcept.Coding[0].Code invalid.")
                .WithErrorCode(
                        ErrorCode.FHIR_OBSERVATION_VALUE_CODE_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Effective).Cascade(CascadeMode.Stop).NotNull()
                .WithMessage("Observation.EffectiveDateTime missing.")
                .WithErrorCode(
                        ErrorCode.FHIR_OBSERVATION_EFFECTIVEDATETIME_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Effective).Cascade(CascadeMode.Stop).Must(IsWithinTimeBounds)
                .When(x => x.Effective != null)
                .WithMessage(GetTimeBoundsErrorMessage())
                .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_EFFECTIVEDATETIME_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Device).Cascade(CascadeMode.Stop).NotNull()
                .WithMessage("Observation.Device missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_DEVICE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Device.Reference).Cascade(CascadeMode.Stop).NotNull()
                .When(x => x.Device != null)
                .WithMessage("Observation.Device.Reference missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_DEVICE_REFERENCE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Status).Cascade(CascadeMode.Stop).NotNull()
                .WithMessage("Observation.Status missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_STATUS_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Status).Cascade(CascadeMode.Stop).Equal(ObservationStatus.Final)
                .When(x => x.Status != null)
                .WithMessage("Observation.Status not 'final'.")
                .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_STATUS_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Performer).Cascade(CascadeMode.Stop).NotNull()
                .WithMessage("Observation.Performer missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_PERFORMER_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Performer.Count).Cascade(CascadeMode.Stop).GreaterThan(0)
                .When(x => x.Performer != null)
                .WithMessage("Observation.Performer missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_PERFORMER_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Performer.FirstOrDefault().Reference).Cascade(CascadeMode.Stop).NotNull()
                .When(x => x.Performer.Any())
                .WithMessage("Observation.Performer[0].Reference missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_PERFORMER_REFERENCE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));
        }


        protected override bool PreValidate(ValidationContext<Observation> context, ValidationResult result)
        {
            if (context.InstanceToValidate.Id == null)
            {
                var validationFailureObj = new ValidationFailure("", "Observation Missing.");
                validationFailureObj.ErrorCode = ErrorCode.FHIR_OBSERVATION_MISSING.ToString(StringUtils.NumberFormattedEnumFormat);
                result.Errors.Add(validationFailureObj);
                return false;
            }
            return base.PreValidate(context, result);
        }


        private bool IsCodableConcept(DataType datatype)
        {
            return datatype is CodeableConcept;
        }

        private bool IsWithinTimeBounds(DataType datatype)
        {
            int validUntil = configuration.GetValue<int>("HoursAfterTestResultBeforeCertificateInvalid");

            FhirDateTime effectiveFhirDate = (FhirDateTime)datatype;
            DateTime effectiveDate = effectiveFhirDate.ToDateTimeOffset(TimeSpan.Zero).DateTime;


            if (effectiveDate.AddHours(validUntil) < dateTimeProviderService.UtcNow)
            {
                //EffectiveDate + maximum time after negative corona-test before we no longer consider the person safe is before today
                //meaning that the negative test is too old to be considered valid
                return false;
            }

            return true;
        }

        private string GetTimeBoundsErrorMessage()
        {
            int validUntil = configuration.GetValue<int>("HoursAfterTestResultBeforeCertificateInvalid");
            return $"Observation.EffectiveDateTime not within last {validUntil} hours.";
        }
    }
}
