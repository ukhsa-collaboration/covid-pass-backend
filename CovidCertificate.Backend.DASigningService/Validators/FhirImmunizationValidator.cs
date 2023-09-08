using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;
using FluentValidation.Results;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.DASigningService.Validators
{
    public class FhirImmunizationValidator : AbstractValidator<Immunization>
    {
        private IVaccinationMapper vaccinationMapper;
        
        public FhirImmunizationValidator(IVaccinationMapper vaccinationMapper)
        {
            this.vaccinationMapper = vaccinationMapper;
            
            RuleFor(x => x.VaccineCode).Cascade(CascadeMode.Stop).NotNull()
                .WithMessage("Immunization.VaccineCode missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_IMMUNIZATION_VACCINECODE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.VaccineCode.Coding).Cascade(CascadeMode.Stop).NotEmpty()
                .When(x => x.VaccineCode != null)
                .WithMessage("Immunization.VaccineCode.Coding missing or empty.")
                .WithErrorCode(
                    ErrorCode.FHIR_IMMUNIZATION_VACCINECODE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.VaccineCode.Coding.FirstOrDefault()).Cascade(CascadeMode.Stop).NotEmpty()
                .When(x => x.VaccineCode?.Coding != null)
                .WithMessage("Immunization.VaccineCode.Coding[0] missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_IMMUNIZATION_VACCINECODE_CODE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.VaccineCode.Coding.FirstOrDefault().Code).Cascade(CascadeMode.Stop).NotEmpty()
                .When(x => x.VaccineCode?.Coding != null)
                .When(x => x.VaccineCode?.Coding.Count > 0)
                .WithMessage("Immunization.VaccineCode.Coding[0].Code missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_IMMUNIZATION_VACCINECODE_CODE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Occurrence).Cascade(CascadeMode.Stop).NotNull()
                .WithMessage("Immunization.Occurence missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_IMMUNIZATION_OCCURENCEDATETIME_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.LotNumber).Cascade(CascadeMode.Stop).NotNull()
                .WithMessage("Immunization.LotNumber missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_IMMUNIZATION_LOTNUMBER_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.ProtocolApplied).Cascade(CascadeMode.Stop).NotEmpty()
                .WithMessage("Immunization.ProtocolApplied missing or empty.")
                .WithErrorCode(
                    ErrorCode.FHIR_IMMUNIZATION_PROTOCOLAPPLIED_DOSENUMBER_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));
RuleFor(x => x.ProtocolApplied.FirstOrDefault().DoseNumber).Cascade(CascadeMode.Stop).NotEmpty()
                .When(x => x.ProtocolApplied?.FirstOrDefault() != null)
                .WithMessage("Immunization.ProtocolApplied[0].DoseNumber missing or empty.")
                .WithErrorCode(
                    ErrorCode.FHIR_IMMUNIZATION_PROTOCOLAPPLIED_DOSENUMBER_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));
            RuleFor(x => x.ProtocolApplied.FirstOrDefault()).Cascade(CascadeMode.Stop).NotEmpty()
                .When(x => x.ProtocolApplied != null)
                .WithMessage("Immunization.ProtocolApplied[0] missing or empty.")
                .WithErrorCode(
                    ErrorCode.FHIR_IMMUNIZATION_PROTOCOLAPPLIED_DOSENUMBER_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            
            
            RuleFor(x => ((PositiveInt) x.ProtocolApplied.FirstOrDefault().DoseNumber).Value).Cascade(CascadeMode.Stop).LessThanOrEqualTo( x => ((PositiveInt) x.ProtocolApplied.FirstOrDefault().SeriesDoses).Value)
                .When(x => x.ProtocolApplied?.FirstOrDefault()?.SeriesDoses != null)
                .When(x => ((PositiveInt)x.ProtocolApplied?.FirstOrDefault()?.SeriesDoses)?.Value > 1)
                .WithMessage("Immunization.ProtocolApplied[0].DoseNumber is larger than Immunization.ProtocolApplied[0].SeriesDoses.")
                .WithErrorCode(
                    ErrorCode.FHIR_IMMUNIZATION_PROTOCOLAPPLIED_DOSENUMBER_LARGER_THAN_SERIESDOSES.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x).Cascade(CascadeMode.Stop).MustAsync(async (x, cancellationToken) => await SeriesDosesFromPayloadIsLessThanOrEqualToSeriesDosesFromMapperAsync(x))
                .When(x => !string.IsNullOrEmpty(x.VaccineCode?.Coding?.FirstOrDefault()?.Code))
                .When(x => x.ProtocolApplied?.FirstOrDefault()?.SeriesDoses != null)
                .When(x => ((PositiveInt) x.ProtocolApplied?.FirstOrDefault()?.DoseNumber)?.Value < ((PositiveInt) x.ProtocolApplied?.FirstOrDefault()?.SeriesDoses)?.Value) //When this is true, the vaccine is not a booster shot
                .WhenAsync(async (x, cancellationToken) => await IsSnomedCodeFromPayloadRecognizedAsync(x))
                .WithMessage("Immunization.ProtocolApplied[0].DoseNumber is smaller than Immunization.ProtocolApplied[0].SeriesDoses && Immunization.ProtocolApplied[0].SeriesDoses is greater than the SeriesDoses for the Vaccine type.")
                .WithErrorCode(
                    ErrorCode.FHIR_IMMUNIZATION_NOTBOOSTER_PROTOCOLAPPLIED_SERIESDOSES_LARGER_THAN_VACCINETYPE_SERIESDOSES.ToString(StringUtils.NumberFormattedEnumFormat));
            
            RuleFor(x => x).Cascade(CascadeMode.Stop).MustAsync(async (x, cancellationToken) => await IsSnomedCodeFromPayloadRecognizedAsync(x))
                .When(x => !string.IsNullOrEmpty(x.VaccineCode?.Coding?.FirstOrDefault()?.Code))
                .WithMessage("Immunization.VaccineCode.Coding[0].Code is not recognized as a valid Snomed code.")
                .WithErrorCode(
                    ErrorCode.FHIR_IMMUNIZATION_VACCINECODE_CODE_NOT_RECOGNIZED_AS_VALID_SNOMED.ToString(StringUtils.NumberFormattedEnumFormat));
        }

        private async Task<bool> SeriesDosesFromPayloadIsLessThanOrEqualToSeriesDosesFromMapperAsync(Immunization immunization) //Do not call this method without using ".WhenAsync(async (x, cancellationToken) => await IsSnomedCodeFromPayloadRecognized(x))"
        {
            var seriesDosesFromPayload = ((PositiveInt) immunization.ProtocolApplied?.FirstOrDefault()?.SeriesDoses)?.Value;
            var snomedCode = immunization.VaccineCode?.Coding?.FirstOrDefault()?.Code;
            VaccineMap vaccineMap;
            try
            {
                vaccineMap  = await vaccinationMapper.MapRawSnomedcodeValueAsync(snomedCode);
            }
            catch (VaccineMappingException)
            {
                return false;
            }
            var seriesDosesFromMapper = vaccineMap.TotalSeriesOfDoses;
            
            return seriesDosesFromPayload <= seriesDosesFromMapper;
        }
        
        private async Task<bool> IsSnomedCodeFromPayloadRecognizedAsync(Immunization immunization)
        {
            var snomedCode = immunization.VaccineCode?.Coding?.FirstOrDefault()?.Code;
            try
            {
                await vaccinationMapper.MapRawSnomedcodeValueAsync(snomedCode);
            }
            catch (VaccineMappingException)
            {
                return false;
            }

            return true;
        }

        protected override bool PreValidate(ValidationContext<Immunization> context, ValidationResult result)
        {
            if (context.InstanceToValidate.Id == null)
            {
                var validationFailureObj = new ValidationFailure("", "Immunization Missing.");
                validationFailureObj.ErrorCode = ErrorCode.FHIR_IMMUNIZATION_MISSING.ToString(StringUtils.NumberFormattedEnumFormat);
                result.Errors.Add(validationFailureObj);
                return false;
            }
            return base.PreValidate(context, result);
        }
    }
}
