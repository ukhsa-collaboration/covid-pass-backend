using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.Interfaces;
using CovidCertificate.Backend.DASigningService.Services.Commands;
using CovidCertificate.Backend.DASigningService.Services.Model;
using CovidCertificate.Backend.DASigningService.Validators;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Certificates.UVCI;
using CovidCertificate.Backend.Interfaces.DateTimeProvider;
using CovidCertificate.Backend.Interfaces.International;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Services.Mappers;
using CovidCertificate.Backend.Utils;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.DASigningService.Services.Helpers
{
    public class RecoveryBarcodeGenerator : GenericBarcodeGenerator<Observation, TestResultNhs, Organization>, IRecoveryBarcodeGenerator
    {
        private readonly DiagnosticTestFhirBundleMapper testResultMapper;
        private readonly IConfiguration configuration;


        public RecoveryBarcodeGenerator(
            IUVCIGeneratorService uvciGeneratorService,
            IVaccinationMapper vaccinationMapper,
            DiagnosticTestFhirBundleMapper testResultMapper,
            IEncoderService encoder,
            ILogger<RecoveryBarcodeGenerator> logger,
            IBlobFilesInMemoryCache<TestMappings> mappingCache,
            IConfiguration configuration,
            IDateTimeProviderService dateTimeProviderService) : base(uvciGeneratorService, vaccinationMapper, encoder, logger, SingleCharCertificateType.Recovery, new FhirObservationRecoveryValidator(configuration, mappingCache, dateTimeProviderService), new FhirOrganizationValidator())
        {
            this.testResultMapper = testResultMapper;
            this.configuration = configuration;
        }

        protected override DateTime CalculateValidityEndDate(GenerateInternationalBarcodeCommand command, List<Observation> observations)
        {
            var newestObservation = observations.Where(x => x.Effective != null).OrderByDescending(x => x.Effective).FirstOrDefault();

            DateTime validityEndDate = command.ValidTo;

            if (newestObservation == null)
            {
                return validityEndDate;
            }

            int validityAfterEffective = configuration.GetValue<int>("HoursAfterRecoveryTestBeforeCertificateInvalid");

            validityEndDate = DateUtils.MinimumOfTwoDates(
                validityEndDate,
                ((FhirDateTime)newestObservation.Effective).ToDateTimeOffset(TimeSpan.Zero).DateTime.AddHours(validityAfterEffective)
            );

            return validityEndDate;
        }

        protected override  Dictionary<Observation, Organization> GetRecordLocationPairs(Bundle bundle, List<Observation> observations)
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
            return organization.Address.FirstOrDefault().Country;
        }

        protected async override Task<IEnumerable<IGenericResult>> GetResultsAsync(GenerateBarcodeResultFromFhirCommand command)
        {
            return new List<TestResultNhs>
            {
               await testResultMapper.ConvertObservationAsync(command.Resource as Observation, command.UVCICountryCode, command.IssuingInstituion)
            };
        }
    }
}
