using System.Threading.Tasks;

namespace CovidCertificate.Backend.Interfaces.PKINationaBackend
{
    public interface IDGCGMutualTLSService
    {
        /// <summary>
        /// Makes a mutually authenticated call to the EU Digital Green Card gateway 
        /// and return the response.
        /// </summary>
        /// <param name="endpoint">The EUDGCG endpoint to call.</param>
        /// <returns>The successful response from the gateway.</returns>
        Task<string> MakeMutuallyAuthenticatedRequestAsync(string endpoint);
    }
}
