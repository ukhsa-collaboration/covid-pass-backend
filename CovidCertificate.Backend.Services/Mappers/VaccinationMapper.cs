using System;
using System.Globalization;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using CovidCertificate.Backend.Models.Exceptions;
using System.Collections.Generic;
using Hl7.Fhir.Serialization;
using CovidCertificate.Backend.Utils.Extensions;
using System.Text;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Services.Mappers
{
    public class VaccinationMapper : IVaccinationMapper
    {
        public const string BlobContainerNameConfigKey = "BlobContainerNameVaccineMappings";
        public const string BlobFileNameConfigKey = "BlobFileNameVaccineMappings";

        private readonly IBlobFilesInMemoryCache<VaccineMappings> _mappings;
        private readonly IConfiguration configuration;
        private readonly ILogger<VaccinationMapper> logger;

        private string BlobContainerName => this.configuration.GetValue<string>(BlobContainerNameConfigKey);
        private string BlobFileName => this.configuration.GetValue<string>(BlobFileNameConfigKey);

        public VaccinationMapper(IBlobFilesInMemoryCache<VaccineMappings> mappings, IConfiguration configuration, ILogger<VaccinationMapper> logger)
        {
            _mappings = mappings;
            this.logger = logger;
            this.configuration = configuration;
        }

        public async Task<IEnumerable<Vaccine>> MapBundleToVaccinesAsync(Bundle bundle)
        {
            var vaccines = new List<Vaccine>();
            foreach (Immunization immunization in bundle.Entry.Where(x => x.Resource is Immunization immunization).Select(y => (Immunization)y.Resource))
            {
                if (immunization.Status != Immunization.ImmunizationStatusCodes.Completed)
                {
                    continue;
                }

                logger.LogTraceAndDebug(
                    $"Immunization being mapped. Date: {immunization.Occurrence}." +
                    $" Vaccine: {immunization.VaccineCode}. Manufacturer: {immunization.Manufacturer}");

                try
                {
                    var vaccine = await MapFhirToVaccineAsync(immunization);
                    logger.LogTraceAndDebug($"Vaccine mapped. Date: {vaccine.VaccinationDate}");
                    vaccines.Add(vaccine);
                }
                catch (VaccineMappingException e)
                {
                    logger.LogError(e, e.Message);
                }
            }
            return vaccines;
        }

        /// <summary>
        /// Takes a vaccine in the Immunization format (from the Hl7.Fhir.R4 NuGet package)
        /// and maps it to the internal data model
        /// </summary>
        public async Task<Vaccine> MapFhirToVaccineAsync(Immunization immunization, string issuer = null, string countryOverride = null)
        {
            var snomedCode = immunization.VaccineCode?.Coding?.FirstOrDefault()?.Code;
            var vaccineMap = await MapRawSnomedcodeValueAsync(snomedCode);
            var occurrenceString = immunization.Occurrence?.ToString();

            if (string.IsNullOrEmpty(occurrenceString))
            {
                throw new VaccineMappingException("Occurrence of the immunization is null.");
            }

            logger.LogInformation("Starting parsing occurance date of immunization, value: " + occurrenceString);

            if (!DateTime.TryParse(occurrenceString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dateTime))
            {
                logger.LogWarning("VaccinationMapper: Could not parse datetime " + occurrenceString);
            }

            var countryCode = immunization.Location?.Identifier?.Value;
            var siteName = GetSiteName(immunization, countryCode);
            var procedureCodeableConcept = (CodeableConcept)immunization.Extension.FirstOrDefault(x => string.Equals(x.Url, "https://fhir.hl7.org.uk/StructureDefinition/Extension-UKCore-VaccinationProcedure", StringComparison.OrdinalIgnoreCase))?.Value;
            var procedureCode = procedureCodeableConcept?.Coding?.FirstOrDefault()?.Code ?? string.Empty;
           
            if (string.IsNullOrEmpty(procedureCode))
            {
                logger.LogWarning("VaccinationMapper: procedureCode is empty.");
            }

            var isBooster = (await _mappings.GetFileAsync(
                BlobContainerName, BlobFileName)).BoosterProcedureCodes.Contains(procedureCode);

            var stringDoseNumber = immunization.ProtocolApplied?.FirstOrDefault()?.DoseNumber?.ToString();
            if (!int.TryParse(stringDoseNumber, out var doseNumber))
            {
                logger.LogError($"Failed to parse {nameof(stringDoseNumber)}. Raw value '{stringDoseNumber}'. IsBooster: {isBooster}");
                if (isBooster)
                    doseNumber = 1;
                else
                    throw new VaccineMappingException($"Parsing doseNumber '{stringDoseNumber}' failed in VaccinationMapper");
            }

            var manufacturer = Tuple.Create(vaccineMap.Manufacturer[0],
                vaccineMap.Manufacturer[1]);
            var validityType = vaccineMap.EligibilityRuleName;
            var country = !string.IsNullOrEmpty(countryOverride) ? countryOverride : countryCode;
            var disease = Tuple.Create(vaccineMap.Disease[0], vaccineMap.Disease[1]);
            var vaccine = Tuple.Create(vaccineMap.Vaccine[0], vaccineMap.Vaccine[1]);
            var product = Tuple.Create(vaccineMap.Product[0], vaccineMap.Product[1]);
            var displayName = vaccineMap.Product.ElementAtOrDefault(2);
            var batchNumber = GetBatchNumber(immunization.LotNumber);
            var authority = issuer ?? configuration["VaccinationAuthority"];
            var totalSeriesOfDoses = vaccineMap.TotalSeriesOfDoses;

            var dateEnteredString = immunization.Recorded;

            var dateEntered = DateTime.TryParse(dateEnteredString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var res) 
                ? res : DateTime.MinValue;

            if (dateEntered == DateTime.MinValue)
            {
                logger.LogWarning("DateEntered failed to be parsed or was null, default min date value was used. Value: " + dateEnteredString);
            }            

            return new Vaccine(
                doseNumber,
                dateTime,
                manufacturer,
                disease,
                vaccine,
                product,
                batchNumber,
                country,
                authority,
                totalSeriesOfDoses,
                siteName,
                displayName,
                snomedCode,
                dateEntered,
                procedureCode,
                isBooster,
                validityType);
        }

        public async Task<Vaccine> MapFhirToVaccineAndAllowOverwriteOfSeriesDosesFromMappingFileAsync(Immunization immunization, string issuer = null, string countryOverride = null)
        {
            Vaccine vaccine = await MapFhirToVaccineAsync(immunization, issuer, countryOverride);
            vaccine.TotalSeriesOfDoses =
                CalculateTotalSeriesOfDoses(immunization, vaccine.DoseNumber, vaccine.TotalSeriesOfDoses);
            return vaccine;
        }

        private int CalculateTotalSeriesOfDoses(Immunization immunization, int doseNumber, int seriesOfDosesFromMapper)
        {
            var stringSeriesDosesFromPayload = immunization.ProtocolApplied?.FirstOrDefault()?.SeriesDoses?.ToString();
            if (SeriesOfDosesSpecifiedInPayload(stringSeriesDosesFromPayload))
            {
                var seriesDosesFromPayload = int.TryParse(stringSeriesDosesFromPayload, out var sD)
                    ? sD : throw new VaccineMappingException($"Parsing seriesDoses '{stringSeriesDosesFromPayload}' failed in VaccinationMapper");

                return seriesDosesFromPayload;
            }

            return Math.Max(doseNumber, seriesOfDosesFromMapper);
        }

        private bool SeriesOfDosesSpecifiedInPayload(string stringSeriesDosesFromPayload)
        {
            return !string.IsNullOrEmpty(stringSeriesDosesFromPayload);
        }

        /// <summary>
        /// Fetches the mappings for a vaccine code
        /// </summary>
        /// <param name="rawSnomedcodeValue"></param>
        /// <returns></returns>
        public async Task<VaccineMap> MapRawSnomedcodeValueAsync(string rawSnomedcodeValue)
        {
            if (string.IsNullOrEmpty(rawSnomedcodeValue))
            {
                throw new VaccineMappingException("No SNOMED code for vaccine record");
            }
            var mappingFound = (await _mappings.GetFileAsync(
                BlobContainerName, BlobFileName)).VaccineMaps.TryGetValue(rawSnomedcodeValue, out var vaccineMap);
            //if this is an unknown code, return the raw value
            if (!mappingFound)
            {
                logger.LogError("Failed to map vaccine - unmapped SNOMED code: " + rawSnomedcodeValue);

                throw new VaccineMappingException("Vaccine SNOMED code is not mapped");
            }
            logger.LogInformation("MapRawSnomedcodeValue has finished");

            return vaccineMap;
        }

        /// <summary>
        /// Finds the ODS code for the administering centre.
        /// Tries to find the performer labeled as administering provider ("AP").
        /// If not present takes the first performer
        /// </summary>
        /// <param name="performers"></param>
        /// <returns></returns>
        public string GetOdsCode(List<Immunization.PerformerComponent> performers)
        {
            var administeringPerformer =
                performers.FirstOrDefault(perf => perf.Function?.Coding?.Any(y => y.Code.Equals("AP")) ?? false);
            if (administeringPerformer == default)
            {
                administeringPerformer = performers.FirstOrDefault();
            }
            if (String.IsNullOrEmpty(administeringPerformer?.Actor?.Identifier?.Value))
                logger.LogWarning("No valid ODS codes available in FHIR Immunization.");
            return administeringPerformer?.Actor?.Identifier?.Value;
        }

        //TODO move this somewhere more appropriate
        public DAUser UserFromRawFhir(string rawFhir)
        {
            var fjp = new FhirJsonParser();
            Bundle bundle = fjp.Parse<Bundle>(rawFhir);
            var patientEntry = bundle.Entry.Find(x => x.Resource is Patient);
            if (patientEntry == default)
                throw new ArgumentNullException("No user given");

            var patient = (Patient)patientEntry.Resource;
            var familyName = patient.Name[0].Family;

            var givenSB = new StringBuilder();
            foreach (var name in patient.Name[0].Given)
            {
                givenSB.Append(name);
                givenSB.Append(" ");
            }
            var givenName = givenSB.ToString();

            var nameSB = new StringBuilder();
            nameSB.Append(givenSB);
            nameSB.Append(familyName);

            var dob = DateTime.Parse(patient.BirthDate);

            return new DAUser(nameSB.ToString(), familyName, givenName.Trim(), dob);
        }

        public DAUser DAUserFromPatient(Patient patient)
        {
            var familyName = patient.Name[0].Family;

            var givenSB = new StringBuilder();
            foreach (var name in patient.Name[0].Given)
            {
                givenSB.Append(name);
                givenSB.Append(" ");
            }
            var givenName = givenSB.ToString();

            var nameSB = new StringBuilder();
            nameSB.Append(givenSB);
            nameSB.Append(familyName);

            var dob = DateTime.Parse(patient.BirthDate);

            return new DAUser(nameSB.ToString(), familyName, givenName.Trim(), dob);
        }

        private string GetSiteName(Immunization immunization, string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode))
            {
                // Just for verification in case country code is empty on FE for some reason
                logger.LogWarning($"'CountryCode' empty or null, 'immunization.Location' field value: '{JsonConvert.SerializeObject(immunization.Location)}'.");
            }


            if (countryCode == "GB")
            {
                var name = GetNameFromImmunization(immunization);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
                
                logger.LogWarning($"Null site name for GB country, using default \"-\" to replace site name.");
            }
            
            return "-";
        }

        private string GetNameFromImmunization(Immunization immunization)
        {
            var performer = immunization.Performer?.FirstOrDefault();

            var displayName = performer?.Actor?.Display;

            return displayName;
        }

        private string GetBatchNumber(string batchNumber)
        {
            if (string.IsNullOrEmpty(batchNumber) || batchNumber.ToLower() == "unknown")
            {
                logger.LogWarning($"Empty or unknown batch number, using \"-\" to replace batch number.");
                return "-";
            }

            return batchNumber;
        }
    }
}
