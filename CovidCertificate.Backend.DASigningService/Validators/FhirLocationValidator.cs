using System.Collections.Generic;
using System.Linq;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.DASigningService.Validators
{
    public class FhirLocationValidator : AbstractValidator<Location>
    {
        private static readonly List<string> RegionCodes = GetCountryCodes();

        public FhirLocationValidator()
        {
            RuleFor(x => x.Address).Cascade(CascadeMode.Stop).NotNull()
                .When(x => x != null)
                .WithMessage("Location.Address missing.")
                .WithErrorCode(ErrorCode.FHIR_LOCATION_ADDRESS_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Address.Country).Cascade(CascadeMode.Stop).NotEmpty()
                .When(x => x?.Address != null)
                .WithMessage("Location.Address.Country missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_LOCATION_ADDRESS_COUNTRY_EMPTY.ToString(StringUtils.NumberFormattedEnumFormat))
                .Must(MustBeOnISORegionCodesList)
                .When(x => x?.Address != null)
                .WithMessage("Location.Address.Country value is not values from ISO3166 country codes list.")
                .WithErrorCode(
                    ErrorCode.FHIR_LOCATION_ADDRESS_COUNTRY_NOTONISOLIST.ToString(StringUtils
                        .NumberFormattedEnumFormat));
        }

        private bool MustBeOnISORegionCodesList(string value)
        {
            return RegionCodes.Contains(value);
        }

        private static List<string> GetCountryCodes()
        {
            var listOfCodes = ISO3166.Country.List.Select(x => x.TwoLetterCode).ToList();
            listOfCodes.AddRange(ISO3166.Country.List.Select(x => x.ThreeLetterCode));

            return listOfCodes;
        }
    }
}
