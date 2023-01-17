using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.ResponseDtos;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Services
{
    public class UserPolicyService : IUserPolicyService
    {
        IGracePeriodService gracePeriodService;
        ILogger<UserPolicyService> logger;
        IMongoRepository<UserPolicies> mongoRepository;

        public UserPolicyService(ILogger<UserPolicyService> logger, IGracePeriodService gracePeriodService, IMongoRepository<UserPolicies> mongoRepository)
        {
            this.logger = logger;
            this.gracePeriodService = gracePeriodService;
            this.mongoRepository = mongoRepository;
        }

        public UserPoliciesResponse GetUserPolicies(CovidPassportUser covidUser)
        {
            logger.LogInformation($"{nameof(GetUserPolicies)} was invoked");

            var gracePeriod = covidUser.GracePeriod;
            var domesticAccessLevel = covidUser.DomesticAccessLevel.ToString();

            logger.LogInformation($"{nameof(GetUserPolicies)} has finished");

            return new UserPoliciesResponse(covidUser.ToNhsNumberAndDobHashKey())
            {
                GracePeriod = gracePeriod,
                DomesticAccessLevel = domesticAccessLevel
            };
        }
    }
}
