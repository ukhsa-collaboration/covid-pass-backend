using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.DASigningService.Requests;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace CovidCertificate.Backend.DASigningService.Validators
{
    public class Create2DBarcodeRequestValidator : AbstractValidator<Create2DBarcodeRequest>
    {
        private IConfiguration configuration;

        public Create2DBarcodeRequestValidator(IConfiguration configuration)
        {
            this.configuration = configuration;

            CascadeMode = CascadeMode.Stop;     

            RuleFor(x => x.Body)
                .NotEmpty()
                .WithMessage("Request should not be empty.")
                .WithErrorCode(ErrorCode.FHIR_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.RegionSubscriptionNameHeader)
                .NotEmpty()
                .WithMessage(
                    $"Missing header '{DevolvedAdministrationBarcodeGeneratorFunction.RegionSubscriptionNameHeader}'")
                .WithErrorCode(ErrorCode.UNEXPECTED_SYSTEM_ERROR.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.ValidFrom)
               .Must(IsPositiveInteger)
               .WithMessage("The query parameter validFrom did not contain a positive integer value.")
               .WithErrorCode(ErrorCode.VALIDFROM_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.ValidFrom)
                .Must(IsPositiveInteger)
                .WithMessage("The query parameter validTo did not contain a positive integer value.")
                .WithErrorCode(ErrorCode.VALIDTO_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x)
                .Must(x => IsDurationWithinBounds(x))
                .WithMessage(x => GetTimeBoundsErrorMessage(x))
                .WithErrorCode(ErrorCode.VALIDTO_INVALID.ToString(StringUtils.NumberFormattedEnumFormat));
        }

        private static bool IsPositiveInteger(string validFromString)
        {
            if (long.TryParse(validFromString, out long validFrom))
            {
                return validFrom > 0;
            }

            return false;
        }

        private bool IsDurationWithinBounds(Create2DBarcodeRequest request)
        {
            string validFromString = request.ValidFrom;
            string validToString = request.ValidTo;
            if (long.TryParse(validFromString, out long validFrom) && long.TryParse(validToString, out long validTo))
            {
                var validFromDateTime = DateUtils.UnixTimeSecondsToDateTime(validFrom);
                var validToDateTime = DateUtils.UnixTimeSecondsToDateTime(validTo);

                int minimumBarcodeDurationHours = configuration.GetValue<int>(request.GetMinimumValidityDurationConfigurationKey());
                int maximumBarcodeDurationHours = configuration.GetValue<int>(request.GetMaximumValidityDurationConfigurationKey());

                return validFromDateTime.AddHours(minimumBarcodeDurationHours) <= validToDateTime &&
                    validFromDateTime.AddHours(maximumBarcodeDurationHours) >= validToDateTime;
            }

            return false;
        }

        private string GetTimeBoundsErrorMessage(Create2DBarcodeRequest request)
        {
            int minimumDomesticBarcodeDurationHours = configuration.GetValue<int>(request.GetMinimumValidityDurationConfigurationKey());
            int maximumDomesticBarcodeDurationHours = configuration.GetValue<int>(request.GetMaximumValidityDurationConfigurationKey());
            return $"The query parameter validTo is invalid. The difference between validFrom and validTo must be at least {minimumDomesticBarcodeDurationHours / 24} days and no more than {maximumDomesticBarcodeDurationHours / 24} days.";
        }
    }

}
