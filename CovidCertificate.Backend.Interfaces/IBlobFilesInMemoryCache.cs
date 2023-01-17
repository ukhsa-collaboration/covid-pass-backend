using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    /// <summary>
    /// Store blob files in memory cache to reduce calls to blob storage
    /// </summary>
    public interface IBlobFilesInMemoryCache<T>
    {
        /// <summary>
        /// Get T either from the in memory cache or access the file in blob storage 
        /// and deserialize it to T if cache is invalid
        /// </summary>
        /// <returns> 
        /// T either from cache or deserialized file from blob storage
        /// </returns>
        public Task<T> GetFileAsync(string container, string filename);
    }
}
