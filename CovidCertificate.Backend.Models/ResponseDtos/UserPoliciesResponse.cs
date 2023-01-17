namespace CovidCertificate.Backend.Models.ResponseDtos
{
    public class UserPoliciesResponse
    {
        public string NhsNumberDobHash { get; private set; }

        public GracePeriodResponse GracePeriod { get; set; }

        public string DomesticAccessLevel { get; set; }

        public UserPoliciesResponse(string nhsNumberDobHash)
        {
            NhsNumberDobHash = nhsNumberDobHash;
        }
    }
}
