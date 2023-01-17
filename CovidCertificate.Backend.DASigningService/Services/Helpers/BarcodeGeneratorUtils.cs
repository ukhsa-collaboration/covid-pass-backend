using System.Collections.Generic;
using System.Linq;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.DASigningService.Responses;
using CovidCertificate.Backend.DASigningService.Services.Model;
using FluentValidation.Results;
using Hl7.Fhir.Model;

namespace CovidCertificate.Backend.DASigningService.Services.Helpers
{
    public class BarcodeGeneratorUtils
    {
        public static BarcodeResults GenerateBarcodeResultsWithErrors(List<Resource> resources, ValidationResult patientValidationResult, SingleCharCertificateType certificateType)
        {
            var validationError = patientValidationResult.Errors.FirstOrDefault();

            var barcodeResults = new List<BarcodeResult>(resources.Count);

            foreach (var resource in resources)
            {
                var barCodeResult = new BarcodeResult
                {
                    Id = resource.Id,
                    CanProvide = false,
                    CertificateType = certificateType.SingleCharValue,
                    Error = new Error
                    {
                        Message = validationError?.ErrorMessage,
                        Code = validationError?.ErrorCode
                    }
                };

                barcodeResults.Add(barCodeResult);
            }

            var result = new BarcodeResults
            {
                Barcodes = barcodeResults
            };

            return result;
        }

        public static BarcodeResults GenerateBarcodeResultsForNullImmunizationOrObservation(ValidationResult validationResult, string certificateType)
        {
            var barcodeResults = new List<BarcodeResult>(1);

            var validationError = validationResult.Errors.FirstOrDefault();

            var barCodeResult = new BarcodeResult
            {
                Id = null,
                CanProvide = false,
                CertificateType = certificateType,
                Error = new Error
                {
                    Message = validationError?.ErrorMessage,
                    Code = validationError?.ErrorCode
                }
            };

            barcodeResults.Add(barCodeResult);

            var result = new BarcodeResults
            {
                Barcodes = barcodeResults
            };

            return result;
        }

        public static Patient GetPatient(Bundle bundle)
        {
            foreach (var entry in bundle.Entry)
            {
                if (entry?.Resource is Patient patient)
                {
                    return patient;
                }
            }

            return null;
        }
    }
}
