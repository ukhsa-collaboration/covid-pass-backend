using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models;
using CovidCertificate.Backend.Models.Pocos;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IEndpointProofingLevelService
    {
        Task<ValidationResponsePoco> ValidateProofingLevel(UserProperties userProperties,
            string callingEndpoint,
            DateTime dateOfBirth,
            ClaimsPrincipal tokenClaims);
    }
}
