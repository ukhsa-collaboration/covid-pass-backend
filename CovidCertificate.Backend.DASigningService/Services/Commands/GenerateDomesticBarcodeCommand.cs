using System;
using CovidCertificate.Backend.DASigningService.Models;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.DASigningService.Services.Commands
{
    public class GenerateDomesticBarcodeCommand
    {
        public Patient Patient { get; set; }
        public string[] Policies { get; set; }
        public int PolicyMask { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public RegionConfig RegionConfig { get; set; }
    }
}
