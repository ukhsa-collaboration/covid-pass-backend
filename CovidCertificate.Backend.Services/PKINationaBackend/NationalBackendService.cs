using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.PKINationaBackend;
using CovidCertificate.Backend.Models.PKINationalBackend;
using CovidCertificate.Backend.Models.PKINationalBackend.DomesticPolicy;
using CovidCertificate.Backend.PKINationalBackend.Models;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.PKINationaBackend
{
    public class NationalBackendService : INationalBackendService
    {
        private const string cacheKeyTrustList = "EUGreenCard:TrustList";
        private const string cacheKeyValueSet = "EUGreenCard:ValueSet";
        private const string cacheKeyPolicy = "EUGreenCard:DomesticPolicyInformation";

        private readonly IMemoryCacheService memoryCache;
        private readonly ILogger<NationalBackendService> logger;
        private readonly ITrustListService trustListService;
        private readonly IValueSetService valueSetService;
        private readonly IDomesticPolicyInformationService domesticPolicyInformationService;
        private readonly DGCGSettings settings;

        public NationalBackendService(IMemoryCacheService memoryCache,
                                      ILogger<NationalBackendService> logger,
                                      ITrustListService trustListService,
                                      DGCGSettings settings,
                                      IValueSetService valueSetService,
                                      IDomesticPolicyInformationService domesticPolicyInformationService)
        {
            this.memoryCache = memoryCache;
            this.logger = logger;
            this.trustListService = trustListService;
            this.settings = settings;
            this.valueSetService = valueSetService;
            this.domesticPolicyInformationService = domesticPolicyInformationService;
        }

        public async Task<DGCGTrustList> GetTrustListCertificatesAsync(string kid = null, string country = null)
        {
            logger.LogTraceAndDebug($"{nameof(NationalBackendService)}: {nameof(GetTrustListCertificatesAsync)} was invoked.");

            var cachedTrustList = await memoryCache.GetOrCreateCacheAsync(cacheKeyTrustList,
                async () => await trustListService.GetDGCGTrustListAsync(),
                DateTimeOffset.UtcNow.AddSeconds(settings.TrustListCacheTimeSeconds));
            var trustList = new DGCGTrustList(cachedTrustList.Certificates);

            if(!kid.NullOrEmpty())
            {
                logger.LogTraceAndDebug($"{nameof(NationalBackendService)}: Filtering by KID '{kid}'.");
                trustList.Certificates = trustList.Certificates.Where(x => x.Kid.Equals(kid, StringComparison.OrdinalIgnoreCase));
            }

            if(!country.NullOrEmpty())
            {
                logger.LogTraceAndDebug($"{nameof(NationalBackendService)}: Filtering by Country '{country}'.");
                trustList.Certificates = trustList.Certificates.Where(x => x.Country.Equals(country, StringComparison.OrdinalIgnoreCase));
            }

            logger.LogTraceAndDebug($"{nameof(NationalBackendService)}: {nameof(GetTrustListCertificatesAsync)} has finished.");

            return trustList;
        }
        
        public async Task<IEnumerable<TrustListSubjectPublicKeyInfoDto>> GetSubjectPublicKeyInfoDtosAsync()
        {
            logger.LogTraceAndDebug($"{nameof(NationalBackendService)}: {nameof(GetSubjectPublicKeyInfoDtosAsync)} was invoked.");
            var trustList = await GetTrustListCertificatesAsync();
            var subjectPublicKeyInfosDto = trustList.Certificates.Select(x => DSCToSubjectPublicKeyInfoDto(x))
                                                                 .Where(x => x != null);
            logger.LogTraceAndDebug($"{nameof(NationalBackendService)}: {nameof(GetSubjectPublicKeyInfoDtosAsync)} has finished.");

            return subjectPublicKeyInfosDto;
        }
        public async Task<EUValueSetResponse> GetEUValueSetResponseAsync(bool includeNonEUValues)
        {
            var lastTimeChecked = new DateTime(1970, 01, 01);
            logger.LogTraceAndDebug($"{nameof(NationalBackendService)}: {nameof(GetEUValueSetResponseAsync)} was invoked.");

            var ((valueSet, blobValueSet), cacheLastUpdated) = await memoryCache.GetOrCreateCacheAsync(cacheKeyValueSet,
                async () => (await valueSetService.GetEUValueSetAsync(), DateTime.UtcNow),
                DateTimeOffset.UtcNow.AddSeconds(settings.ValueSetCacheTimeSeconds));

            if(lastTimeChecked > cacheLastUpdated)
            {
                return null;
            }

            if (includeNonEUValues)
            {
                valueSet = NationalBackendUtils.CombineValueSets(valueSet, blobValueSet);
            }

            var response = new EUValueSetResponse(valueSet, cacheLastUpdated);

            logger.LogTraceAndDebug($"{nameof(NationalBackendService)}: {nameof(GetEUValueSetResponseAsync)} has finished.");

            return response;
        }

        public async Task<DomesticPolicyInformation> GetDomesticPolicyInformationAsync(DateTime lastTimeChecked)
        {
            logger.LogTraceAndDebug($"{nameof(NationalBackendService)}: {nameof(GetDomesticPolicyInformationAsync)} was invoked.");

            var policy = await memoryCache.GetOrCreateCacheAsync(cacheKeyPolicy,
                async () => await domesticPolicyInformationService.GetDomesticPolicyInformationAsync(),
                DateTimeOffset.UtcNow.AddSeconds(settings.PolicyCacheTimeSeconds));

            logger.LogTraceAndDebug($"{nameof(NationalBackendService)}: {nameof(GetDomesticPolicyInformationAsync)} has finished.");

            return lastTimeChecked < policy.LastUpdated ? policy : null;
        }

        private TrustListSubjectPublicKeyInfoDto DSCToSubjectPublicKeyInfoDto(DocumentSignerCertificate dsc)
        {
            try
            {
                return dsc.ConvertToSubjectPublicKeyInfoDto();
            }
            catch (NullReferenceException e)
            {
                logger.LogError(e, $"Certificate with kid '{dsc.Kid}' cannot be converted to TrustListSubjectPublicKeyInfoDto.");
                return null;
            }
        }
    }
}
