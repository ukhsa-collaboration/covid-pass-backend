using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.PKINationaBackend;
using CovidCertificate.Backend.Models.PKINationalBackend;
using CovidCertificate.Backend.PKINationalBackend.Models;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Services.PKINationaBackend
{
    public class ValueSetService : IValueSetService
    {
        private readonly ILogger<ValueSetService> logger;
        private readonly IDGCGMutualTLSService mutualTLSService;
        private readonly DGCGSettings settings;
        private readonly IBlobFilesInMemoryCache<EUValueSet> valueSetCache;
        private readonly IConfiguration configuration;
        public ValueSetService(ILogger<ValueSetService> logger,
                               IDGCGMutualTLSService mutualTLSService,
                               DGCGSettings settings,
                               IBlobFilesInMemoryCache<EUValueSet> valueSetCache,
                               IConfiguration configuration)
        {
            this.logger = logger;
            this.mutualTLSService = mutualTLSService;
            this.settings = settings;
            this.valueSetCache = valueSetCache;
            this.configuration = configuration;
        }

        public async Task<(EUValueSet, EUValueSet)> GetEUValueSetAsync() 
        {
            logger.LogTraceAndDebug($"{nameof(ValueSetService)}: {nameof(GetEUValueSetAsync)} was invoked.");

            var apiValueSetTask = GetValueSetFromAPIAsync();
            var blobStoreValueSetTask = GetValueSetFromBlobStoreAsync();

            await Task.WhenAll(apiValueSetTask, blobStoreValueSetTask);   
            
            logger.LogTraceAndDebug($"{nameof(ValueSetService)}: {nameof(GetEUValueSetAsync)} has finished.");

            return (await apiValueSetTask, await blobStoreValueSetTask);
        }

        private async Task<EUValueSet> GetValueSetFromBlobStoreAsync()
        {
            var container = configuration["BlobContainerNameEUValueSets"];
            var filename = configuration["BlobFileNameEUValueSets"];
            return await valueSetCache.GetFileAsync(container, filename);
        }

        private async Task<EUValueSet> GetValueSetFromAPIAsync()
        {
            var valueSet = new EUValueSet();

            var valuesResponsesTask = settings.ValueSets.Keys.Select(valueProperty => AddValuesToValueSetAsync(valueProperty, valueSet));
            await Task.WhenAll(valuesResponsesTask);

            return valueSet;
        }

        private async Task AddValuesToValueSetAsync(string valueProperty, EUValueSet valueSet)
        {
            var propertyValueSet = await GetValueSetResponseAsync(settings.ValueSets[valueProperty]);
            var values = ConvertApiResponseToValues(propertyValueSet);
            typeof(EUValueSet).GetProperty(valueProperty).SetValue(valueSet, values, null);
        }

        private async Task<DGCGValueSetAPIResponse> GetValueSetResponseAsync(string endpoint)
        {
            var response = await mutualTLSService.MakeMutuallyAuthenticatedRequestAsync("valuesets/" + endpoint);
            var valueSetValues = JsonConvert.DeserializeObject<DGCGValueSetAPIResponse>(response);
            return valueSetValues;
        }

        private Dictionary<string, string> ConvertApiResponseToValues(DGCGValueSetAPIResponse apiData)
        {
            var values = new Dictionary<string, string>();
            foreach(var key in apiData.valueSetValues.Keys)
            {
                var valueDict = apiData.valueSetValues[key];
                values.Add(key, valueDict["display"]);
            }

            return values;
        }
    }
}
