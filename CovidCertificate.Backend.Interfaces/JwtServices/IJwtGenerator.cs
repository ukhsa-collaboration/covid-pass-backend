using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Models.ResponseDtos;

namespace CovidCertificate.Backend.Interfaces.JwtServices
{
    public interface IJwtGenerator
    {
        Task<ValidateTwoFactorResponseDto> IssueAsync<T>(T user) where T : IJwtTokenData;
    }
}
