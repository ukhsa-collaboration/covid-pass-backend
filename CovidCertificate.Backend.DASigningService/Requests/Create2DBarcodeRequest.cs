using CovidCertificate.Backend.DASigningService.Requests.Interfaces;
using CovidCertificate.Backend.DASigningService.Validators;
using CovidCertificate.Backend.Models.Enums;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using System;
using CovidCertificate.Backend.Interfaces.DateTimeProvider;

namespace CovidCertificate.Backend.DASigningService.Requests
{
    public class Create2DBarcodeRequest : ICreate2DBarcodeRequest
    {
        private readonly IDateTimeProviderService dateTimeProviderService;

        private IConfiguration configuration;
        private Create2DBarcodeRequestValidator validator;

        public CertificateType Type { get; set; }
        public string RegionSubscriptionNameHeader { get; set; }
        public string Body { get; set; }
        public string ValidFrom { get; set; }
        public string ValidTo { get; set; }

        public Create2DBarcodeRequest(IConfiguration configuration,
            IDateTimeProviderService dateTimeProviderService)
        {
            this.configuration = configuration;
            this.validator = new Create2DBarcodeRequestValidator(configuration);
            this.dateTimeProviderService = dateTimeProviderService;
        }

        public void SetDefaults()
        {            
            if (String.IsNullOrEmpty(ValidFrom))
            {
                TimeSpan t = dateTimeProviderService.UtcNow - new DateTime(1970, 1, 1);
                int secondsSinceEpoch = (int)t.TotalSeconds;
                ValidFrom = secondsSinceEpoch.ToString();
            }


            if (String.IsNullOrEmpty(ValidTo))
            {
                int defaultBarcodeValidityHours = configuration.GetValue<int>(GetValidityConfigurationKey());                
                int validFromSecondsSinceEpoch = Int32.Parse(ValidFrom);
                ValidTo = (validFromSecondsSinceEpoch + defaultBarcodeValidityHours * 60 * 60).ToString();
            }
        }

        public string GetValidityConfigurationKey()
         => Type switch
         {
             CertificateType.Vaccination => "DefaultVaccinationBarcodeValidityHours",
             CertificateType.TestResult => "DefaultTestResultBarcodeValidityHours",
             CertificateType.Recovery => "DefaultRecoveryBarcodeValidityHours",
             _ => string.Empty
         };

        public string GetMinimumValidityDurationConfigurationKey()
        => Type switch
        {
            CertificateType.Vaccination => "MinimumVaccinationBarcodeDurationHours",
            CertificateType.TestResult => "MinimumTestResultBarcodeDurationHours",
            CertificateType.Recovery => "MinimumRecoveryBarcodeDurationHours",
            _ => string.Empty
        };

        public string GetMaximumValidityDurationConfigurationKey()
        => Type switch
        {
            CertificateType.Vaccination => "MaximumVaccinationBarcodeDurationHours",
            CertificateType.TestResult => "MaximumTestResultBarcodeDurationHours",
            CertificateType.Recovery => "MaximumRecoveryBarcodeDurationHours",
            _ => string.Empty
        };

        public ValidationResult Validate()
        {
            SetDefaults();
            return validator.Validate(this);
        }

        public string GetRegionSubscriptionNameHeader()
        {
            return RegionSubscriptionNameHeader;
        }
    }
}
