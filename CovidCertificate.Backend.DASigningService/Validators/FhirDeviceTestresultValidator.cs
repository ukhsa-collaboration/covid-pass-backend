using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.Interfaces.PKINationaBackend;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;
using FluentValidation.Results;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.DASigningService.Validators
{
    public class FhirDeviceTestResultValidator : AbstractValidator<Device>
    {
        private readonly INationalBackendService nationalBackendService;
        public FhirDeviceTestResultValidator(INationalBackendService nationalBackendService)
        {
            this.nationalBackendService = nationalBackendService;
            CascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Identifier)
                .Must(x => x.Any())
                .WithMessage("Device.Identifier missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_DEVICE_IDENTIFIER_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Identifier)
                .Must(HasLoincIdentifier)
                .When(x => x.Identifier.Any())
                .WithMessage("Device.Identifier has no LOINC identifier.")
                .WithErrorCode(
                    ErrorCode.FHIR_DEVICE_LOINC_IDENTIFIER_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Identifier)
                .Must(LoincIdentifierValid)
                .When(x => HasLoincIdentifier(x.Identifier))
                .WithMessage("LOINC identifier invalid.")
                .WithErrorCode(
                    ErrorCode.FHIR_DEVICE_LOINC_IDENTIFIER_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.DeviceName)
                .Must(HasDeviceName)
                .When(x => HasLoincIdentifier(x.Identifier) && LoincIdentifierValid(x.Identifier) && !IsRAT(x.Identifier))
                .WithMessage("Device.DeviceName missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_DEVICE_DEVICENAME_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.DeviceName[0].Name)
                .NotNull()
                .When(x => HasLoincIdentifier(x.Identifier) && LoincIdentifierValid(x.Identifier) && !IsRAT(x.Identifier) && x.DeviceName != null && x.DeviceName.Any())
                .WithMessage("Device.DeviceName[0].Name missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_DEVICE_DEVICENAME_NAME_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Manufacturer)
                .NotNull()
                .When(x => HasLoincIdentifier(x.Identifier) && LoincIdentifierValid(x.Identifier) && !IsRAT(x.Identifier))
                .WithMessage("Device.Manufacturer missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_DEVICE_MANUFACTURER_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Identifier)
                .Must(HasRATIdentifier)
                .When(x => HasLoincIdentifier(x.Identifier) && LoincIdentifierValid(x.Identifier) && IsRAT(x.Identifier))
                .WithMessage("Device.Identifier has no RAT identifier.")
                .WithErrorCode(
                    ErrorCode.FHIR_DEVICE_RAT_IDENTIFIER_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Identifier)
                .MustAsync(ValidEURATIdentifierAsync)
                .When(x => HasLoincIdentifier(x.Identifier) && LoincIdentifierValid(x.Identifier) && IsRAT(x.Identifier) && HasRATIdentifier(x.Identifier))
                .WithMessage("Device.Identifier has unrecognized RAT identifier number in EU value set.")
                .WithErrorCode(
                    ErrorCode.FHIR_DEVICE_RAT_IDENTIFIER_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));
        }

        private bool HasDeviceName(List<Device.DeviceNameComponent> deviceNames)
        {
            return deviceNames != null && deviceNames.Any();
        }

        private bool HasLoincIdentifier(List<Identifier> identifiers)
        {
            try
            {
                GetLoincIdentifier(identifiers);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool HasRATIdentifier(List<Identifier> identifiers)
        {
            try
            {
                var ratIdentifier = GetRATIdentifier(identifiers);
                return !string.IsNullOrEmpty(ratIdentifier.Value);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> ValidEURATIdentifierAsync(List<Identifier> identifiers, CancellationToken arg)
        {
            Identifier identifier = GetRATIdentifier(identifiers);

            var valueSets = await nationalBackendService.GetEUValueSetResponseAsync(false);
            var testManufacturersId = valueSets.ValueSets.TestManufacturers.Keys.ToList();
            
            return testManufacturersId.Contains(identifier.Value);
        }
        

        private Identifier GetRATIdentifier(List<Identifier> identifiers)
        {
            return identifiers.Single(x => "https://covid-19-diagnostics.jrc.ec.europa.eu/devices/hsc-common-recognition-rat".Equals(x.System));
        }

        private bool LoincIdentifierValid(List<Identifier> identifiers)
        {
            Identifier identifier = GetLoincIdentifier(identifiers);
            return "LP217198-3".Equals(identifier.Value) || "LP6464-4".Equals(identifier.Value);
        }

        private bool IsRAT(List<Identifier> identifiers)
        {
            Identifier identifier = GetLoincIdentifier(identifiers);
            return "LP217198-3".Equals(identifier.Value);
        }

        private Identifier GetLoincIdentifier(List<Identifier> identifiers)
        {
            return identifiers.Single(x => "http://loinc.org".Equals(x.System));
        }

        protected override bool PreValidate(ValidationContext<Device> context, ValidationResult result)
        {
            if (context.InstanceToValidate.Id == null)
            {
                var validationFailureObj = new ValidationFailure("", "Device missing.");
                validationFailureObj.ErrorCode = ErrorCode.FHIR_DEVICE_MISSING.ToString(StringUtils.NumberFormattedEnumFormat);
                result.Errors.Add(validationFailureObj);
                return false;
            }
            return base.PreValidate(context, result);
        }
    }
}
