using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;

namespace CovidCertificate.Backend.Interfaces.BlobService
{
    /// <summary>
    /// This service is used to save and load objects to the blob store
    /// </summary>
    public interface IBlobService
    {
        /// <summary>
        /// Fetches an object from the blob store and deserialises to T
        /// </summary>
        /// <typeparam name="T">The type to deserialise</typeparam>
        /// <param name="container">Our blob container</param>
        /// <param name="location">The file location</param>
        /// <returns>Our object</returns>
        Task<T> GetObjectFromBlobAsync<T>(string container, string location) where T : class, new();
        Task<string> GetStringFromBlobAsync(string container, string location);

        Task<byte[]> GetImageFromBlobWithRetryAsync(string container, string location);

        /// <summary>
        /// Serialises and then saves an object to blob store in a location we set
        /// </summary>
        /// <typeparam name="T">The type we want to save</typeparam>
        /// <param name="objectToSave">The object we want to upload</param>
        /// <param name="container">The upload container</param>
        /// <param name="location">Our file location within the container</param>
        /// <returns></returns>
        Task<bool> SaveToBlobAsync<T>(T objectToSave, string container, string location) where T : class, new();

        /// <summary>
        /// Fetches a blob's properties
        /// </summary>
        /// <param name="container">Our blob container</param>
        /// <param name="location">The file location</param>
        /// <returns>The blob's properties</returns>
        Task<BlobProperties> GetBlobPropertiesAsync(string container, string location);
    }
}
