using System;
using System.Linq;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.DASigningService.Requests;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace CovidCertificate.Backend.DASigningService.Validators
{
    public class Create2DDomesticBarcodeRequestValidator : AbstractValidator<Create2DDomesticBarcodeRequest>
    {
        private IConfiguration configuration;

        public Create2DDomesticBarcodeRequestValidator(IConfiguration configuration)
        {
            this.configuration = configuration;

            // Global validator options
            ValidatorOptions.Global.CascadeMode = CascadeMode.Stop;

            RuleFor(x => x.RegionSubscriptionNameHeader)
                .NotEmpty()
                .WithMessage(
                    $"Missing header '{DevolvedAdministrationBarcodeGeneratorFunction.RegionSubscriptionNameHeader}'")
                .WithErrorCode(ErrorCode.UNEXPECTED_SYSTEM_ERROR.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Body)
                .NotEmpty()
                .WithMessage("Request should not be empty.")
                .WithErrorCode(ErrorCode.FHIR_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Policy)
                .NotEmpty()
                .WithMessage("Policy query parameter missing.")
                .WithErrorCode(ErrorCode.POLICY_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Policy)
                .Must(IsPolicyValid)
                .WithMessage("The query parameter policy is invalid.")
                .WithErrorCode(ErrorCode.POLICY_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.PolicyMask)
                .NotEmpty()
                .WithMessage("PolicyMask query parameter missing.")
                .WithErrorCode(ErrorCode.POLICYMASK_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.PolicyMask)
                .Must(IsPolicyMaskBetweenBusinessValues)
                .WithMessage("PolicyMask did not contain an integer value between 0 and 255.")
                .WithErrorCode(ErrorCode.POLICYMASK_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.ValidFrom)
                .Must(IsPositiveInteger)
                .WithMessage("The query parameter validFrom did not contain a positive integer value.")
                .WithErrorCode(ErrorCode.VALIDFROM_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.ValidFrom)
                .Must(IsPositiveInteger)
                .WithMessage("The query parameter validTo did not contain a positive integer value.")
                .WithErrorCode(ErrorCode.VALIDTO_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x)
                .Must(x => IsDurationWithinBounds(x.ValidFrom, x.ValidTo))
                .WithMessage(GetTimeBoundsErrorMessage())
                .WithErrorCode(ErrorCode.VALIDTO_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));
        }

        private static bool IsPolicyMaskBetweenBusinessValues(string policyMaskValue)
        {
            if (int.TryParse(policyMaskValue, out int policyMask))
            {
                return policyMask > 0 && policyMask <= 255;
            }

            return false;
        }

        private static bool IsPositiveInteger(string validFromString)
        {
            if (long.TryParse(validFromString, out long validFrom))
            {
                return validFrom > 0;
            }

            return false;
        }
        
        private bool IsDurationWithinBounds(string validFromString, string validToString)
        {
            if (long.TryParse(validFromString, out long validFrom) && long.TryParse(validToString, out long validTo))
            {
                var validFromDateTime = DateUtils.UnixTimeSecondsToDateTime(validFrom);
                var validToDateTime = DateUtils.UnixTimeSecondsToDateTime(validTo);

                int minimumDomesticBarcodeDurationHours = configuration.GetValue<int>("MinimumDomesticBarcodeDurationHours");
                int maximumDomesticBarcodeDurationHours = configuration.GetValue<int>("MaximumDomesticBarcodeDurationHours");

                return validFromDateTime.AddHours(minimumDomesticBarcodeDurationHours) <= validToDateTime &&
                    validFromDateTime.AddHours(maximumDomesticBarcodeDurationHours) >= validToDateTime;
            }

            return false;
        }

        private String GetTimeBoundsErrorMessage()
        {
            int minimumDomesticBarcodeDurationHours = configuration.GetValue<int>("MinimumDomesticBarcodeDurationHours");
            int maximumDomesticBarcodeDurationHours = configuration.GetValue<int>("MaximumDomesticBarcodeDurationHours");
            return $"The query parameter validTo is invalid. The difference between validFrom and validTo must be at least {minimumDomesticBarcodeDurationHours / 24} days and no more than {maximumDomesticBarcodeDurationHours / 24} days.";
        }

        private static bool IsPolicyValid(string policy)
        {
            // Policy must be comma separated policies, empty not allowed
            var policies = policy.Split(",");

            if (!policies.Any())
            {
                return false;
            }

            foreach (var policyItem in policies)
            {
                if (string.IsNullOrEmpty(policyItem))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
