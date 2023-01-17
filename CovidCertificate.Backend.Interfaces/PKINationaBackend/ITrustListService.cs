using System.Threading.Tasks;
using CovidCertificate.Backend.Models.PKINationalBackend;

namespace CovidCertificate.Backend.Interfaces.PKINationaBackend
{
    public interface ITrustListService
    {
        /// <summary>
        /// Returns the latest EU Trust List from the EU Digital Green Card Gateway.
        /// </summary>
        /// <returns>Returns the latest EU Trust List from the EU Digital Green Card Gateway.</returns>
        Task<DGCGTrustList> GetDGCGTrustListAsync();
    }
}
