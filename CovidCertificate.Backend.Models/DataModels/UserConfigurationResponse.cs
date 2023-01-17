using CovidCertificate.Backend.Models.ResponseDtos;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class UserConfigurationResponse
    {
        public UserPreferenceResponse Preferences { get; set; }
        public UserPoliciesResponse Policies { get; set; }

        public UserConfigurationResponse(UserPreferenceResponse userPreference, UserPoliciesResponse userPolicy)
        {
            this.Preferences = userPreference;
            this.Policies = userPolicy;
        }
    }
}
