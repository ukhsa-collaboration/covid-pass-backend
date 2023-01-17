using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CovidCertificate.Backend.Interfaces.BlobService;
using CovidCertificate.Backend.Models.Exceptions;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;

namespace CovidCertificate.Backend.Services.AzureServices
{
    public class BlobService : IBlobService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<BlobService> logger;
        private readonly BlobServiceSettings settings;
        private string connectionString;
        private const string ConnectionStringkey = "PublicKeyBlobStoreConnectionString";

        public BlobService(IConfiguration configuration, ILogger<BlobService> logger, BlobServiceSettings settings)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.settings = settings;
        }

        private void SetConnectionString(string key)
        {
            logger.LogInformation(LogType.BlobStorage, "SetConnectionString was invoked");
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var connString = configuration[key];
            if(string.IsNullOrEmpty(connString))
                throw new ConfigurationException("Connection string does not exist");

            connectionString = connString;
            logger.LogInformation(LogType.BlobStorage, "SetConnectionString has finished");
        }

        public async Task<T> GetObjectFromBlobAsync<T>(string container, string location) where T : class, new()
        {
            var returnText = await GetStringFromBlobAsync(container, location);

            return JsonConvert.DeserializeObject<T>(returnText);
        }

        public async Task<string> GetStringFromBlobAsync(string container, string location)
        {
            logger.LogInformation(LogType.BlobStorage, $"Try to get object from container: '{container}', location: '{location}'.");

            ContainerLocationCheck(container, location);

            var blobClient = GetBlobClient(container, location);
            var blobStream = await blobClient.OpenReadAsync();
            using var reader = new StreamReader(blobStream);
            var returnText = await reader.ReadToEndAsync();

            logger.LogInformation(LogType.BlobStorage, $"'{location} fetched successfully, returning object.");

            return returnText;

        }

        public async Task<byte[]> GetImageFromBlobWithRetryAsync(string container, string location)
        {
            var response = Policy
              .Handle<SocketException>()
              .Or<TimeoutException>()
              .WaitAndRetryAsync(settings.GetImageRetryCount, count => TimeSpan.FromMilliseconds(settings.GetImageRetrySleepDurationInMilliseconds))
              .ExecuteAsync(
                  async () => await GetImageFromBlobAsync(container, location)
               );

            return await response;
        }

        public async Task<BlobProperties> GetBlobPropertiesAsync(string container, string location)
        {
            logger.LogInformation(LogType.BlobStorage, $"Try to get blob properties from container: '{container}', location: '{location}'.");

            ContainerLocationCheck(container, location);

            var blobClient = GetBlobClient(container, location);
            var properties = await blobClient.GetPropertiesAsync();

            logger.LogInformation(LogType.BlobStorage, $"'{location} properties fetched successfully, returning properties.");

            return properties;
        }

        private async Task<byte[]> GetImageFromBlobAsync(string container, string location)
        {
            logger.LogInformation(LogType.BlobStorage, "GetImageFromBlob was invoked");

            ContainerLocationCheck(container, location);

            var blobClient = GetBlobClient(container, location);
            var s = await blobClient.OpenReadAsync(new BlobOpenReadOptions(false));
            var blobLength = blobClient.GetProperties().Value.ContentLength;
            var toReturn = new byte[blobLength];
            await s.ReadAsync(toReturn, 0, int.MaxValue);

            logger.LogInformation(LogType.BlobStorage, "GetImageFromBlob has finished");

            return toReturn;
        }

        public async Task<bool> SaveToBlobAsync<T>(T objectToSave, string container, string location) where T : class, new()
        {
            logger.LogInformation(LogType.BlobStorage, $"SaveToBlob was invoked, container/location: '{container}/{location}'.");
            ContainerLocationCheck(container, location);

            var blobClient = GetBlobClient(container, location);

            string uploadText;
            if (typeof(T) == typeof(string))
            {
                uploadText = objectToSave as string;
            } else
            {
                uploadText = JsonConvert.SerializeObject(objectToSave);
            }

            try
            {
                var uploadTask = await blobClient.UploadAsync(uploadText.GetStream());
                logger.LogInformation(LogType.BlobStorage, "SaveToBlob has finished");
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(LogType.BlobStorage, e.Message);
                logger.LogInformation(LogType.BlobStorage, "SaveToBlob has finished");
                return false;
            }
        }

        private BlobClient GetBlobClient(string container, string location)
        {
            logger.LogInformation(LogType.BlobStorage, "GetBlobClient was invoked");

            ContainerLocationCheck(container, location);

            if (string.IsNullOrEmpty(connectionString))
            {
                SetConnectionString(ConnectionStringkey);
            }

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(container);

            logger.LogInformation(LogType.BlobStorage, "GetBlobClient has finished");

            return containerClient.GetBlobClient(location);
        }

        private void ContainerLocationCheck(string container, string location)
        {
            if (string.IsNullOrEmpty(container))
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (string.IsNullOrEmpty(location))
            {
                throw new ArgumentNullException(nameof(location));
            }
        }
    }
}
