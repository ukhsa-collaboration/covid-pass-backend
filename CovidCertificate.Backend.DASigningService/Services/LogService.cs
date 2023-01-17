using System;
using System.Net;
using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.Interfaces;
using CovidCertificate.Backend.DASigningService.Models;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using Microsoft.Extensions.Logging;


namespace CovidCertificate.Backend.DASigningService.Services
{
    public class LogService : ILogService
    {
        private readonly IMongoRepository<Region2DBarcodeResult> mongoRepository;

        public LogService(IMongoRepository<Region2DBarcodeResult> mongoRepository)
        {            
            this.mongoRepository = mongoRepository;            
        }

        public async Task LogResultAsync(ILogger logger, string uvci, string apiName, HttpStatusCode httpCode, RegionConfig regionConfig)
        {
            string regionCode = "Unrecognized";

            if (regionConfig != null)
            {
                regionCode = regionConfig.SubscriptionKeyIdentifier;
            }
            const string formatString = "{regionCode}:{className}:{message}";
            string messageString = $"Returned HTTP code: {httpCode}";

            switch (httpCode)
            {
                case HttpStatusCode.OK:
                    logger.LogInformation(formatString, regionCode, apiName, messageString);
                    break;
                case HttpStatusCode.BadRequest:
                    logger.LogWarning(formatString, regionCode, apiName, messageString);
                    break;
                default:
                    logger.LogError(formatString, regionCode, apiName, messageString);
                    break;
            }
            
            await SaveResultToCosmosDBAsync(uvci, (int)httpCode, DateTime.Now, regionCode);
        }

        private async Task SaveResultToCosmosDBAsync(string uvci, int httpStatus, DateTime timstamp, string regionCode)
        {
            var document = new Region2DBarcodeResult(uvci, httpStatus, timstamp, regionCode);

            await mongoRepository.InsertOneAsync(document);
        }
    }
}
