using System.IdentityModel.Tokens.Jwt;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IAssertedLoginIdentityService
    {
        public string GenerateAssertedLoginIdentity(JwtSecurityToken jtiValue);
    }
}
