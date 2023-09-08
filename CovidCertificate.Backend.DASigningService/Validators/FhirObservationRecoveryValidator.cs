using System;
using System.Linq;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using FluentValidation;
using FluentValidation.Results;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Configuration;
using CovidCertificate.Backend.Interfaces;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Utils.Extensions;
using CovidCertificate.Backend.Interfaces.DateTimeProvider;

namespace CovidCertificate.Backend.DASigningService.Validators
{
    public class FhirObservationRecoveryValidator : AbstractValidator<Observation>
    {
        private readonly IDateTimeProviderService dateTimeProviderService;
        private readonly string blobContainer;
        private readonly string blobFilename;

        private IConfiguration configuration;
        private IBlobFilesInMemoryCache<TestMappings> mappingCache;

        public FhirObservationRecoveryValidator(IConfiguration configuration,
            IBlobFilesInMemoryCache<TestMappings> mappingCache,
            IDateTimeProviderService dateTimeProviderService)
        {
            this.configuration = configuration;
            this.mappingCache = mappingCache;
            this.dateTimeProviderService = dateTimeProviderService;
            blobContainer = configuration["BlobContainerNameTestMappings"];
            blobFilename = configuration["BlobFileNameTestMappings"];

            RuleFor(x => x.Value).Cascade(CascadeMode.Stop).NotNull()
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

            RuleFor(x => ((CodeableConcept)x.Value).Coding.FirstOrDefault().Code).Cascade(CascadeMode.Stop).Equal("1240581000000104")
                .When(x => x.Value != null && ((CodeableConcept)x.Value).Coding != null && ((CodeableConcept)x.Value).Coding.FirstOrDefault() != null && ((CodeableConcept)x.Value).Coding.FirstOrDefault()?.Code != null)
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

            RuleFor(x => x.Device.Identifier).Cascade(CascadeMode.Stop).NotNull()
                .When(x => x.Device != null)
                .WithMessage("Observation.Device.Identifier missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_DEVICE_IDENTIFIER_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Device.Identifier.Value).Cascade(CascadeMode.Stop).NotNull()
                .When(x => x.Device != null && x.Device.Identifier != null)
                .WithMessage("Observation.Device.Identifier.Value missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_DEVICE_IDENTIFIER_VALUE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Device.Identifier.Value).Cascade(CascadeMode.Stop)
                .MustAsync(async (x, ct) => await IsAcceptedTestMethodAsync(x))
                .When(x => x.Device != null && x.Device.Identifier != null && x.Device.Identifier.Value != null)
                .WithMessage("Observation.Device.Identifier.Value not among allowed values.")
                .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_DEVICE_IDENTIFIER_VALUE_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Status).Cascade(CascadeMode.Stop).NotNull()                
                .WithMessage("Observation.Status missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_STATUS_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Status).Cascade(CascadeMode.Stop).Equal(ObservationStatus.Final)
                .When(x => x.Status != null)
                .WithMessage("Observation.Status not 'final'.")
                .WithErrorCode(
                    ErrorCode.FHIR_OBSERVATION_STATUS_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));
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

        private bool IsWithinTimeBounds(DataType datatype)
        {
            int validAfter = configuration.GetValue<int>("HoursAfterRecoveryTestBeforeCertificateValid");
            int validUntil = configuration.GetValue<int>("HoursAfterRecoveryTestBeforeCertificateInvalid");

            FhirDateTime effectiveFhirDate = (FhirDateTime)datatype;
            DateTime effectiveDate = effectiveFhirDate.ToDateTimeOffset(TimeSpan.Zero).DateTime;

            if (effectiveDate.AddHours(validAfter) > dateTimeProviderService.UtcNow)
            {
                //EffectiveDate + minimum time which must elapse before a positive corona-test may result in a 2D barcode is after today
                //meaning that the positive test is too recent to be considered valid
                return false;
            }

            if (effectiveDate.AddHours(validUntil) < dateTimeProviderService.UtcNow)
            {
                //EffectiveDate + maximum time after positive corona-test before we no longer consider the person to be immune is before today
                //meaning that the positive test is too old to be considered valid
                return false;
            }

            return true;
        }

        private async Task<bool> IsAcceptedTestMethodAsync(string deviceIdentifierValue)
        {
            var mappings = await mappingCache.GetFileAsync(blobContainer, blobFilename);
            var testKitMapping = mappings.Type;

            var acceptedTestTypes = testKitMapping.Keys;

            return acceptedTestTypes.Contains(deviceIdentifierValue, StringComparer.OrdinalIgnoreCase);
        }

        private String GetTimeBoundsErrorMessage()
        {
            int validAfter = configuration.GetValue<int>("HoursAfterRecoveryTestBeforeCertificateValid");
            int validUntil = configuration.GetValue<int>("HoursAfterRecoveryTestBeforeCertificateInvalid");
            return $"Observation.EffectiveDateTime not between {validAfter / 24} and {validUntil / 24} days ago.";
        }
    }
}
