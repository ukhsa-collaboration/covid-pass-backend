using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Settings;
using CovidCertificate.Backend.NhsApiIntegration.Interfaces;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.DomesticExemptions
{
    public class MedicalExemptionService : IMedicalExemptionService
    {
        private readonly IMedicalExemptionApiService medicalExemptionApiService;
        private readonly ILogger<MedicalExemptionService> logger;
        private readonly IRedisCacheService redisCacheService;

        private readonly IEnumerable<ExemptionReasonCode> validReasonCodes = new List<ExemptionReasonCode>() { ExemptionReasonCode.Type1ValidVaccinationExemption, ExemptionReasonCode.Type2ValidVaccinationAndTestExemption };

        public MedicalExemptionService(IMedicalExemptionApiService medicalExemptionApiService,
                                       ILogger<MedicalExemptionService> logger,
                                       IRedisCacheService redisCacheService)
        {
            this.medicalExemptionApiService = medicalExemptionApiService;
            this.logger = logger;
            this.redisCacheService = redisCacheService;
        }

        public virtual async Task<bool> IsUserMedicallyExemptAsync(CovidPassportUser user, string idToken)
        {
            var exemptions = await GetMedicalExemptionsAsync(user, idToken);
            return exemptions.Where(exemption => IsValidExemption(exemption)).Any();
        }

        public virtual async Task<IEnumerable<MedicalExemption>> GetMedicalExemptionsAsync(CovidPassportUser user, string idToken)
        {
            logger.LogTraceAndDebug($"{nameof(GetMedicalExemptionsAsync)} was invoked.");

            var key = $"GetMedicalExemptions:{user.ToNhsNumberAndDobHashKey()}";

            (var medicalExemptions, var cachedExemptionExists) = await redisCacheService.GetKeyValueAsync<IEnumerable<MedicalExemption>>(key);
            if (cachedExemptionExists)
            {
                return medicalExemptions;
            }

            medicalExemptions = string.IsNullOrEmpty(idToken) ? await medicalExemptionApiService.GetMedicalExemptionDataUnattendedAsync(user.NhsNumber) :
                                                            await medicalExemptionApiService.GetMedicalExemptionDataAttendedAsync(idToken);
            if (medicalExemptions == null)
            {
                return Enumerable.Empty<MedicalExemption>();
            }

            await redisCacheService.AddKeyAsync<IEnumerable<MedicalExemption>>(key, medicalExemptions, RedisLifeSpanLevel.OneHour);
            

            logger.LogTraceAndDebug($"{nameof(GetMedicalExemptionsAsync)} has finished.");
            return medicalExemptions;
        }

        public virtual async Task<IEnumerable<DomesticExemption>> GetValidMedicalExemptionsAsDomesticExemptionsAsync(CovidPassportUser user, string idToken)
        {
            logger.LogTraceAndDebug($"{nameof(GetValidMedicalExemptionsAsDomesticExemptionsAsync)} was invoked.");

            var medicalExemptions = await GetMedicalExemptionsAsync(user, idToken);
            var exemptions =  medicalExemptions?.Where(exemption => IsValidExemption(exemption))
                                                                          .Select(medicalExemption => (DomesticExemption)medicalExemption) 
                                                       ?? Enumerable.Empty<DomesticExemption>();

            logger.LogTraceAndDebug($"{nameof(GetValidMedicalExemptionsAsDomesticExemptionsAsync)} has finished.");
            return exemptions;
        }        

        private bool IsValidExemption(MedicalExemption exemption)
        {
            if(exemption.DateExemptionExpires != null && exemption.DateExemptionExpires.Value < DateTime.UtcNow)
            {
                return false;
            }

            return exemption.ExemptionReasonCode == null || validReasonCodes.Contains(exemption.ExemptionReasonCode.Value);
        }
    }
}
