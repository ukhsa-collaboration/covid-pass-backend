using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.ResponseDtos;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IUserPolicyService
    {
        UserPoliciesResponse GetUserPolicies(CovidPassportUser covidUser);
    }
}
