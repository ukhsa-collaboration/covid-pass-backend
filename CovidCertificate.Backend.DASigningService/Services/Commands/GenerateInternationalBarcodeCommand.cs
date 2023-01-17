using CovidCertificate.Backend.DASigningService.Models;
using CovidCertificate.Backend.Models.Enums;
using Hl7.Fhir.Model;
using System;

namespace CovidCertificate.Backend.DASigningService.Services.Commands
{
    public class GenerateInternationalBarcodeCommand
    {
        public Bundle Bundle { get; set; }
        public RegionConfig RegionConfig { get; set; }
        public CertificateType CertificateType { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
    }
}
