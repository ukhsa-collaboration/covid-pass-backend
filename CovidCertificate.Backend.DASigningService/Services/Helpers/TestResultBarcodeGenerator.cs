using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.Interfaces;
using CovidCertificate.Backend.DASigningService.Services.Commands;
using CovidCertificate.Backend.DASigningService.Services.Model;
using CovidCertificate.Backend.DASigningService.Validators;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FluentValidation.Results;
using CovidCertificate.Backend.DASigningService.Models;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Interfaces.DateTimeProvider;
using CovidCertificate.Backend.Interfaces.International;
using CovidCertificate.Backend.Interfaces.PKINationaBackend;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Services.Mappers;

namespace CovidCertificate.Backend.DASigningService.Services.Helpers
{
    public class TestResultBarcodeGenerator : GenericBarcodeGenerator<Observation, TestResultNhs, Organization>, ITestResultBarcodeGenerator
    {
        private readonly DiagnosticTestFhirBundleMapper testResultMapper;
        private readonly IConfiguration configuration;
        private readonly FhirDeviceTestResultValidator fhirDeviceTestResultValidator;
        private readonly FhirObservationReferenceValidator fhirBundleReferenceValidator;

        public TestResultBarcodeGenerator(
            IUVCIGeneratorService uvciGenerator,
            IVaccinationMapper vaccinationMapper,
            DiagnosticTestFhirBundleMapper testResultMapper,
            IEncoderService encoder,
            INationalBackendService nationalBackendService,
            ILogger<TestResultBarcodeGenerator> logger,
            IConfiguration configuration,
            IDateTimeProviderService dateTimeProviderService) : base(uvciGenerator, vaccinationMapper, encoder, logger, SingleCharCertificateType.TestResult, new FhirObservationTestResultValidator(configuration, dateTimeProviderService), new FhirOrganizationValidator(true))
        {
            this.testResultMapper = testResultMapper;
            this.configuration = configuration;
            this.fhirDeviceTestResultValidator = new FhirDeviceTestResultValidator(nationalBackendService);
            this.fhirBundleReferenceValidator = new FhirObservationReferenceValidator();
        }

        protected override DateTime CalculateValidityEndDate(GenerateInternationalBarcodeCommand command, List<Observation> observations)
        {
            var newestObservation = observations.Where(x => x.Effective != null).OrderByDescending(x => x.Effective).FirstOrDefault();

            var validityEndDate = command.ValidTo;

            if (newestObservation == null)
            {
                return validityEndDate;
            }

            var validityAfterEffective = configuration.GetValue<int>("HoursAfterTestResultBeforeCertificateInvalid");
                
            validityEndDate = DateUtils.MinimumOfTwoDates(
                validityEndDate,
                ((FhirDateTime)newestObservation.Effective).ToDateTimeOffset(TimeSpan.Zero).DateTime.AddHours(validityAfterEffective)
            );
            return validityEndDate;
        }


        protected override Dictionary<Observation, Organization> GetRecordLocationPairs(Bundle bundle, List<Observation> observations)
        {
            var observationLocationPairs = new Dictionary<Observation, Organization>();

            foreach (var observation in observations)
            {
                var potentialLocation = bundle.Entry.FirstOrDefault(x => x.FullUrl == observation.Performer?.FirstOrDefault()?.Reference)?.Resource;

                observationLocationPairs.Add(observation, potentialLocation as Organization);
            }

            return observationLocationPairs;
        }


        protected override string GetCountry(Organization organization)
        {
            return organization.Address.FirstOrDefault()?.Country;
        }

        protected async override Task<IEnumerable<IGenericResult>> GetResultsAsync(GenerateBarcodeResultFromFhirCommand command)
        {
            var testCommand = command as TestResultBarcodeResultFromFhirCommand;
            return new List<TestResultNhs>
            {
                await testResultMapper.ConvertObservationWithDeviceAsync(testCommand.Resource as Observation, testCommand.Device, testCommand.Organization, testCommand.UVCICountryCode, testCommand.IssuingInstituion)
            };
        }

        protected override async Task<ValidationResult> DoCustomValidationsAsync(Bundle bundle)
        {
            var devices = GetDevices(bundle);
            if(devices.Count == 0)
            {
                return await fhirDeviceTestResultValidator.ValidateAsync(new Device());
            }

            foreach (var device in devices)
            {
                var result = await fhirDeviceTestResultValidator.ValidateAsync(device);
                if(!result.IsValid)
                {
                    return result;
                }
            }

            return fhirBundleReferenceValidator.Validate(bundle);            
        }

        protected override GenerateBarcodeResultFromFhirCommand CreateCommand(Bundle bundle, Observation resource, DAUser user, RegionConfig regionConfig, string uvci, DateTime validityStartDate, DateTime validityEndDate)
        {
            var device = GetDeviceFromObservationDeviceReference(bundle, resource);
            var organization = bundle.Entry.FirstOrDefault(x => x.FullUrl == resource.Performer?.FirstOrDefault()?.Reference)?.Resource;

            return new TestResultBarcodeResultFromFhirCommand(
                    resource,
                    device,
                    organization as Organization,
                    user,
                    regionConfig,
                    uvci,
                    validityStartDate,
                    validityEndDate);
        }

        private List<Device> GetDevices(Bundle bundle)
        {
            var devices = new List<Device>();
            foreach (var entry in bundle.Entry)
            {
                if (entry?.Resource is Device d)
                {
                    devices.Add(d);
                }
            }

            return devices;
        }

        private static Device GetDeviceFromObservationDeviceReference(Bundle bundle, Observation observation)
        {
            foreach (var entry in bundle.Entry)
            {
                if (entry?.Resource is Device d)
                {
                    if (observation.Performer != null && 
                        observation.Device != null && 
                        observation.Device.Reference != null && 
                        observation.Device.Reference.Equals(entry.FullUrl))
                    {
                        return d;
                    }
                }
            }

            return null;
        }
     }
 }
