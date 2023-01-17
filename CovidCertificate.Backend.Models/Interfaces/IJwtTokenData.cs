using System.Collections.Generic;
using System.Security.Claims;

namespace CovidCertificate.Backend.Models.Interfaces
{
    public interface IJwtTokenData
    {
        IEnumerable<Claim> GetClaims();
    }
}
