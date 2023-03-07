using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.DASigningService.Interfaces;
using CovidCertificate.Backend.DASigningService.Responses;
using CovidCertificate.Backend.DASigningService.Services.Commands;
using CovidCertificate.Backend.DASigningService.Services.Model;
using CovidCertificate.Backend.DASigningService.Validators;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Utils.Extensions;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.DASigningService.Services
{
    public class DomesticBarcodeGenerator : IDomesticBarcodeGenerator
    {
        private static readonly SingleCharCertificateType CertificateType = SingleCharCertificateType.Domestic;
        private static readonly FhirPatientValidator fhirPatientValidator = new FhirPatientValidator();

        private readonly ILogger logger;
        private readonly IUVCIGeneratorService uvciGeneratorService;
        private readonly IVaccinationMapper vaccinationMapper;
        private readonly IQRCodeGenerator qrCodeGenerator;

        public DomesticBarcodeGenerator(
            ILogger<DomesticBarcodeGenerator> logger,
            IUVCIGeneratorService uvciGeneratorService,
            IVaccinationMapper vaccinationMapper,
            IQRCodeGenerator qrCodeGenerator)
        {
            this.logger = logger;
            this.uvciGeneratorService = uvciGeneratorService;
            this.vaccinationMapper = vaccinationMapper;
            this.qrCodeGenerator = qrCodeGenerator;
        }

        public async Task<BarcodeResults> GenerateDomesticBarcodeAsync(GenerateDomesticBarcodeCommand command)
        {
            var errorResult = ValidatePatient(command.Patient);

            if (errorResult != null)
            {
                return errorResult;
            }

            DAUser daUser = vaccinationMapper.DAUserFromPatient(command.Patient);

            logger.LogDebug("Generation of UVCI for domestic DA certificate.");
            
            var uvci = await uvciGeneratorService.GenerateAndInsertUvciAsync(new GenerateAndInsertUvciCommand(
                command.RegionConfig.IssuingInstituion,
                command.RegionConfig.UVCICountryCode,
                StringUtils.GetHashValue(daUser.Name, daUser.DateOfBirth),
                Backend.Models.Enums.CertificateType.DomesticMandatory,
                CertificateScenario.Domestic,
                command.ValidTo));
            
            var certificate = CreateCertificate(daUser, uvci, command);

            var qrCode = await qrCodeGenerator.GenerateQRCodesAsync(certificate, DAUserToCovidPassportUser(daUser), command.RegionConfig.IssuingCountry);

            var barcodeResults = new BarcodeResults
            {
                Barcodes = new List<BarcodeResult>
                {
                    new BarcodeResult
                    {
                        CertificateType = CertificateType.SingleCharValue,
                        CanProvide = true,
                        Barcode = qrCode.FirstOrDefault()
                    }
                }
            };

            logger.LogDebug("Generation of UVCI Complete.");

            return barcodeResults;
        }

        private Certificate CreateCertificate(DAUser daUser, string uvci, GenerateDomesticBarcodeCommand command)
        {            
            var certificate = new Certificate(
                daUser.Name,
                daUser.DateOfBirth,
                command.ValidTo,
                command.ValidTo,
                Backend.Models.Enums.CertificateType.DomesticMandatory,
                CertificateScenario.Domestic);

            certificate.UniqueCertificateIdentifier = uvci;
            certificate.Policy = command.Policies;
            certificate.PolicyMask = command.PolicyMask;
            certificate.Issuer = command.RegionConfig.IssuingInstituion;
            certificate.Country = command.RegionConfig.DefaultResultCountry;
            certificate.PKICountry = command.RegionConfig.SigningCertificateIdentifier;
            certificate.ValidityStartDate = command.ValidFrom;

            return certificate;
        }

        private CovidPassportUser DAUserToCovidPassportUser(DAUser daUser)
        {
            return new CovidPassportUser(
                daUser.Name, 
                daUser.DateOfBirth, 
                emailAddress:"", 
                phoneNumber:"", 
                givenName: daUser.GivenName, 
                familyName: daUser.FamilyName);
        }

        private BarcodeResults ValidatePatient(Patient patient)
        {
            var patientValidationResult = fhirPatientValidator.Validate(patient);

            // If patient is not valid, return response with errors
            if (!patientValidationResult.IsValid)
            {
                logger.LogWarning($"Patient is not valid. PatientValidationResult: '{patientValidationResult}'.");

                var validationError = patientValidationResult.Errors.FirstOrDefault();

                return new BarcodeResults
                {
                    Errors = new List<Error>
                    {
                        new Error { Message = validationError?.ErrorMessage, Code = validationError?.ErrorCode }
                    }
                };
            }

            logger.LogDebug("Patient Validated.");

            return null;
        }
    }
}
