using System;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace CovidCertificate.Backend.Services.InMemoryCache
{
    public class MemoryCacheService: IMemoryCacheService
    {
        private readonly IMemoryCache memoryCache;

        public MemoryCacheService(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;
        }

        public async Task<T> GetOrCreateCacheAsync<T>(string cacheKey,
            Func<Task<T>> createCacheFunction,
            DateTimeOffset offset)
        {
            var cacheExists = memoryCache.TryGetValue(cacheKey, out T cachedValue);

            if (cacheExists)
            {
                return cachedValue;
            }

            await AsyncGenericLock<T>.LockAsync();

            try
            {
                cachedValue = await memoryCache.GetOrCreateAsync(
                    cacheKey,
                    async cacheEntry =>
                    {
                        cacheEntry.AbsoluteExpiration = offset;

                        return await createCacheFunction();
                    });
            }
            finally
            {
                AsyncGenericLock<T>.Release();
            }

            return cachedValue;
        }
    }
}
