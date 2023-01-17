using System;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IMemoryCacheService
    {
        Task<T> GetOrCreateCacheAsync<T>(string cacheKey,
            Func<Task<T>> createCacheFunction,
            DateTimeOffset offset);
    }
}
