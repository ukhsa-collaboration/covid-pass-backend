using System;
using CovidCertificate.Backend.DASigningService.Requests.Interfaces;
using CovidCertificate.Backend.DASigningService.Validators;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;

namespace CovidCertificate.Backend.DASigningService.Requests
{
    public class Create2DDomesticBarcodeRequest : ICreate2DBarcodeRequest
    {
        private IConfiguration configuration;
        private Create2DDomesticBarcodeRequestValidator validator;

        public string RegionSubscriptionNameHeader { get; set; }
        public string Policy { get; set; }
        public string PolicyMask { get; set; }
        public string ValidFrom { get; set; }
        public string ValidTo { get; set; }
        public string Body { get; set; }

        public Create2DDomesticBarcodeRequest(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.validator = new Create2DDomesticBarcodeRequestValidator(configuration);
        }

        public void setDefaults()
        {
            if (String.IsNullOrEmpty(ValidFrom))
            {
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                int secondsSinceEpoch = (int)t.TotalSeconds;
                ValidFrom = secondsSinceEpoch.ToString();
            }

            if(String.IsNullOrEmpty(ValidTo))
            {
                int defaultBarcodeValidityHours = configuration.GetValue<int>("DefaultDomesticBarcodeValidityHours");
                int validFromSecondsSinceEpoch = Int32.Parse(ValidFrom);
                ValidTo = (validFromSecondsSinceEpoch + defaultBarcodeValidityHours * 60 * 60).ToString();
            }
        }

        public ValidationResult Validate()
        {
            setDefaults();
            return validator.Validate(this);
        }

        public string GetRegionSubscriptionNameHeader()
        {
            return RegionSubscriptionNameHeader;
        }
    }
}
