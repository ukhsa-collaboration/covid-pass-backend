using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using CovidCertificate.Backend.DASigningService.Models;
using CovidCertificate.Backend.DASigningService.Responses;
using CovidCertificate.Backend.DASigningService.Services.Commands;
using CovidCertificate.Backend.DASigningService.Services.Model;
using CovidCertificate.Backend.DASigningService.Validators;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Interfaces.International;
using CovidCertificate.Backend.Models.Commands.UvciGeneratorCommands;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Utils.Extensions;
using FluentValidation;
using FluentValidation.Results;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.DASigningService.Services.Helpers
{
    public abstract class GenericBarcodeGenerator<T, U, V> where T : Resource, new() where V : Resource
    {
        private readonly ILogger logger;
        private readonly SingleCharCertificateType certificateType;
        private readonly IEncoderService encoder;
        private readonly IVaccinationMapper vaccinationMapper;
        private readonly IUVCIGeneratorService uvciGeneratorService;

        private static readonly FhirPatientValidator fhirPatientValidator = new FhirPatientValidator();
        private IValidator<T> validator;
        private IValidator<V> locationValidator;

        public GenericBarcodeGenerator(
            IUVCIGeneratorService uvciGeneratorService,
            IVaccinationMapper vaccinationMapper,
            IEncoderService encoder, 
            ILogger logger,
            SingleCharCertificateType certificateType,
            IValidator<T> validator,
            IValidator<V> locationValidator)
        {
            this.uvciGeneratorService = uvciGeneratorService;
            this.vaccinationMapper = vaccinationMapper;
            this.encoder = encoder;
            this.logger = logger;
            this.certificateType = certificateType;
            this.validator = validator;
            this.locationValidator = locationValidator;
        }

        public async Task<BarcodeResults> BarcodesFromFhirBundleAsync(GenerateInternationalBarcodeCommand command)
        {
            var patient = BarcodeGeneratorUtils.GetPatient(command.Bundle);
            var records = GetRecords(command.Bundle);
            var errorResult = await ValidateAsync(command.Bundle, patient, records);
            if (errorResult != null)
            {
                return errorResult;
            }

            logger.LogDebug("DAUser from Patient.");

            var user = vaccinationMapper.DAUserFromPatient(patient);

            var validityEndDate = CalculateValidityEndDate(command, records);

            logger.LogDebug("Generation of UVCI for Regional.");

            var uvci = await uvciGeneratorService.GenerateAndInsertUvciAsync(new GenerateAndInsertUvciCommand(
                command.RegionConfig.IssuingInstituion,
                command.RegionConfig.UVCICountryCode,
                StringUtils.GetHashValue(user.Name, user.DateOfBirth),
                certificateType.CertificateType,
                CertificateScenario.International,
                validityEndDate));

            var recordLocationPairs = GetRecordLocationPairs(command.Bundle, records);

            var tasks = CreateListOfTasksForBarcodeResultGeneration(command.Bundle, command.RegionConfig, recordLocationPairs, user, uvci, command.ValidFrom, validityEndDate);

            var barcodeResults = (await System.Threading.Tasks.Task.WhenAll(tasks)).ToList();

            var result = new BarcodeResults
            {
                Barcodes = barcodeResults,
                UVCI = uvci
            };

            return result;
        }

        protected async Task<BarcodeResults> ValidateAsync<T>(Bundle bundle, Patient patient, List<T> records)
        {
            logger.LogDebug("Validating patient from FHIR bundle.");

            var patientValidationResult = fhirPatientValidator.Validate(patient);

            if (!records.Any())
            {
                var validationResult = await validator.ValidateAsync(CreateNewRecord());

                logger.LogWarning($"No records of type {typeof(T)} found");

                return BarcodeGeneratorUtils.GenerateBarcodeResultsForNullImmunizationOrObservation(validationResult, certificateType.SingleCharValue);
            }

            // If patient is not valid, return response with errors
            if (!patientValidationResult.IsValid)
            {
                logger.LogWarning($"Patient is not valid. PatientValidationResult: '{patientValidationResult}'.");

                return BarcodeGeneratorUtils.GenerateBarcodeResultsWithErrors(records.ConvertAll(x => x as Resource), patientValidationResult, certificateType);
            }

            logger.LogDebug("Doing custom validation.");

            var customValidationResult = await DoCustomValidationsAsync(bundle);

            if (customValidationResult != null && !customValidationResult.IsValid)
            {
                logger.LogWarning($"Custom validation failed. CustomValidationResult: '{customValidationResult}'.");

                return BarcodeGeneratorUtils.GenerateBarcodeResultsWithErrors(records.ConvertAll(x => x as Resource), customValidationResult, certificateType);
            }

            return null;
        }

        protected async Task<BarcodeResult> GenerateBarcodeResultFromFhirAsync(GenerateBarcodeResultFromFhirCommand command)
        {
            var barcodeResult = new BarcodeResult
            {
                Id = command.Resource.Id,
                CanProvide = false,
                CertificateType = certificateType.SingleCharValue
            };

            var validationResult = await validator.ValidateAsync(command.Resource as T);

            if (!validationResult.IsValid)
            {
                logger.LogWarning($"{typeof(T)} not valid. Validation error: '{validationResult.Errors}'.");

                var validationError = validationResult.Errors.FirstOrDefault();

                barcodeResult.Error = new Error
                {
                    Code = validationError?.ErrorCode,
                    Message = validationError?.ErrorMessage
                };

                return barcodeResult;
            }

            try
            {
                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                TimeSpan diff = command.ValidityStartDate.ToUniversalTime() - origin;

                var results = await GetResultsAsync(command);
                barcodeResult.Barcode = await encoder.EncodeFlowAsync(
                    command.User,
                    Convert.ToInt64(diff.TotalSeconds),
                    results.First(),
                    command.Uvci,
                    command.ValidityEndDate,
                    command.SigningCertificateIdentifier,
                    command.IssuingCountry);
                barcodeResult.CanProvide = true;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Unexpected error. Exception message: '{e.Message}'.");

                barcodeResult.Error = new Error
                {
                    Code = ErrorCode.UNEXPECTED_SYSTEM_ERROR.ToString(StringUtils.NumberFormattedEnumFormat),
                    Message = "Barcode generation failed."
                };
            }

            return barcodeResult;
        }

        protected List<Task<BarcodeResult>> CreateListOfTasksForBarcodeResultGeneration(
            Bundle bundle, 
            RegionConfig regionConfig,
            Dictionary<T, V> pairs, 
            DAUser user, 
            string uvci, 
            DateTime validityStartDate,
            DateTime validityEndDate
            )
        {           

            var tasks = new List<Task<BarcodeResult>>();

            foreach (var pair in pairs)
            {
                var command = CreateCommand(bundle, pair.Key, user, regionConfig, uvci, validityStartDate, validityEndDate);

                if (pair.Value == null)
                {
                    tasks.Add(GenerateBarcodeResultFromFhirAsync(command));

                    continue;
                }

                var locationValidationResult = locationValidator.Validate(pair.Value);

                if (locationValidationResult.IsValid)
                {
                    command.UVCICountryCode = GetCountry(pair.Value);

                    tasks.Add(GenerateBarcodeResultFromFhirAsync(command));

                    continue;
                }

                tasks.Add(System.Threading.Tasks.Task.FromResult(GenerateBarcodeResultForNotValidLocation(pair.Key,
                    locationValidationResult)));
            }

            return tasks;
        }

        protected BarcodeResult GenerateBarcodeResultForNotValidLocation(T record, ValidationResult locationValidationResult)
        {
            var validationError = locationValidationResult.Errors.FirstOrDefault();

            var barCodeResult = new BarcodeResult
            {
                Id = record.Id,
                CanProvide = false,
                CertificateType = certificateType.SingleCharValue,
                Error = new Error
                {
                    Message = validationError?.ErrorMessage,
                    Code = validationError?.ErrorCode
                }
            };

            return barCodeResult;
        }

        protected virtual async Task<ValidationResult> DoCustomValidationsAsync(Bundle bundle)
        {
            return await System.Threading.Tasks.Task.FromResult<ValidationResult>(null);
        }

        protected virtual GenerateBarcodeResultFromFhirCommand CreateCommand(
            Bundle bundle,
            T resource,
            DAUser user,
            RegionConfig regionConfig,
            string uvci,
            DateTime validityStartDate,
            DateTime validityEndDate)
        {
            return new GenerateBarcodeResultFromFhirCommand(
                    resource,
                    user,
                    regionConfig.IssuingInstituion,
                    regionConfig.UVCICountryCode,
                    regionConfig.IssuingCountry,
                    regionConfig.SigningCertificateIdentifier,
                    uvci,
                    validityStartDate,
                    validityEndDate);
        }

        protected List<T> GetRecords(Bundle bundle)
        {            
            return bundle.Entry.Select(x => x.Resource).OfType<T>().ToList();            
        }

        protected abstract Task<IEnumerable<IGenericResult>> GetResultsAsync(GenerateBarcodeResultFromFhirCommand command);

        protected abstract string GetCountry(V resource);

        protected abstract DateTime CalculateValidityEndDate(GenerateInternationalBarcodeCommand command, List<T> records);

        protected abstract Dictionary<T, V> GetRecordLocationPairs(Bundle bundle, List<T> records);

        private T CreateNewRecord()
        {
            return new T();
        }
    }
}
