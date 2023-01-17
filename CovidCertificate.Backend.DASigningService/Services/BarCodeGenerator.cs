using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.Interfaces;
using CovidCertificate.Backend.DASigningService.Responses;
using CovidCertificate.Backend.DASigningService.Services.Commands;
using CovidCertificate.Backend.Models.Enums;

namespace CovidCertificate.Backend.DASigningService.Services
{
    public class BarcodeGenerator : IBarcodeGenerator
    {

        private readonly IRecoveryBarcodeGenerator recoveryBarcodeGenerator;
        private readonly IVaccinationBarcodeGenerator vaccinationBarcodeGenerator;
        private readonly IDomesticBarcodeGenerator domesticBarcodeGenerator;
        private readonly ITestResultBarcodeGenerator testResultBarcodeGenerator;

        public BarcodeGenerator(
            IRecoveryBarcodeGenerator recoveryBarcodeGenerator,
            IVaccinationBarcodeGenerator vaccinationBarcodeGenerator,
            IDomesticBarcodeGenerator domesticBarcodeGenerator,
            ITestResultBarcodeGenerator testResultBarcodeGenerator)
        {
            this.recoveryBarcodeGenerator = recoveryBarcodeGenerator;
            this.vaccinationBarcodeGenerator = vaccinationBarcodeGenerator;
            this.domesticBarcodeGenerator = domesticBarcodeGenerator;
            this.testResultBarcodeGenerator = testResultBarcodeGenerator;
        }

        public async Task<BarcodeResults> GenerateInternationalBarcodesAsync(GenerateInternationalBarcodeCommand command)
        => command.CertificateType switch
        {
            CertificateType.Vaccination => await vaccinationBarcodeGenerator.BarcodesFromFhirBundleAsync(command),
            CertificateType.Recovery => await recoveryBarcodeGenerator.BarcodesFromFhirBundleAsync(command),
            CertificateType.TestResult => await testResultBarcodeGenerator.BarcodesFromFhirBundleAsync(command),
            _ => throw new ArgumentException("Provided certificate type was not supported")
        };

        public async Task<BarcodeResults> GenerateDomesticBarcodeAsync(GenerateDomesticBarcodeCommand command)
        {
            return await domesticBarcodeGenerator.GenerateDomesticBarcodeAsync(command);
        }
    }
}
