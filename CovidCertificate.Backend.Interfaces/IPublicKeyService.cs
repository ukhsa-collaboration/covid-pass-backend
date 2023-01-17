using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces
{
    public interface IPublicKeyService
    {
        Task<IList<JsonWebKey>> GetPublicKeysAsync();

        Task<IList<JsonWebKey>> RefreshPublicKeysAsync();
    }
}