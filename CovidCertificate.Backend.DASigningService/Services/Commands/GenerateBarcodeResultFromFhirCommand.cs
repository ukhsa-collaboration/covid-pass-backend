using System;
using CovidCertificate.Backend.Models.DataModels;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.DASigningService.Services.Commands
{
    public class GenerateBarcodeResultFromFhirCommand
    {
        public Resource Resource { get; }
        public DAUser User { get; }
        public string IssuingInstituion { get; }
        public string IssuingCountry { get; }
        public string UVCICountryCode { get; set; }
        public string Uvci { get; }
        public string SigningCertificateIdentifier { get; set; }

        public DateTime ValidityStartDate { get; }
        public DateTime ValidityEndDate { get; }

        public GenerateBarcodeResultFromFhirCommand(
            Resource resource,
            DAUser user,
            string issuingInstituion,
            string uvciCountryCode,
            string issuingCountry,
            string signingCertificateIdentifier,
            string uvci,
            DateTime validityStartDate,
            DateTime validityEndDate)
        {
            this.Resource = resource;
            this.User = user;
            this.IssuingInstituion = issuingInstituion;
            this.UVCICountryCode = uvciCountryCode;
            this.IssuingCountry = issuingCountry;
            this.SigningCertificateIdentifier = signingCertificateIdentifier;
            this.Uvci = uvci;
            this.ValidityStartDate = validityStartDate;
            this.ValidityEndDate = validityEndDate;
        }
    }
}
