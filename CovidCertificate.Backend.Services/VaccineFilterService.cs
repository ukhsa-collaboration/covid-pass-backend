using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.DataModels.EligibilityConfiguration;
using CovidCertificate.Backend.Models.Helpers;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CovidCertificate.Backend.Services
{
    public class VaccineFilterService : IVaccineFilterService
    {
        private readonly ILogger<VaccineFilterService> logger;
        private readonly IConfiguration configuration;
        private readonly IBlobFilesInMemoryCache<EligibilityConfiguration> eligibilityConfigurationBlobCache;
        private readonly IFeatureManager featureManager;
        private readonly IEligibilityConfigurationService eligibilityConfigurationService;
        private readonly IBlobFilesInMemoryCache<VaccineMappings> vaccineMappingsCache;

        public VaccineFilterService(ILogger<VaccineFilterService> logger,
            IFeatureManager featureManager,
            IBlobFilesInMemoryCache<EligibilityConfiguration> eligibilityConfigurationBlobCache,
            IConfiguration configuration,
            IEligibilityConfigurationService eligibilityConfigurationService,
            IBlobFilesInMemoryCache<VaccineMappings> vaccineMappingsCache)
        {
            this.logger = logger;
            this.featureManager = featureManager;
            this.eligibilityConfigurationBlobCache = eligibilityConfigurationBlobCache;
            this.configuration = configuration;
            this.eligibilityConfigurationService = eligibilityConfigurationService;
            this.vaccineMappingsCache = vaccineMappingsCache;
        }

        public async Task<IEnumerable<Vaccine>> FilterVaccinesByFlagsAsync(List<Vaccine> vaccines, bool shouldFilterFirstAndLast)
        {
            var uniqueVaccines = await GetUniqueVaccinesAsync(vaccines);

            var boosters = uniqueVaccines.Where(x => x.IsBooster).ToList();
            uniqueVaccines = uniqueVaccines.Where(x => !x.IsBooster).ToList();

            uniqueVaccines = await GetFirstAndLastVaccinesAsync(uniqueVaccines, shouldFilterFirstAndLast);
            uniqueVaccines = await FilterBoosterOnlyCodes(uniqueVaccines);
            if (await featureManager.IsEnabledAsync(FeatureFlags.RemoveBoosters))
            {
                return uniqueVaccines;
            }

            var gbCountries = configuration.GetSection("GBCountries").Get<IEnumerable<string>>();
            var acceptedOverseasBoosterManufacturers = configuration.GetSection("AcceptedBoosterManufacturers").Get<IEnumerable<string>>();
            boosters = boosters.Where(FilterBoostersByCountryOrManufacturer(gbCountries, acceptedOverseasBoosterManufacturers)).ToList();
            await UpdateBoostersDoseNumberAndTotalSeriesOfDosesAsync(boosters, uniqueVaccines);

            return uniqueVaccines;
        }

        private async Task<List<Vaccine>> GetUniqueVaccinesAsync(List<Vaccine> vaccines)
        {
            var validVaccines = GetVaccinesFromAllowedCountriesBySnomedCode(vaccines, await GetAllowedCountriesFromEligibilityConfigAsync());
            return validVaccines.DistinctWithPreference(v => new { v.SnomedCode, v.DoseNumber, v.DateTimeOfTest.Date }, "DateEntered").ToList();
        }

        private async Task<Dictionary<string, IEnumerable<string>>> GetAllowedCountriesFromEligibilityConfigAsync()
        {
            (var container, var filename) = await eligibilityConfigurationService.GetEligibilityConfigurationBlobContainerAndFilenameAsync();
            return (await eligibilityConfigurationBlobCache.GetFileAsync(container, filename)).AllowedCountries;
        }

        private IEnumerable<Vaccine> GetVaccinesFromAllowedCountriesBySnomedCode(IEnumerable<Vaccine> allVaccines, Dictionary<string, IEnumerable<string>> allowedCountries)
        {
            var validVaccines = new List<Vaccine>();
            foreach (Vaccine v in allVaccines)
            {
                if (allowedCountries.TryGetValue(v.SnomedCode, out var snomedCountries))
                {
                    if (!snomedCountries.Any() || snomedCountries.Contains(v.CountryCode))
                    {
                        validVaccines.Add(v);
                    }
                }
            }

            return validVaccines;
        }

        private async Task<List<Vaccine>> GetFirstAndLastVaccinesAsync(List<Vaccine> vaccines, bool shouldFilterFirstAndLast)
        {
            var filterFirstAndLastVaccines = shouldFilterFirstAndLast || await featureManager.IsEnabledAsync(FeatureFlags.FilterFirstAndLastVaccines);

            if (filterFirstAndLastVaccines)
            {
                if (vaccines.Count > 2)
                {
                    logger.LogTraceAndDebug("Filtering for first and last vaccines");

                    var sortedList = vaccines.OrderBy(v => v.VaccinationDate).ToList();

                    return new List<Vaccine> { sortedList.First(), sortedList.Last() };
                }
            }

            return vaccines;
        }

        private static Func<Vaccine, bool> FilterBoostersByCountryOrManufacturer(IEnumerable<string> gbCountries, IEnumerable<string> acceptedOverseasBoosterManufacturers)
        {
            return v => acceptedOverseasBoosterManufacturers.Contains(v.VaccineManufacturer.Item1) || gbCountries.Contains(v.CountryOfVaccination?.ToUpper());
        }

        /// <summary>
        /// Update the DoseNumber and TotalSeriesOfDoses values for boosters to align with the EU DCC standards.
        /// </summary>
        /// <returns>
        /// An IEnumerable<Vaccine> of boosters whose DoseNumber and TotalSeriesOfDoses iterate on from the
        /// lastDoseNumber in chronological order.
        /// </returns>
        private async Task UpdateBoostersDoseNumberAndTotalSeriesOfDosesAsync(IEnumerable<Vaccine> boosters, List<Vaccine> validVaccines)
        {
            var lastDoseNumber = validVaccines.OrderByDescending(x => x.VaccinationDate).FirstOrDefault()?.TotalSeriesOfDoses ?? 0;
            var vaccineMappingsContainer = configuration.GetValue<string>("BlobContainerNameVaccineMappings");
            var vaccineMappingsFile = configuration.GetValue<string>("BlobFileNameVaccineMappings");
            var mappings = await GetVaccineMappingsAsync();
            var freezeFollowingBoosterVaccinationSNOMEDs = mappings.FreezeFollowingBoosterVaccinationSNOMEDs;
            var freezeDenominator = validVaccines.Any(v => freezeFollowingBoosterVaccinationSNOMEDs.Contains(v.SnomedCode));

            validVaccines.AddRange(boosters.OrderBy(b => b.VaccinationDate).Select(booster =>
            {
                lastDoseNumber++;
                booster.DoseNumber = lastDoseNumber;
                booster.TotalSeriesOfDoses = freezeDenominator ? 1 : lastDoseNumber;

                return booster;
            }));
        }

        private async Task<List<Vaccine>> FilterBoosterOnlyCodes (List<Vaccine> vaccines)
        {
            var mappings = await GetVaccineMappingsAsync();
            var boosterOnlyVaccineSnomeds = mappings.BoosterOnlyVaccineCodes;

            return vaccines.Where(v => !boosterOnlyVaccineSnomeds.Contains(v.SnomedCode)).ToList();
        }

        private async Task<VaccineMappings> GetVaccineMappingsAsync()
        {
            var vaccineMappingsContainer = configuration.GetValue<string>("BlobContainerNameVaccineMappings");
            var vaccineMappingsFile = configuration.GetValue<string>("BlobFileNameVaccineMappings");
            return await vaccineMappingsCache.GetFileAsync(vaccineMappingsContainer, vaccineMappingsFile);
        }
    }
}
