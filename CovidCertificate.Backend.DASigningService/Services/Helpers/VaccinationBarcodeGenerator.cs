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
using CovidCertificate.Backend.Interfaces.International;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Interfaces;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.DASigningService.Services.Helpers
{
    public class VaccinationBarcodeGenerator : GenericBarcodeGenerator<Immunization, Vaccine, Location>, IVaccinationBarcodeGenerator
    {
        private readonly IVaccinationMapper vaccinationMapper;
        private readonly IConfiguration configuration;


        public VaccinationBarcodeGenerator(
            IUVCIGeneratorService uvciGeneratorService,
            IVaccinationMapper vaccinationMapper,
            IEncoderService encoder,
            ILogger<VaccinationBarcodeGenerator> logger,
            IConfiguration configuration) : base(uvciGeneratorService, vaccinationMapper, encoder, logger, SingleCharCertificateType.Vaccination, new FhirImmunizationValidator(vaccinationMapper), new FhirLocationValidator())
        {
            this.vaccinationMapper = vaccinationMapper;
            this.configuration = configuration;
        }

        protected override DateTime CalculateValidityEndDate(GenerateInternationalBarcodeCommand command, List<Immunization> immunizations)
        {
            return command.ValidTo;
        }
        
        protected override Dictionary<Immunization, Location> GetRecordLocationPairs(Bundle bundle, List<Immunization> immunizations)
        {
            var immunizationLocationPairs = new Dictionary<Immunization, Location>();

            foreach (var immunization in immunizations)
            {
                var potentialLocation =
                    bundle.Entry.FirstOrDefault(x => x.FullUrl == immunization.Location?.Reference)?.Resource;

                immunizationLocationPairs.Add(immunization, potentialLocation as Location);
            }

            return immunizationLocationPairs;
        }

        protected override string GetCountry(Location location)
        {
            return location.Address.Country;
        }

        protected async override Task<IEnumerable<IGenericResult>> GetResultsAsync(GenerateBarcodeResultFromFhirCommand command)
        {
            return new List<Vaccine>
            {
                await vaccinationMapper.MapFhirToVaccineAndAllowOverwriteOfSeriesDosesFromMappingFileAsync(command.Resource as Immunization, command.IssuingInstituion, command.UVCICountryCode)
            };
        }
    }
}
