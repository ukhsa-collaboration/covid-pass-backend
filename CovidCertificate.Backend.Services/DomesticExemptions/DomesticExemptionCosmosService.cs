using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Utils;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.DomesticExemptions
{
    public class DomesticExemptionCosmosService : IDomesticExemptionRecordsService
    {
        private readonly ILogger<DomesticExemptionService> logger;
        private readonly IMongoRepository<DomesticExemptionRecord> mongoRepository;
        private readonly IDomesticExemptionCache domesticExemptionCache;

        public DomesticExemptionCosmosService(ILogger<DomesticExemptionService> logger,
                                              IMongoRepository<DomesticExemptionRecord> mongoRepository,
                                              IDomesticExemptionCache domesticExemptionCache)
        {
            this.logger = logger;
            this.mongoRepository = mongoRepository;
            this.domesticExemptionCache = domesticExemptionCache;
        }

        public async Task<IEnumerable<DomesticExemptionRecord>> GetDomesticExemptionsAsync(string nhsNumber, DateTime dateOfBirth)
        {
            logger.LogTraceAndDebug($"{nameof(GetDomesticExemptionsAsync)} was invoked");

            if (string.IsNullOrEmpty(nhsNumber) || dateOfBirth == default)
            {
                return null;
            }

            var userHash = HashUtils.GenerateHash(nhsNumber, dateOfBirth);

            var exemptions = await domesticExemptionCache.GetDomesticExemptionsAsync();

            logger.LogTraceAndDebug($"{nameof(GetDomesticExemptionsAsync)} has finished");

            if (exemptions.ContainsKey(userHash))
            {
                return exemptions[userHash];
            }

            return null;
        }

        public async Task<bool> SaveDomesticExemptionAsync(DomesticExemptionRecord exemption, bool isMedicalExemption)
        {
            logger.LogTraceAndDebug($"{nameof(SaveDomesticExemptionAsync)} was invoked.");
            if (string.IsNullOrEmpty(exemption.NhsDobHash) || string.IsNullOrEmpty(exemption.Reason))
                return false;

            logger.LogInformation($"Checking if record for user: {exemption.NhsDobHash} already exists.");
            var existingRecord = await mongoRepository.FindOneAsync(x => x.NhsDobHash == exemption.NhsDobHash && x.IsMedicalExemption == isMedicalExemption);
            if (existingRecord == null)
            {
                logger.LogInformation("Record not found, creating new one.");
                await mongoRepository.InsertOneAsync(exemption);
                logger.LogTraceAndDebug($"{nameof(SaveDomesticExemptionAsync)} has finished.");
                return true;
            }
            logger.LogInformation("Record found, replacing old one.");
            exemption.Id = existingRecord.Id;
            await mongoRepository.ReplaceOneAsync(exemption, x => x.NhsDobHash == exemption.NhsDobHash && x.IsMedicalExemption == isMedicalExemption);

            logger.LogTraceAndDebug($"{nameof(SaveDomesticExemptionAsync)} has finished.");
            return true;
        }

        public async Task RemoveDomesticExemptionsForUserAsync(string nhsNumberDobHash, bool isMedicalExemption)
        {
            logger.LogTraceAndDebug($"{nameof(RemoveDomesticExemptionsForUserAsync)} was invoked");

            await mongoRepository.DeleteManyAsync(x => x.NhsDobHash == nhsNumberDobHash && x.IsMedicalExemption == isMedicalExemption);

            logger.LogTraceAndDebug($"{nameof(RemoveDomesticExemptionsForUserAsync)} has finished");
        }
    }
}
