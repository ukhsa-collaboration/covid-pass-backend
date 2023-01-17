using System.Collections.Generic;
using System.Linq;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.DASigningService.Validators
{
    public class FhirOrganizationValidator : AbstractValidator<Organization>
    {
        private static readonly List<string> RegionCodes = GetCountryCodes();

        public FhirOrganizationValidator(bool requireName = false)
        {
            RuleFor(x => x.Address).Cascade(CascadeMode.Stop).NotNull()
                .When(x => x != null)
                .WithMessage("Performer.Address missing.")
                .WithErrorCode(ErrorCode.FHIR_PERFORMER_ADDRESS_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Address.FirstOrDefault()).Cascade(CascadeMode.Stop).NotNull()
                .When(x => x != null && x.Address != null)
                .WithMessage("Performer.Address missing.")
                .WithErrorCode(ErrorCode.FHIR_PERFORMER_ADDRESS_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Address.FirstOrDefault().Country).Cascade(CascadeMode.Stop).NotEmpty()
                .When(x => x?.Address != null && x.Address.FirstOrDefault() != null)
                .WithMessage("Performer.Address.Country missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_PERFORMER_ADDRESS_COUNTRY_EMPTY.ToString(StringUtils.NumberFormattedEnumFormat));

            RuleFor(x => x.Address.FirstOrDefault().Country).Cascade(CascadeMode.Stop).Must(MustBeOnISORegionCodesList)
                .When(x => x?.Address != null && x.Address.FirstOrDefault() != null && x?.Address.FirstOrDefault().Country != null)                
                .WithMessage("Performer.Address.Country value is not value from ISO3166 country codes list.")
                .WithErrorCode(
                    ErrorCode.FHIR_PERFORMER_ADDRESS_COUNTRY_NOTONISOLIST.ToString(StringUtils
                        .NumberFormattedEnumFormat));

            RuleFor(x => x.Name).Cascade(CascadeMode.Stop).NotEmpty()
                .When(x => requireName)
                .WithMessage("Performer.Name missing.")
                .WithErrorCode(
                    ErrorCode.FHIR_PERFORMER_NAME_MISSING.ToString(StringUtils.NumberFormattedEnumFormat));
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
