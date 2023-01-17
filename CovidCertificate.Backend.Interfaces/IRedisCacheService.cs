using CovidCertificate.Backend.Models.Settings;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IRedisCacheService
    {
        /// <summary>
        /// Add the key using RedisLifeSpanLevel, which it can be either FiveMinutes, ThirtyMinutes, OneHour, TenHours,OneDay
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expirationType"></param>
        /// <returns></returns>
        Task<bool> AddKeyAsync<T>(string key, T value, RedisLifeSpanLevel redisLifeSpanLevel);

        /// <summary>
        /// Looks up the value for the specified key and returns if one exists.
        /// </summary>
        /// <typeparam name="T">Return Type</typeparam>
        /// <param name="key"></param>
        /// <returns>
        /// </returns>
        Task<(T, bool)> GetKeyValueAsync<T>(string key);
    }
}
