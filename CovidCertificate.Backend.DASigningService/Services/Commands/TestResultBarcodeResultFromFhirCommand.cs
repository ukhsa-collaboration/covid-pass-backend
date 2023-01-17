using System;
using CovidCertificate.Backend.DASigningService.Models;
using CovidCertificate.Backend.Models.DataModels;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.DASigningService.Services.Commands
{
    public class TestResultBarcodeResultFromFhirCommand : GenerateBarcodeResultFromFhirCommand
    {
        public Device Device { get; }
        public Organization Organization { get; }

        public TestResultBarcodeResultFromFhirCommand(
            Observation observation,
            Device device,
            Organization organization,
            DAUser user,
            RegionConfig regionConfig,
            string uvci,
            DateTime validityStartDate,
            DateTime validityEndDate) : base(observation, user, regionConfig.IssuingInstituion, regionConfig.UVCICountryCode, regionConfig.IssuingCountry, regionConfig.SigningCertificateIdentifier, uvci, validityStartDate, validityEndDate)
        {
            this.Device = device;
            this.Organization = organization;
        }
    }
}
