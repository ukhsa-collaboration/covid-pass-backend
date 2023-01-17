using System.Threading.Tasks;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Services
{
    public class UserConfigurationService : IUserConfigurationService
    {
        private readonly IUserPreferenceService userPreferenceService;
        private readonly IUserPolicyService userPolicyService;
        private readonly ILogger<UserConfigurationService> logger;

        public UserConfigurationService(ILogger<UserConfigurationService> logger, IUserPreferenceService userPreferenceService, IUserPolicyService userPolicyService)
        {
            this.logger = logger;
            this.userPreferenceService = userPreferenceService;
            this.userPolicyService = userPolicyService;
        }

        public async Task<UserConfigurationResponse> GetUserConfigurationAsync(CovidPassportUser covidUser,string preferenceHash)
        {
            logger.LogTraceAndDebug($"{nameof(GetUserConfigurationAsync)} was invoked");

            var userPreferences = await userPreferenceService.GetPreferencesAsync(preferenceHash);
            var userPolicies = userPolicyService.GetUserPolicies(covidUser);

            logger.LogTraceAndDebug($"{nameof(GetUserConfigurationAsync)} has finished");

            return new UserConfigurationResponse(userPreferences, userPolicies);
        }
    }
}
