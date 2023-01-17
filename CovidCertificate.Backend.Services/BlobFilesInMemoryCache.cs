using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.BlobService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace CovidCertificate.Backend.Services
{
    public class BlobFilesInMemoryCache<T> : IBlobFilesInMemoryCache<T> where T : class, new()
    {
        private readonly ILogger<BlobFilesInMemoryCache<T>> logger;
        private readonly IConfiguration configuration;
        private readonly IBlobService blobService;
        private readonly IMemoryCacheService memoryCache;

        public BlobFilesInMemoryCache(ILogger<BlobFilesInMemoryCache<T>> logger,
            IConfiguration configuration,
            IBlobService blobService,
            IMemoryCacheService memoryCache)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.blobService = blobService;
            this.memoryCache = memoryCache;
        }

        public async Task<T> GetFileAsync(string container, string filename)
        {
            logger.LogInformation($"{nameof(BlobFilesInMemoryCache<T>)} was invoked.");

            var cacheKey = container + "|" + filename;

            var blobFile = await memoryCache.GetOrCreateCacheAsync(
                cacheKey,
                async () => await CreateBlobCacheEntryAsync(container, filename),
                DateTimeOffset.Now.AddSeconds(configuration.GetValue<int?>("BlobFilesInMemoryCacheExpiryTime") ?? 600)
                );
            
            logger.LogInformation($"{nameof(BlobFilesInMemoryCache<T>)} cache is valid, returning {typeof(T)}.");

            return blobFile;
        }

        private async Task<T>CreateBlobCacheEntryAsync(string container, string filename)
        {
            logger.LogInformation("In-memory blobFile not found or invalid. Attempting to fetch data from blobFile storage.");

            return await blobService.GetObjectFromBlobAsync<T>(container, filename);
        }
    }
}
