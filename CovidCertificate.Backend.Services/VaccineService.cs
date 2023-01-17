using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using System;
using System.Collections.Generic;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Settings;
using Hl7.Fhir.Model;
using CovidCertificate.Backend.Models.Exceptions;
using System.Linq;
using CovidCertificate.Backend.NhsApiIntegration.Interfaces;

namespace CovidCertificate.Backend.Services
{
    public class VaccineService : IVaccineService
    {
        private readonly IVaccinationMapper vacMapper;
        private readonly ILogger<VaccineService> logger;
        private readonly IRedisCacheService redisCacheService;
        private readonly INhsdFhirApiService nhsdFhirApiService;
        private readonly IUnattendedSecurityService unattendedSecurityService;
        private readonly IVaccineFilterService vaccineFilterService;

        public VaccineService(IVaccinationMapper mapper,
                         ILogger<VaccineService> _logger,
                         IRedisCacheService _redisCacheService,
                         INhsdFhirApiService nhsdFhirApiService,
                         IUnattendedSecurityService unattendedSecurityService,
                         IVaccineFilterService vaccineFilterService)
        {
            logger = _logger;
            redisCacheService = _redisCacheService;
            vacMapper = mapper;
            this.nhsdFhirApiService = nhsdFhirApiService;
            this.unattendedSecurityService = unattendedSecurityService;
            this.vaccineFilterService = vaccineFilterService;
        }

        public async Task<List<Vaccine>> GetAttendedVaccinesAsync(string idToken, CovidPassportUser covidUser, string apiKey, bool shouldFilterFirstAndLast = false)
        {
            logger.LogTraceAndDebug($"{nameof(GetAttendedVaccinesAsync)} was invoked.");

            var key = $"GetVaccines:{covidUser.ToNhsNumberAndDobHashKey()}";
            var (cachedResponse, isCached) = await ReturnCachedResponseAsync(key);
            if (isCached)
            {
                return cachedResponse;
            }

            var bundle = await nhsdFhirApiService.GetAttendedVaccinesBundleAsync(idToken, apiKey);
            var vaccines = (await vacMapper.MapBundleToVaccinesAsync(bundle)).ToList();

            vaccines = (await vaccineFilterService.FilterVaccinesByFlagsAsync(vaccines, shouldFilterFirstAndLast)).ToList();
            await redisCacheService.AddKeyAsync(key, vaccines, RedisLifeSpanLevel.FiveMinutes);
            return vaccines;
        }

        public async Task<List<Vaccine>> GetUnattendedVaccinesAsync(CovidPassportUser covidUser, string apiKey, bool shouldFilterFirstAndLast = false, bool checkBundleBirthdate = false)
        {
            unattendedSecurityService.Authorize();

            var key = $"GetUnattendedVaccines:{covidUser.ToNhsNumberAndDobHashKey()}";
            var (cachedResponse, isCached) = await ReturnCachedResponseAsync(key);

            if (isCached)
            {
                return cachedResponse;
            }

            var bundle = await nhsdFhirApiService.GetUnattendedVaccinesBundleAsync(covidUser, apiKey);

            if (checkBundleBirthdate)
            {
                ValidateBundleBirthdate(bundle, covidUser);
            }

            var mappedVaccines = (await vacMapper.MapBundleToVaccinesAsync(bundle)).ToList();
            mappedVaccines = (await vaccineFilterService.FilterVaccinesByFlagsAsync(mappedVaccines, shouldFilterFirstAndLast)).ToList();
            await redisCacheService.AddKeyAsync(key, mappedVaccines, RedisLifeSpanLevel.FiveMinutes);

            return mappedVaccines;
        }

        private void ValidateBundleBirthdate(Bundle bundle, CovidPassportUser covidUser)
        {
            if (bundle.Entry.FirstOrDefault(x => x.Resource is Patient)?.Resource is Patient patient)
            {
                if (Convert.ToDateTime(patient.BirthDate) != covidUser.DateOfBirth)
                {
                    throw new BirthdayValidationException("Invalid date of birth");
                }
            }
        }

        private async Task<(List<Vaccine>, bool)> ReturnCachedResponseAsync(string key)
        {
            var (cachedResponse, cacheExists) = await redisCacheService.GetKeyValueAsync<List<Vaccine>>(key);
            if (cacheExists)
            {
                return (cachedResponse, true);
            }

            return (new List<Vaccine>(), false);
        }
    }
}
