using System.Collections.Generic;
using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Interfaces.Redis;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.NhsApiIntegration.Interfaces;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services.DomesticExemptions
{
    public class UnattendedMedicalExemptionService : MedicalExemptionService, IMedicalExemptionService
    {
        public UnattendedMedicalExemptionService(IMedicalExemptionApiService medicalExemptionApiService,
                                       ILogger<MedicalExemptionService> logger,
                                       IRedisCacheService redisCacheService) : base(medicalExemptionApiService,
                                                                                    logger,
                                                                                    redisCacheService)
        {

        }

        public override async Task<bool> IsUserMedicallyExemptAsync(CovidPassportUser user, string idToken)
        {
            return await base.IsUserMedicallyExemptAsync(user, null);
        }

        public override async Task<IEnumerable<MedicalExemption>> GetMedicalExemptionsAsync(CovidPassportUser user, string idToken)
        {
            return await base.GetMedicalExemptionsAsync(user, null);
        }

        public override async Task<IEnumerable<DomesticExemption>> GetValidMedicalExemptionsAsDomesticExemptionsAsync(CovidPassportUser user, string idToken)
        {
            return await base.GetValidMedicalExemptionsAsDomesticExemptionsAsync(user, null);
        }
    }
}
